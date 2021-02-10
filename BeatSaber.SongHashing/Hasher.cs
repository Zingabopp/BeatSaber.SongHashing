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
        /// <summary>
        /// Generates a hash for the song and assigns it to the SongHash field. Returns null if info.dat doesn't exist.
        /// Uses Kylemc1413's implementation from SongCore.
        /// TODO: Handle/document exceptions (such as if the files no longer exist when this is called).
        /// https://github.com/Kylemc1413/SongCore
        /// </summary>
        /// <returns>Hash of the song files. Null if the info.dat file doesn't exist</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="JsonException"></exception>
        public string? HashDirectory(string songDirectory)
        {
            if (string.IsNullOrEmpty(songDirectory))
                throw new ArgumentNullException(nameof(songDirectory));
            DirectoryInfo directory = new DirectoryInfo(songDirectory);
            if (!directory.Exists)
                throw new DirectoryNotFoundException($"Directory doesn't exist: '{songDirectory}'");
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
            
            JObject token = JObject.Parse(File.ReadAllText(infoFile));
            string[] beatmapFiles = Utilities.GetDifficultyFileNames(token).ToArray();
            using ConcatenatedStream streams = new ConcatenatedStream(beatmapFiles.Length + 1);
            streams.Append(File.OpenRead(infoFile));
            for(int i = 0; i < beatmapFiles.Length; i++)
            {
                string filePath = Path.Combine(songDirectory, beatmapFiles[i]);
                if (!File.Exists(filePath))
                    continue;
                streams.Append(File.OpenRead(filePath));
            }

            string hash = Utilities.CreateSha1FromStream(streams);
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
                dirHash ^= Utilities.SumCharacters(file.Name); // Replacement for if GetHashCode stops being predictable.
                dirHash ^= file.Length;
            }
            return dirHash;
        }
    }
}
