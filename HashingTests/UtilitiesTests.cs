using BeatSaber.SongHashing;
using BeatSaber.SongHashing.Legacy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HashingTests
{
    [TestClass]
    public class UtilitiesTests
    {
        internal static readonly char s = Path.DirectorySeparatorChar;
        internal static readonly string WorkspaceDir = Path.GetFullPath($"..{s}..{s}..{s}..{s}");
        internal static readonly string DataFolder = Path.Combine(WorkspaceDir, "ReadOnlyData");

        IBeatmapHasher DefaultHasher = new LegacyHasher();

        [TestMethod]
        public void SingleDifficulty()
        {
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(DataFolder, "29"));
            string infoFile = Path.Combine(dir.FullName, "info.dat");
            JObject obj = JObject.Parse(File.ReadAllText(infoFile));
            string[] diffFiles = Utilities.GetDifficultyFileNames(obj).ToArray();
            string[] expectedFiles = dir.GetFiles("*.dat").Select(f => f.Name).ToArray();
            CheckFiles(expectedFiles, diffFiles);
        }

        [TestMethod]
        public void MultipleSets()
        {
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(DataFolder, "MultipleSets"));
            Assert.IsTrue(dir.Exists);
            string infoFile = Path.Combine(dir.FullName, "info.dat");
            JObject obj = JObject.Parse(File.ReadAllText(infoFile));
            string[] diffFiles = Utilities.GetDifficultyFileNames(obj).ToArray();
            string[] expectedFiles = dir.GetFiles("*.dat").Select(f => f.Name).ToArray();
            CheckFiles(expectedFiles, diffFiles);
        }

        private void CheckFiles(string[] expectedFiles, string[] actualFiles)
        {

            for (int i = 0; i < expectedFiles.Length; i++)
            {
                if (expectedFiles[i] == "info.dat")
                    continue;
                Assert.IsTrue(actualFiles.Contains(expectedFiles[i]), $"Expected file '{expectedFiles[i]}'");
            }
        }
    }
}
