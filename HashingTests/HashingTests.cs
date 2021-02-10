using BeatSaber.SongHashing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace HashingTests
{
    [TestClass]
    public class HashingTests
    {
        internal static readonly char s = Path.DirectorySeparatorChar;
        internal static readonly string WorkspaceDir = Path.GetFullPath($"..{s}..{s}..{s}..{s}");
        internal static readonly string DataFolder = Path.Combine(WorkspaceDir, "ReadOnlyData");

        IBeatmapHasher DefaultHasher = new Hasher();

        [TestMethod]
        public void SingleDifficulty()
        {
            string dir = Path.Combine(DataFolder, "29");
            string expectedHash = "c1c8e2b9394050afad435608137941da0b64b8f3".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            var hash = DefaultHasher.HashDirectory(dir);
            Assert.AreEqual(expectedHash, hash);
        }

        [TestMethod]
        public void MultipleDifficulties()
        {
            string dir = Path.Combine(DataFolder, "5d02");
            string expectedHash = "d6f3f15484fe169f4593718f50ef6d049fcaa72e".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            var hash = DefaultHasher.HashDirectory(dir);
            Assert.AreEqual(expectedHash, hash);
        }

        [TestMethod]
        public void MismatchedDifficultyCase()
        {
            string dir = Path.Combine(DataFolder, "MismatchedCase");
            string expectedHash = "d6f3f15484fe169f4593718f50ef6d049fcaa72e".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            var hash = DefaultHasher.HashDirectory(dir);
            Assert.AreEqual(expectedHash, hash);
        }

        [TestMethod]
        public void MissingDifficulty()
        {
            string dir = Path.Combine(DataFolder, "Missing-Expected-Diff");
            string expectedHash = "FF9FC9A9A11A575B7EFE7707F2F66AD9A92FE447".ToUpper();
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            var hash = DefaultHasher.HashDirectory(dir);
            Assert.AreEqual(expectedHash, hash);
        }

        [TestMethod]
        public void NoInfo()
        {
            string dir = Path.Combine(DataFolder, "Missing-Info");
            Assert.IsTrue(Directory.Exists(dir), $"Could not find '{dir}'");
            var hash = DefaultHasher.HashDirectory(dir);
            Assert.IsNull(hash);
        }
    }
}
