using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace BeatSaber.SongHashing.Legacy
{
    /// <summary>
    /// A collection of hashing utilities.
    /// </summary>
    public class LegacyHasher : IBeatmapHasher
    {
        /// <summary>
        /// Default instance of <see cref="LegacyHasher"/>.
        /// </summary>
        public static readonly LegacyHasher Default = new LegacyHasher();

        /// <inheritdoc/>
        public HashResult HashDirectory(string songDirectory)
        {
            if (string.IsNullOrEmpty(songDirectory))
                throw new ArgumentNullException(nameof(songDirectory));
            DirectoryInfo directory = new DirectoryInfo(songDirectory);
            if (!directory.Exists)
                throw new DirectoryNotFoundException($"Directory doesn't exist: '{songDirectory}'");
            try
            {
                bool missingDiffs = false;
                string? message = null;
                byte[] combinedBytes = Array.Empty<byte>();
                FileInfo[] files = directory.GetFiles();
                // Could theoretically get the wrong hash if there are multiple 'info.dat' files with different cases on linux.
                string? infoFileName = files.FirstOrDefault(f => f.Name.Equals("info.dat", StringComparison.OrdinalIgnoreCase))?.FullName;
                if (infoFileName == null)
                {
                    return new HashResult(null,$"Could not find 'info.dat' file in '{songDirectory}'", null);
                }
                string infoFile = Path.Combine(songDirectory, infoFileName);
                if (!File.Exists(infoFile))
                    return new HashResult(null, $"Could not find 'info.dat' file in '{songDirectory}'", null);

                combinedBytes = combinedBytes.Concat(File.ReadAllBytes(infoFile)).ToArray();
                JObject? token = JObject.Parse(File.ReadAllText(infoFile));
                string[] beatmapFiles = Utilities.GetDifficultyFileNames(token).ToArray();
                for (int i = 0; i < beatmapFiles.Length; i++)
                {
                    string filePath = Path.Combine(songDirectory, beatmapFiles[i]);
                    if (!File.Exists(filePath))
                    {
                        if (missingDiffs == false)
                        {
                            message = $"Could not find difficulty file '{filePath}'";
                        }
                        else
                            message = $"Missing multiple difficulty files in '{songDirectory}'";
                        missingDiffs = true;
                        continue;
                    }
                    combinedBytes = combinedBytes.Concat(File.ReadAllBytes(filePath)).ToArray();
                }

                string hash = Utilities.CreateSha1FromBytes(combinedBytes.ToArray());
                return new HashResult(hash, message, null);
            }
            catch (Exception ex)
            {
                return new HashResult(null, $"Error hashing '{songDirectory}'", ex);
            }
        }


        /// <summary>
        /// Generates a quick hash of a directory's contents. Does NOT match SongCore.
        /// Uses most of Kylemc1413's implementation from SongCore.
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="ArgumentNullException">Thrown when path is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when path's directory doesn't exist.</exception>
        /// <returns></returns>
        public long QuickDirectoryHash(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path), "Path cannot be null or empty for GenerateDirectoryHash");
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
                throw new DirectoryNotFoundException($"GenerateDirectoryHash couldn't find {path}");
            long dirHash = 0L;
            foreach (FileInfo file in directoryInfo.GetFiles("*.dat"))
            {
                dirHash ^= file.CreationTimeUtc.ToFileTimeUtc();
                dirHash ^= file.LastWriteTimeUtc.ToFileTimeUtc();
                //dirHash ^= file.Name.GetHashCode();
                dirHash ^= Utilities.SumCharacters(file.Name);
                dirHash ^= file.Length;
            }
            return dirHash;
        }
    }
}
