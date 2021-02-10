using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace BeatSaber.SongHashing
{
    /// <summary>
    /// Utilities used for song hashing.
    /// </summary>
    public static class Utilities
    {
        private const string DifficultyBeatmapSetsPropertyName = "_difficultyBeatmapSets";
        private const string DifficultyBeatmapsPropertyName = "_difficultyBeatmaps";
        private const string BeatmapFilePropertyName = "_beatmapFilename";

        //public static string[] GetDifficultyFileNames(JObject infoFile)
        //{
        //    var sets = infoFile[DifficultyBeatmapSetsPropertyName] as JArray
        //        ?? throw new JsonException($"info file did not have a '{DifficultyBeatmapSetsPropertyName}' property");
        //    return sets
        //        .Where(s => s[DifficultyBeatmapsPropertyName] is JArray)
        //        .Select(s => s[DifficultyBeatmapsPropertyName] as JArray)
        //        .SelectMany(a => a.Select(d => (string)d[BeatmapFilePropertyName]))
        //        .Where(d => d != null)
        //        .ToArray();
        //}

        /// <summary>
        /// Enumerates over the list of difficulty files from the <see cref="JObject"/> of an 'info.dat' file.
        /// </summary>
        /// <param name="infoFile"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetDifficultyFileNames(JObject infoFile)
        {
            JArray? sets = infoFile[DifficultyBeatmapSetsPropertyName] as JArray
                ?? throw new JsonException($"info file did not have a '{DifficultyBeatmapSetsPropertyName}' property");

            foreach (JToken? set in sets)
            {
                JArray? diffs = set[DifficultyBeatmapsPropertyName] as JArray;
                if (diffs != null)
                {
                    foreach (JToken? diff in diffs)
                    {
                        string? file = diff[BeatmapFilePropertyName]?.Value<string>();
                        if (file == null)
                            continue;
                        yield return file;
                    }
                }
            }
        }

        /// <summary>
        /// Generates a SHA1 hash from a byte array.
        /// </summary>
        /// <param name="input">Byte array to hash.</param>
        /// <returns>Sha1 hash of the byte array.</returns>
        public static string CreateSha1FromBytes(byte[] input)
        {
            using SHA1 sha1 = SHA1.Create();
            byte[] inputBytes = input;
            byte[] hashBytes = sha1.ComputeHash(inputBytes);
            //return BitConverter.ToString(hashBytes).Replace("-", "");
            return HexMate.Convert.ToHexString(hashBytes);
        }

        /// <summary>
        /// Generates a SHA1 hash from a Stream.
        /// </summary>
        /// <param name="input">Stream to hash.</param>
        /// <returns>Sha1 hash of the Stream.</returns>
        public static string CreateSha1FromStream(Stream input)
        {
            using SHA1 sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(input);
            return HexMate.Convert.ToHexString(hashBytes);
        }

        /// <summary>
        /// Sums the char value of each character in the given string (unchecked).
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int SumCharacters(string str)
        {
            unchecked
            {
                int charSum = 0;
                for (int i = 0; i < str.Length; i++)
                {
                    charSum += str[i];
                }
                return charSum;
            }
        }
    }
}
