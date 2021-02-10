using BeatSaber.SongHashing;
using BeatSaber.SongHashing.Legacy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

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
                var result = Hashers[i].HashDirectory(dir);
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
                var result = Hashers[i].HashDirectory(dir);
                Assert.AreEqual(HashResultType.Success, result.ResultType, Hashers[i].GetType().Name);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
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
                var result = Hashers[i].HashDirectory(dir);
                if (result.ResultType != HashResultType.Success)
                {
                    Console.WriteLine(result.Message);
                }
                else
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
            string expectedHash = "FF9FC9A9A11A575B7EFE7707F2F66AD9A92FE447".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashDirectory(dir);
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
                var result = Hashers[i].HashDirectory(dir);
                Assert.AreEqual(HashResultType.Error, result.ResultType, Hashers[i].GetType().Name);
                Assert.IsNull(result.Hash, Hashers[i].GetType().Name);
                Assert.IsNotNull(result.Message);
                Console.WriteLine(result.Message);
            }
        }
    }
}
