﻿using Newtonsoft.Json;
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
    public partial class Hasher : IBeatmapHasher
    {
        /// <summary>
        /// The result of <see cref="PrepareStream(string, CancellationToken)"/>
        /// </summary>
        protected struct PrepResult
        {
            /// <summary>
            /// Creates a new <see cref="PrepResult"/>
            /// </summary>
            /// <param name="streams"></param>
            /// <param name="warning"></param>
            public PrepResult(ConcatenatedStream streams, string? warning)
            {
                Streams = streams;
                Warning = warning;
            }
            /// <summary>
            /// <see cref="ConcatenatedStream"/> with all difficulty files
            /// </summary>
            public readonly ConcatenatedStream Streams;
            /// <summary>
            /// Warning message generated, if any
            /// </summary>
            public readonly string? Warning;
        }
        /// <summary>
        /// Shared <see cref="Newtonsoft.Json.JsonSerializer"/>.
        /// </summary>
        protected static readonly JsonSerializer JsonSerializer = new JsonSerializer();
        /// <summary>
        /// Prepares the stream to be hashed
        /// </summary>
        /// <param name="songDirectory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected PrepResult PrepareStream(string songDirectory, CancellationToken cancellationToken)
        {
            DirectoryInfo directory = new DirectoryInfo(songDirectory);
            if (!directory.Exists)
                throw new DirectoryNotFoundException($"Directory doesn't exist: '{songDirectory}'");

            //FileInfo[] files = directory.GetFiles();
            // TODO: Could theoretically get the wrong hash if there are multiple 'info.dat' files with different cases on linux.
            string? infoFileName = directory.EnumerateFiles().FirstOrDefault(f => f.Name.Equals("info.dat", StringComparison.OrdinalIgnoreCase))?.FullName;

            if (infoFileName == null)
            {
                throw new InvalidOperationException($"Could not find 'info.dat' file in '{songDirectory}'");
            }
            string infoFile = Path.Combine(songDirectory, infoFileName);
            if (!File.Exists(infoFile))
                throw new InvalidOperationException($"Could not find 'info.dat' file in '{songDirectory}'");

            JObject token = JObject.Parse(File.ReadAllText(infoFile));

            cancellationToken.ThrowIfCancellationRequested();
            string[] beatmapFiles = Utilities.GetDifficultyFileNames(token).ToArray();
            ConcatenatedStream streams = new ConcatenatedStream(beatmapFiles.Length + 1);
            streams.Append(File.OpenRead(infoFile));

            bool missingDiffs = false;
            string? message = null;
            for (int i = 0; i < beatmapFiles.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
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
            return new PrepResult(streams, message);
        }

        /// <inheritdoc/>
        public HashResult HashDirectory(string songDirectory, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(songDirectory))
                throw new ArgumentNullException(nameof(songDirectory));
            if (cancellationToken.IsCancellationRequested)
                return HashResult.AsCanceled;
            try
            {
                var prep = PrepareStream(songDirectory, cancellationToken);
                using ConcatenatedStream streams = prep.Streams;
                string? message = prep.Warning;

                if (cancellationToken.IsCancellationRequested)
                    return HashResult.AsCanceled;
                string? hash = null;
                if (streams.StreamCount > 0)
                    hash = Utilities.CreateSha1FromStream(streams);
                return new HashResult(hash, message, null);
            }
            catch (OperationCanceledException)
            {
                return HashResult.AsCanceled;
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
