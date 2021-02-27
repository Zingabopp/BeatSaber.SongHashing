using BeatSaber.SongHashing;
using BeatSaber.SongHashing.Legacy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;

namespace HashingTests
{
    [TestClass]
    public class ZippedHashingTests
    {
        internal static readonly char s = Path.DirectorySeparatorChar;
        internal static readonly string WorkspaceDir = Path.GetFullPath($"..{s}..{s}..{s}..{s}");
        internal static readonly string DataFolder = Path.Combine(WorkspaceDir, "ReadOnlyData", "ZippedBeatmaps");

        Hasher[] Hashers = new Hasher[]
        {
            new Hasher()
        };

        [TestMethod]
        public void SingleDifficulty()
        {
            string zip = Path.Combine(DataFolder, "29.zip");
            string expectedHash = "c1c8e2b9394050afad435608137941da0b64b8f3".ToUpper();
            Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashZippedBeatmap(zip, CancellationToken.None);
                Assert.AreEqual(HashResultType.Success, result.ResultType, Hashers[i].GetType().Name);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
            }
        }

        [TestMethod]
        public void MultipleDifficulties()
        {
            string zip = Path.Combine(DataFolder, "5d02.zip");
            string expectedHash = "d6f3f15484fe169f4593718f50ef6d049fcaa72e".ToUpper();
            Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashZippedBeatmap(zip, CancellationToken.None);
                Assert.AreEqual(HashResultType.Success, result.ResultType, Hashers[i].GetType().Name);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
            }
        }

        [TestMethod]
        public void MismatchedDifficultyCase()
        {
            string zip = Path.Combine(DataFolder, "MismatchedCase.zip");
            string expectedHash = "d6f3f15484fe169f4593718f50ef6d049fcaa72e".ToUpper();
            Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashZippedBeatmap(zip, CancellationToken.None);
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
            string zip = Path.Combine(DataFolder, "Missing-Expected-Diff.zip");
            string expectedHash = "EF1A4AC10D2E271D6B95D7FEB773D1F387F28525".ToUpper();
            Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashZippedBeatmap(zip, CancellationToken.None);
                Assert.AreEqual(HashResultType.Warn, result.ResultType, Hashers[i].GetType().Name, $"Actual: {result.ResultType} | {result.Message} | {result.Exception?.ToString() ?? "No Exception"}");
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
                Assert.IsNotNull(result.Message);
                Console.WriteLine(result.Message);
            }
        }

        [TestMethod]
        public void NoInfo()
        {
            string zip = Path.Combine(DataFolder, "Missing-Info.zip");
            string expectedHash = null;
            Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashZippedBeatmap(zip, CancellationToken.None);
                Assert.AreEqual(HashResultType.Error, result.ResultType, Hashers[i].GetType().Name);
                Assert.IsNull(result.Hash, Hashers[i].GetType().Name);
                Assert.IsNotNull(result.Message);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
                Console.WriteLine(result.Message);
            }
        }

        [TestMethod]
        public void InvalidInfoJson()
        {
            string zip = Path.Combine(DataFolder, "InvalidInfoJson.zip");
            string expectedHash = null;
            Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = Hashers[i].HashZippedBeatmap(zip, CancellationToken.None);
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
