using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BeatSaber.SongHashing;
using BeatSaber.SongHashing.Legacy;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public class Program
    {
        public const string MonoPath = @"C:\Program Files\Unity\2019.3.15f1\Editor\Data\MonoBleedingEdge\bin\mono.exe";


        public static void Main(string[] args)
        {
            Job[] jobs = new Job[]
            {
                Job.ShortRun.WithRuntime(ClrRuntime.Net461),
                Job.ShortRun.WithRuntime(CoreRuntime.Core50),
                Job.ShortRun.WithRuntime(new MonoRuntime("Mono", MonoPath))
            };
            for (int i = 0; i < jobs.Length; i++)
            {
                jobs[i] = jobs[i]
                            .WithIterationCount(2)
                            .WithMinWarmupCount(1)
                            .WithMaxWarmupCount(2);
            }
            var config = DefaultConfig.Instance;
            foreach (var job in jobs)
            {
                config = config.AddJob(job);
            }
            var summary = BenchmarkRunner.Run<HashTests>(config);
        }
    }

    [MemoryDiagnoser]
    public class HashTests
    {
        internal static readonly string WorkspaceDir = Path.GetFullPath(@"..\..\..\..\");
        internal static readonly string DataFolder = Path.Combine("HashingTests", "ReadOnlyData");

        private readonly IBeatmapHasher LegacyBeatmapHasher = new LegacyHasher();
        private readonly IBeatmapHasher NewBeatmapHasher = new Hasher();
        private string songDirectory = null!;
        private string[] songDirectories = Array.Empty<string>();
        [Params(1)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            //DirectoryInfo workspace = new DirectoryInfo(WorkspaceDir).Parent.Parent.Parent.Parent;
            //Console.WriteLine(workspace);
            string beatSaberDir = @"H:\SteamApps\SteamApps\common\Beat Saber\Beat Saber_Data\CustomLevels";
            //songDirectory = Path.Combine(workspace.FullName, DataFolder, "5d8d");
            songDirectories = Directory.GetDirectories(beatSaberDir);
            //Console.WriteLine(songDirectory);
            //if (!Directory.Exists(songDirectory))
            //    throw new DirectoryNotFoundException($"Could not find '{songDirectory}'");
        }

        [Benchmark]
        public void NewHasher()
        {
            for (int i = 0; i < N; i++)
            {
                songDirectories.AsParallel().ForAll(d => NewBeatmapHasher.QuickDirectoryHash(d));
                //for (int j = 0; j < songDirectories.Length; j++)
                //    NewHasher.HashDirectory(songDirectories[j]);
            }
        }

        [Benchmark]
        public void LegacyHasher()
        {
            for (int i = 0; i < N; i++)
            {
                songDirectories.AsParallel().ForAll(d => LegacyBeatmapHasher.QuickDirectoryHash(d));
                //for (int j = 0; j < songDirectories.Length; j++)
                //    Hasher.HashDirectory(songDirectories[j]);
            }
        }
    }
}
