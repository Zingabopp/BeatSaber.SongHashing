using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace BeatSaber.SongHashing
{
    /// <summary>
    /// A collection of hashing utilities.
    /// </summary>
    public class Hasher : IBeatmapHasher
    {
        /// <summary>
        /// Shared <see cref="Newtonsoft.Json.JsonSerializer"/>.
        /// </summary>
        protected static readonly JsonSerializer JsonSerializer = new JsonSerializer();

        /// <inheritdoc/>
        public HashResult HashDirectory(string songDirectory, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(songDirectory))
                throw new ArgumentNullException(nameof(songDirectory));
            if (cancellationToken.IsCancellationRequested)
                return HashResult.AsCanceled;
            bool missingDiffs = false;
            string? message = null;
            DirectoryInfo directory = new DirectoryInfo(songDirectory);
            if (!directory.Exists)
                return new HashResult(null, $"Directory doesn't exist: '{songDirectory}'", new DirectoryNotFoundException($"Directory doesn't exist: '{songDirectory}'"));
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

                if (cancellationToken.IsCancellationRequested)
                    return HashResult.AsCanceled;
                string[] beatmapFiles = Utilities.GetDifficultyFileNames(token).ToArray();
                using ConcatenatedStream streams = new ConcatenatedStream(beatmapFiles.Length + 1);
                streams.Append(File.OpenRead(infoFile));
                for (int i = 0; i < beatmapFiles.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return HashResult.AsCanceled;
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

                if (cancellationToken.IsCancellationRequested)
                    return HashResult.AsCanceled;
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
        public HashResult HashZippedBeatmap(string zipPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(zipPath))
                throw new ArgumentNullException(nameof(zipPath));
            if (cancellationToken.IsCancellationRequested)
                return HashResult.AsCanceled;
            bool missingDiffs = false;
            string? message = null;
            if (!File.Exists(zipPath))
                return new HashResult(null, $"File doesn't exist: '{zipPath}'", new FileNotFoundException($"File doesn't exist: '{zipPath}'"));
            ZipArchive? zip = null;
            try
            {
                try
                {
                    zip = ZipFile.OpenRead(zipPath);
                }
                catch (Exception ex)
                {
                    return new HashResult(null, $"Unable to hash beatmap zip '{zipPath}': {ex.Message}", ex);
                }
                ZipArchiveEntry? infoFile = zip.Entries.FirstOrDefault(e => e.FullName.Equals("info.dat", StringComparison.OrdinalIgnoreCase));
                if (infoFile == null)
                {
                    return new HashResult(null, $"Could not find 'info.dat' file in '{zipPath}'", null);
                }
                JObject? token = null;
                using (Stream info = infoFile.Open())
                using (StreamReader reader = new StreamReader(info))
                using (JsonTextReader jsonReader = new JsonTextReader(reader))
                {
                    token = JsonSerializer.Deserialize<JObject>(jsonReader);
                }
                if (token == null)
                    return new HashResult(null, $"Could not read 'info.dat' file in '{zipPath}'", null);
                if (cancellationToken.IsCancellationRequested)
                    return HashResult.AsCanceled;
                using Stream infoStream = infoFile.Open();
                string[] beatmapFiles = Utilities.GetDifficultyFileNames(token).ToArray();
                using ConcatenatedStream streams = new ConcatenatedStream(beatmapFiles.Length + 1);
                streams.Append(infoStream);
                for (int i = 0; i < beatmapFiles.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return HashResult.AsCanceled;
                    ZipArchiveEntry? diff = zip.Entries.FirstOrDefault(e => e.FullName.Equals(beatmapFiles[i], StringComparison.OrdinalIgnoreCase));
                    if (diff == null)
                    {
                        if (missingDiffs == false)
                        {
                            message = $"Could not find difficulty file '{beatmapFiles[i]}' in '{zipPath}'";
                        }
                        else
                            message = $"Missing multiple difficulty files in '{zipPath}'";
                        missingDiffs = true;
                        continue;
                    }
                    streams.Append(diff.Open());
                }

                if (cancellationToken.IsCancellationRequested)
                    return HashResult.AsCanceled;
                string? hash = null;
                if (streams.StreamCount > 0)
                    hash = Utilities.CreateSha1FromStream(streams);
                return new HashResult(hash, message, null);
            }
            catch (Exception ex)
            {
                return new HashResult(null, $"Error hashing '{zipPath}'", ex);
            }
            finally
            {
                zip?.Dispose();
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
