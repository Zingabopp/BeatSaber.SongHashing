using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BeatSaber.SongHashing
{
    /// <summary>
    /// Interface for a beatmap hasher.
    /// </summary>
    public interface IBeatmapHasher
    {
        /// <summary>
        /// Generates a hash for the song and assigns it to the SongHash field. Returns null if info.dat doesn't exist.
        /// </summary>
        /// <returns>Hash of the song files. Null if the info.dat file doesn't exist</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="JsonException"></exception>
        HashResult HashDirectory(string songDirectory);

        /// <summary>
        /// Generates a quick hash to determine if the song directory or it's files has changed. Should match SongCore's hash.
        /// Does NOT equate to a Beat Saver hash.
        /// </summary>
        /// <param name="songDirectory"></param>
        /// <returns></returns>
        long QuickDirectoryHash(string songDirectory);
    }

    /// <summary>
    /// Stores the result of a beatmap hashing.
    /// </summary>
    public struct HashResult
    {
        /// <summary>
        /// Result of the hash.
        /// </summary>
        public HashResultType ResultType;

        /// <summary>
        /// Beatmap hash if the hashing was successful, null otherwise.
        /// </summary>
        public string? Hash;

        /// <summary>
        /// Any <see cref="Exception"/> thrown while hashing will be stored here.
        /// </summary>
        public Exception? Exception;

        /// <summary>
        /// Any warning or error messages.
        /// </summary>
        public string? Message;

        /// <summary>
        /// Creates a new <see cref="HashResult"/> with <see cref="HashResultType.Success"/>.
        /// </summary>
        /// <param name="hash"></param>
        public HashResult(string hash)
        {
            if(string.IsNullOrEmpty(hash))
                throw new ArgumentNullException(nameof(hash), "This constructor should only be used with a successful hash.");
            Hash = hash;
            ResultType = HashResultType.Success;
            Exception = null;
            Message = null;
        }

        /// <summary>
        /// Creates a new <see cref="HashResult"/>.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public HashResult(string? hash, string? message, Exception? exception)
        {
            if (hash == null || hash.Length == 0)
                Hash = null;
            else
                Hash = hash;
            if (hash == null)
                ResultType = HashResultType.Error;
            else if ((message != null && message.Length > 0) || exception != null)
                ResultType = HashResultType.Warn;
            else
                ResultType = HashResultType.Success;
            Exception = exception;
            Message = message;
        }
    }

    /// <summary>
    /// Type of hash result.
    /// </summary>
    public enum HashResultType
    {
        /// <summary>
        /// Beatmap was hashed successfully.
        /// </summary>
        Success,
        /// <summary>
        /// Beatmap was hashed with a warning.
        /// </summary>
        Warn,
        /// <summary>
        /// Beatmap could not be hashed.
        /// </summary>
        Error
    }
}
