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
        public string? HashDirectory(string songDirectory)
        {
            if (string.IsNullOrEmpty(songDirectory))
                throw new ArgumentNullException(nameof(songDirectory));
            DirectoryInfo directory = new DirectoryInfo(songDirectory);
            if (!directory.Exists)
                throw new DirectoryNotFoundException($"Directory doesn't exist: '{songDirectory}'");
            byte[] combinedBytes = Array.Empty<byte>();
            FileInfo[] files = directory.GetFiles();
            // Could theoretically get the wrong hash if there are multiple 'info.dat' files with different cases on linux.
            string? infoFileName = files.FirstOrDefault(f => f.Name.Equals("info.dat", StringComparison.OrdinalIgnoreCase))?.FullName;
            if (infoFileName == null)
            {
                return null;
            }
            string infoFile = Path.Combine(songDirectory, infoFileName);
            if (!File.Exists(infoFile))
                return null;
            
            combinedBytes = combinedBytes.Concat(File.ReadAllBytes(infoFile)).ToArray();
            JToken? token = JToken.Parse(File.ReadAllText(infoFile));
            JToken? beatMapSets = token["_difficultyBeatmapSets"];
            int numChars = beatMapSets?.Children().Count() ?? 0;
            for (int i = 0; i < numChars; i++)
            {
                JToken? diffs = beatMapSets.ElementAt(i);
                int numDiffs = diffs["_difficultyBeatmaps"]?.Children().Count() ?? 0;
                for (int i2 = 0; i2 < numDiffs; i2++)
                {
                    JToken? diff = diffs["_difficultyBeatmaps"].ElementAt(i2);
                    string? beatmapFileName = diff["_beatmapFilename"]?.Value<string>();
                    string? beatmapFile = files.FirstOrDefault(f => f.Name.Equals(beatmapFileName, StringComparison.OrdinalIgnoreCase))?.FullName;
                    if (beatmapFile != null)
                    {
                        string beatmapPath = Path.Combine(songDirectory, beatmapFile);
                        if (File.Exists(beatmapPath))
                            combinedBytes = combinedBytes.Concat(File.ReadAllBytes(beatmapPath)).ToArray();
                        //else
                        //    Logger.log?.Debug($"Missing difficulty file {beatmapPath.Split('\\', '/').LastOrDefault()}");
                    }
                    //else
                    //    Logger.log?.Warn($"_beatmapFilename property is null in {infoFile}");
                }
            }

            string hash = Utilities.CreateSha1FromBytes(combinedBytes.ToArray());
            return hash;
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
