using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace BeatSaber.SongHashing
{
    /// <summary>
    /// A collection of hashing utilities.
    /// </summary>
    public class Hasher : IBeatmapHasher
    {
        /// <inheritdoc/>
        public HashResult HashDirectory(string songDirectory)
        {
            if (string.IsNullOrEmpty(songDirectory))
                throw new ArgumentNullException(nameof(songDirectory));
            bool missingDiffs = false;
            string? message = null;
            DirectoryInfo directory = new DirectoryInfo(songDirectory);
            if (!directory.Exists)
                throw new DirectoryNotFoundException($"Directory doesn't exist: '{songDirectory}'");
            try
            {
                //FileInfo[] files = directory.GetFiles();
                // Could theoretically get the wrong hash if there are multiple 'info.dat' files with different cases on linux.
                string? infoFileName = directory.EnumerateFiles().FirstOrDefault(f => f.Name.Equals("info.dat", StringComparison.OrdinalIgnoreCase))?.FullName;
                if (infoFileName == null)
                {
                    return new HashResult(null, $"Could not find 'info.dat' file in '{songDirectory}'", null);
                }
                string infoFile = Path.Combine(songDirectory, infoFileName);
                if (!File.Exists(infoFile))
                    return new HashResult(null, $"Could not find 'info.dat' file in '{songDirectory}'", null);

                JObject token = JObject.Parse(File.ReadAllText(infoFile));
                string[] beatmapFiles = Utilities.GetDifficultyFileNames(token).ToArray();
                using ConcatenatedStream streams = new ConcatenatedStream(beatmapFiles.Length + 1);
                streams.Append(File.OpenRead(infoFile));
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
                    streams.Append(File.OpenRead(filePath));
                }

                string? hash = null;
                if (streams.StreamCount > 0)
                    hash = Utilities.CreateSha1FromStream(streams);
                return new HashResult(hash, message, null);
            }
            catch (Exception ex)
            {
                return new HashResult(null, $"Error hashing '{songDirectory}'", ex);
            }
        }

        /// <inheritdoc/>
        public long QuickDirectoryHash(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path), "Path cannot be null or empty for GenerateDirectoryHash");
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
                throw new DirectoryNotFoundException($"GenerateDirectoryHash couldn't find {path}");
            long dirHash = 0L;
            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                dirHash ^= file.CreationTimeUtc.ToFileTimeUtc();
                dirHash ^= file.LastWriteTimeUtc.ToFileTimeUtc();
                dirHash ^= Utilities.GetStringHash(file.Name);
                dirHash ^= file.Length;
            }
            return dirHash;
        }
    }
}
