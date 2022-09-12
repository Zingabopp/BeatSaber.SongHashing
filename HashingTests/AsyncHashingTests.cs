using BeatSaber.SongHashing;
using BeatSaber.SongHashing.Legacy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HashingTests
{
    [TestClass]
    public class AsyncHashingTests
    {
        internal static readonly char s = Path.DirectorySeparatorChar;
        internal static readonly string WorkspaceDir = Path.GetFullPath($"..{s}..{s}..{s}..{s}");
        internal static readonly string DataFolder = Path.Combine(WorkspaceDir, "ReadOnlyData");

        IBeatmapHasher[] Hashers = new IBeatmapHasher[]
        {
            new Hasher(),
            new LegacyHasher()
        };

        [TestMethod]
        public async Task SingleDifficulty()
        {
            string dir = Path.Combine(DataFolder, "29");
            string expectedHash = "5444F9070133CB10EDED3C676FDEFA9428655115".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = await Hashers[i].HashDirectoryAsync(dir, CancellationToken.None);
                Assert.AreEqual(HashResultType.Success, result.ResultType, Hashers[i].GetType().Name);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
            }
        }

        [TestMethod]
        public async Task MultipleDifficulties()
        {
            string dir = Path.Combine(DataFolder, "5d02");
            string expectedHash = "A955A84C6974761F5E1600998C7EC202DB7810B1".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = await Hashers[i].HashDirectoryAsync(dir, CancellationToken.None);
                Assert.AreEqual(HashResultType.Success, result.ResultType, Hashers[i].GetType().Name);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
            }
        }

        [TestMethod]
        public async Task MultipleValidBeatmaps()
        {
            Dictionary<string, string> dirHashes = new Dictionary<string, string>()
            {
                { "2cd", "7012E0B9F32A542B3AA4C271D7A963D974948E85" },
                {"5d02", "A955A84C6974761F5E1600998C7EC202DB7810B1" },
                {"5d8d", "0D342446F1E21BD52B9F1F2E94A862CEA03E7BCA" }, // *B62
                {"5dbf", "3E51494DFC52CE39272A656B0FA99A8E5DDB3FA8" }, // *7dd
                {"29", "5444F9070133CB10EDED3C676FDEFA9428655115" }
                //{"29-2", "5444F9070133CB10EDED3C676FDEFA9428655115" }
            };
            foreach (var pair in dirHashes)
            {
                string dir = Path.Combine(DataFolder, pair.Key);
                string expectedHash = pair.Value?.ToUpper() ?? string.Empty;
                Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
                for (int i = 0; i < Hashers.Length; i++)
                {
                    var result = await Hashers[i].HashDirectoryAsync(dir, CancellationToken.None);
                    Assert.AreEqual(HashResultType.Success, result.ResultType, Hashers[i].GetType().Name);
                    Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
                }
            }
        }

        [TestMethod]
        public async Task MismatchedDifficultyCase()
        {
            string dir = Path.Combine(DataFolder, "MismatchedCase");
            string expectedHash = "A955A84C6974761F5E1600998C7EC202DB7810B1".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = await Hashers[i].HashDirectoryAsync(dir, CancellationToken.None);
                if (result.Message != null)
                    Console.WriteLine(result.Message);
                if (result.ResultType == HashResultType.Success)
                {
                    Assert.AreEqual(HashResultType.Success, result.ResultType, Hashers[i].GetType().Name);
                    Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
                }
            }
        }

        [TestMethod]
        public async Task MissingDifficulty()
        {
            string dir = Path.Combine(DataFolder, "Missing-Expected-Diff");
            string expectedHash = "FF9FC9A9A11A575B7EFE7707F2F66AD9A92FE447".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = await Hashers[i].HashDirectoryAsync(dir, CancellationToken.None);
                if (result.Exception != null)
                    throw result.Exception;
                Assert.AreEqual(HashResultType.Warn, result.ResultType, Hashers[i].GetType().Name);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
                Assert.IsNotNull(result.Message);
                Console.WriteLine(result.Message);
            }
        }

        [TestMethod]
        public async Task NoInfo()
        {
            string dir = Path.Combine(DataFolder, "Missing-Info");
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = await Hashers[i].HashDirectoryAsync(dir, CancellationToken.None);
                Assert.AreEqual(HashResultType.Error, result.ResultType, Hashers[i].GetType().Name);
                Assert.IsNull(result.Hash, Hashers[i].GetType().Name);
                Assert.IsNotNull(result.Message);
                Console.WriteLine(result.Message);
            }
        }

        [TestMethod]
        public async Task InvalidInfoJson()
        {
            string dir = Path.Combine(DataFolder, "InvalidInfoJson");
            string expectedHash = null;
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = await Hashers[i].HashDirectoryAsync(dir, CancellationToken.None);
                Assert.AreEqual(HashResultType.Error, result.ResultType, Hashers[i].GetType().Name);
                Assert.IsNull(result.Hash, Hashers[i].GetType().Name);
                Assert.IsNotNull(result.Message);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
                Console.WriteLine(result.Message);
                Console.WriteLine(result.Exception.ToString() ?? "No Exception");
            }
        }
    }
}
