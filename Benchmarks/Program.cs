using System;
using System.Collections.Generic;
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
using Newtonsoft.Json.Linq;

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
        internal static readonly string BeatSaberDir = @"H:\SteamApps\SteamApps\common\Beat Saber";

        private readonly IBeatmapHasher LegacyBeatmapHasher = new LegacyHasher();
        private readonly IBeatmapHasher NewBeatmapHasher = new Hasher();
        //private string songDirectory = null!;
        private string[] songDirectories = Array.Empty<string>();

        (string path, HashResult result)[] NewHasherResults = Array.Empty<(string, HashResult)>();
        (string path, HashResult result)[] OldHasherResults = Array.Empty<(string, HashResult)>();

        [Params(1)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            //DirectoryInfo workspace = new DirectoryInfo(WorkspaceDir).Parent.Parent.Parent.Parent;
            //Console.WriteLine(workspace);
            string beatSaberSongs = Path.Combine(BeatSaberDir, @"Beat Saber_Data\CustomLevels");
            //songDirectory = Path.Combine(workspace.FullName, DataFolder, "5d8d");
            songDirectories = Directory.GetDirectories(beatSaberSongs);
            //Console.WriteLine(songDirectory);
            //if (!Directory.Exists(songDirectory))
            //    throw new DirectoryNotFoundException($"Could not find '{songDirectory}'");
        }

        [Benchmark]
        public void AsParallel()
        {
            for (int i = 0; i < N; i++)
            {
                NewHasherResults = songDirectories.AsParallel().Select(d => (d,NewBeatmapHasher.HashDirectory(d))).ToArray();
                //for (int j = 0; j < songDirectories.Length; j++)
                //    NewHasher.HashDirectory(songDirectories[j]);
            }
        }

        //[Benchmark]
        public void Serial()
        {
            for (int i = 0; i < N; i++)
            {
                (string, HashResult)[] results = new (string, HashResult)[songDirectories.Length];
                for(int j = 0; j < results.Length; j++)
                {
                    results[j] = (songDirectories[j], NewBeatmapHasher.HashDirectory(songDirectories[j]));
                }
                //for (int j = 0; j < songDirectories.Length; j++)
                //    NewHasher.HashDirectory(songDirectories[j]);
                NewHasherResults = results;
            }
        }

        //[Benchmark]
        public void LegacyHasher()
        {
            for (int i = 0; i < N; i++)
            {
                OldHasherResults = songDirectories.AsParallel().Select(d => (d, LegacyBeatmapHasher.HashDirectory(d))).ToArray();
                //for (int j = 0; j < songDirectories.Length; j++)
                //    Hasher.HashDirectory(songDirectories[j]);
            }
        }

        [GlobalCleanup]
        public void CheckResults()
        {
            Console.WriteLine($"----Results Analysis----");
            string hashDataPath = Path.Combine(BeatSaberDir, "UserData", "SongCore", "SongHashData.json");
            Dictionary<string, string> hashMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            JObject token = JObject.Parse(File.ReadAllText(hashDataPath));
            int noSongCore = 0;
            int nullSongDirectory = 0;
            int mismatchedHash = 0;
            foreach (var prop in token.Children().Select(t => (JProperty)t))
            {
                string? hash = ((JObject?)prop.Value)?["songHash"]?.Value<string>();
                if (hash != null)
                    hashMap[prop.Name] = hash;
                else
                    Console.WriteLine($"hash was null in {prop.Name}");
            }
            foreach (var result in NewHasherResults)
            {
                if (result.path == null)
                {
                    Console.WriteLine("Null SongDirectory in result.");
                    nullSongDirectory++;
                    continue;
                }
                if (result.result.ResultType != HashResultType.Success)
                    Console.WriteLine($"{result.result.ResultType}: {result.result.Message}");
                if (hashMap.TryGetValue(result.path, out string? songCoreHash))
                {
                    if (result.result.Hash != songCoreHash)
                    {
                        Console.WriteLine($"Mismatched hash in '{result.path}'");
                        mismatchedHash++;
                    }
                }
                else
                {
                    Console.WriteLine($"result for '{result.path}' does not have an entry in SongCore data.");
                    noSongCore++;
                }
            }
            Console.WriteLine($"\nStats:\n Song Directories Checked: {NewHasherResults.Length}\n   Null SongDirectories: {nullSongDirectory}\n   Mismatched Hashes: {mismatchedHash}\n   No SongCore Entry: {noSongCore}");
        }
    }
}
