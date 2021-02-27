using BeatSaber.SongHashing;
using BeatSaber.SongHashing.Legacy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace HashingTests
{
    [TestClass]
    public class HashingTests
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
        public void SingleDifficulty()
        {
            string dir = Path.Combine(DataFolder, "29");
            string expectedHash = "c1c8e2b9394050afad435608137941da0b64b8f3".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashDirectory(dir, CancellationToken.None);
                Assert.AreEqual(HashResultType.Success, result.ResultType, Hashers[i].GetType().Name);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
            }
        }

        [TestMethod]
        public void MultipleDifficulties()
        {
            string dir = Path.Combine(DataFolder, "5d02");
            string expectedHash = "d6f3f15484fe169f4593718f50ef6d049fcaa72e".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashDirectory(dir, CancellationToken.None);
                Assert.AreEqual(HashResultType.Success, result.ResultType, Hashers[i].GetType().Name);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
            }
        }

        [TestMethod]
        public void MultipleValidBeatmaps()
        {
            Dictionary<string, string> dirHashes = new Dictionary<string, string>()
            {
                { "2cd", "EA2D289FB640CE8A0D7302AE36BFA3A5710D9EE8" },
                {"5d02", "D6F3F15484FE169F4593718F50EF6D049FCAA72E" },
                {"5d8d", "310694F2FF8D129D4E64192251653CAFFDC65B62" }, // *B62
                {"5dbf", "05FF1B7ECDD5089E5EAFC1D8474680E448A017DD" }, // *7dd
                {"29", "C1C8E2B9394050AFAD435608137941DA0B64B8F3" }
                //{"29-2", "5444F9070133CB10EDED3C676FDEFA9428655115" }
            };
            foreach (var pair in dirHashes)
            {
                string dir = Path.Combine(DataFolder, pair.Key);
                string expectedHash = pair.Value?.ToUpper() ?? string.Empty;
                Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
                for (int i = 0; i < Hashers.Length; i++)
                {
                    var result = Hashers[i].HashDirectory(dir, CancellationToken.None);
                    Assert.AreEqual(HashResultType.Success, result.ResultType, Hashers[i].GetType().Name);
                    Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
                }
            }
        }

        [TestMethod]
        public void MismatchedDifficultyCase()
        {
            string dir = Path.Combine(DataFolder, "MismatchedCase");
            string expectedHash = "d6f3f15484fe169f4593718f50ef6d049fcaa72e".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashDirectory(dir, CancellationToken.None);
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
        public void MissingDifficulty()
        {
            string dir = Path.Combine(DataFolder, "Missing-Expected-Diff");
            string expectedHash = "EF1A4AC10D2E271D6B95D7FEB773D1F387F28525".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashDirectory(dir, CancellationToken.None);
                Assert.AreEqual(HashResultType.Warn, result.ResultType, Hashers[i].GetType().Name);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
                Assert.IsNotNull(result.Message);
                Console.WriteLine(result.Message);
            }
        }

        [TestMethod]
        public void NoInfo()
        {
            string dir = Path.Combine(DataFolder, "Missing-Info");
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashDirectory(dir, CancellationToken.None);
                Assert.AreEqual(HashResultType.Error, result.ResultType, Hashers[i].GetType().Name);
                Assert.IsNull(result.Hash, Hashers[i].GetType().Name);
                Assert.IsNotNull(result.Message);
                Console.WriteLine(result.Message);
            }
        }

        [TestMethod]
        public void InvalidInfoJson()
        {
            string dir = Path.Combine(DataFolder, "InvalidInfoJson");
            string expectedHash = null;
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashDirectory(dir, CancellationToken.None);
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
