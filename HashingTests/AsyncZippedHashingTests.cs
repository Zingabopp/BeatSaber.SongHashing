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
    public class AsyncZippedHashingTests
    {
        internal static readonly char s = Path.DirectorySeparatorChar;
        internal static readonly string WorkspaceDir = Path.GetFullPath($"..{s}..{s}..{s}..{s}");
        internal static readonly string DataFolder = Path.Combine(WorkspaceDir, "ReadOnlyData", "ZippedBeatmaps");

        IBeatmapHasher[] Hashers = new IBeatmapHasher[]
        {
            new Hasher(),
            new LegacyHasher()
        };

        [TestMethod]
        public async Task SingleDifficulty()
        {
            string zip = Path.Combine(DataFolder, "29.zip");
            string expectedHash = "c1c8e2b9394050afad435608137941da0b64b8f3".ToUpper();
            Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = await Hashers[i].HashZippedBeatmapAsync(zip, CancellationToken.None);
                Assert.AreEqual(HashResultType.Success, result.ResultType, Hashers[i].GetType().Name);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
            }
        }

        [TestMethod]
        public async Task MultipleDifficulties()
        {
            string zip = Path.Combine(DataFolder, "5d02.zip");
            string expectedHash = "d6f3f15484fe169f4593718f50ef6d049fcaa72e".ToUpper();
            Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = await Hashers[i].HashZippedBeatmapAsync(zip, CancellationToken.None);
                Assert.AreEqual(HashResultType.Success, result.ResultType, Hashers[i].GetType().Name);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
            }
        }


        [TestMethod]
        public async Task MultipleValidBeatmaps()
        {
            Dictionary<string, string> zipHashes = new Dictionary<string, string>()
            {
                { "2cd.zip", "EA2D289FB640CE8A0D7302AE36BFA3A5710D9EE8" },
                {"5d02.zip", "D6F3F15484FE169F4593718F50EF6D049FCAA72E" },
                {"5d8d.zip", "310694F2FF8D129D4E64192251653CAFFDC65B62" }, // *B62
                {"5dbf.zip", "05FF1B7ECDD5089E5EAFC1D8474680E448A017DD" }, // *7dd
                {"29.zip", "C1C8E2B9394050AFAD435608137941DA0B64B8F3" },
                {"29-2.zip", "5444F9070133CB10EDED3C676FDEFA9428655115" }
            };
            foreach (var pair in zipHashes)
            {
                string zip = Path.Combine(DataFolder, pair.Key);
                string expectedHash = pair.Value?.ToUpper() ?? string.Empty;
                Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
                for (int i = 0; i < Hashers.Length; i++)
                {
                    var result = await Hashers[i].HashZippedBeatmapAsync(zip, CancellationToken.None);
                    Assert.AreEqual(HashResultType.Success, result.ResultType, $"{Hashers[i].GetType().Name}: {result.Exception}");
                    Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
                }
            }
        }

        [TestMethod]
        public async Task MismatchedDifficultyCase()
        {
            string zip = Path.Combine(DataFolder, "MismatchedCase.zip");
            string expectedHash = "d6f3f15484fe169f4593718f50ef6d049fcaa72e".ToUpper();
            Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = await Hashers[i].HashZippedBeatmapAsync(zip, CancellationToken.None);
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
            string zip = Path.Combine(DataFolder, "Missing-Expected-Diff.zip");
            string expectedHash = "EF1A4AC10D2E271D6B95D7FEB773D1F387F28525".ToUpper();
            Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = await Hashers[i].HashZippedBeatmapAsync(zip, CancellationToken.None);
                Assert.AreEqual(HashResultType.Warn, result.ResultType, Hashers[i].GetType().Name, $"Actual: {result.ResultType} | {result.Message} | {result.Exception?.ToString() ?? "No Exception"}");
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
                Assert.IsNotNull(result.Message);
                Console.WriteLine(result.Message);
            }
        }

        [TestMethod]
        public async Task NoInfo()
        {
            string zip = Path.Combine(DataFolder, "Missing-Info.zip");
            string expectedHash = null;
            Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = await Hashers[i].HashZippedBeatmapAsync(zip, CancellationToken.None);
                Assert.AreEqual(HashResultType.Error, result.ResultType, Hashers[i].GetType().Name);
                Assert.IsNull(result.Hash, Hashers[i].GetType().Name);
                Assert.IsNotNull(result.Message);
                Assert.AreEqual(expectedHash, result.Hash, Hashers[i].GetType().Name);
                Console.WriteLine(result.Message);
            }
        }

        [TestMethod]
        public async Task InvalidInfoJson()
        {
            string zip = Path.Combine(DataFolder, "InvalidInfoJson.zip");
            string expectedHash = null;
            Assert.IsTrue(File.Exists(zip), $"Could not find '{zip}'");
            for (int i = 0; i < Hashers.Length; i++)
            {
                var result = await Hashers[i].HashZippedBeatmapAsync(zip, CancellationToken.None);
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
