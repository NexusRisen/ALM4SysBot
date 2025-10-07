using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System.Text;
using Xunit;
using static PKHeX.Core.GameVersion;

namespace AutoModTests;

/// <summary>
/// Class to test multiple pastebins of eggs to ensure they are parsed and regenerated correctly for each generation.
/// </summary>
public static class EggTests
{
    static EggTests() => TestUtil.InitializePKHeXEnvironment();

    private static string TestPath => TestUtil.GetTestFolder("ShowdownSets");
    private static string LogDirectory => Path.Combine(Directory.GetCurrentDirectory(), "logs");

    private static List<TestResult> RunVerification(string file, ReadOnlySpan<GameVersion> saves)
    {
        var results = new List<TestResult>(saves.Length);
        foreach (var s in saves)
        {
            var result = new TestResult(s);
            results.Add(result);
            var sav = BlankSaveFile.Get(s.GetContext(), "ALMUT");
            RecentTrainerCache.SetRecentTrainer(sav);

            var lines = File.ReadAllLines(file).Where(z => !z.StartsWith("====="));
            var sets = ShowdownParsing.GetShowdownSets(lines).ToList();

            for (int i = 0; i < sets.Count; i++)
            {
                var set = sets[i];
                if (set.Species == 0)
                    continue;

                try
                {
                    Debug.Write($"Checking Set {i:000} [Species: {(Species)set.Species}] from File {file} using Save {s}: ");
                    var created = sav.GenerateEgg(set, out var almres);
                    var la = new LegalityAnalysis(created);
                    if (almres is LegalizationResult.Regenerated && la.Valid)
                    {
                        Debug.WriteLine("Valid");
                        result.Legal.Add(set);
                    }
                    else
                    {
                        result.Failed.Add(new(set, la));
                        Debug.WriteLine($"Invalid Set for {(Species)set.Species} in file {file} with set: {set.Text}");
                    }
                }
                catch
                {
                    Debug.WriteLine($"Exception for {(Species)set.Species} in file {file} with set: {set.Text}");
                }
            }
        }
        return results;
    }

    [Theory]
    [InlineData(EggPK2, new[] { GSC })]
    [InlineData(EggPK3, new[] { R, S, E, FR, LG })]
    [InlineData(EggPK4, new[] { D, P, Pt, HG, SS })]
    [InlineData(EggPK5, new[] { B, W, B2, W2 })]
    [InlineData(EggPK6, new[] { X, Y, OR, AS })]
    [InlineData(EggPK7, new[] { SN, MN, US, UM })]
    [InlineData(EggPK8, new[] { SW, SH })]
    [InlineData(EggPB8, new[] { BD, SP })]
    [InlineData(EggPK9, new[] { SL, VL })]
    public static void VerifyFile(string path, GameVersion[] testversions)
    {
        Directory.CreateDirectory(LogDirectory);
        var full = Path.Combine(TestPath, path);
        var dev = APILegality.EnableDevMode;
        APILegality.EnableDevMode = true;

        var res = RunVerification(full, testversions);
        APILegality.EnableDevMode = dev;

        var msg = new StringBuilder();
        var error = new StringBuilder();
        var testfailed = false;
        foreach (var result in res)
        {
            var illegalcount = result.Failed.Count;
            if (illegalcount == 0)
                continue;

            testfailed = true;
            msg.AppendLine($"GameVersion {result.Version} : Illegal: {illegalcount} | Legal: {result.Legal.Count}");

            error.AppendLine($"=============== GameVersion: {result.Version} ===============");
            foreach (var f in result.Failed)
            {
                error.AppendLine(f.Set.Text);
                error.AppendLine(f.Result.Report());
                error.AppendLine();
            }
        }
        if (error.Length != 0)
        {
            var fileName = $"{Path.GetFileName(path).Replace('.', '_')}{DateTime.Now:_yyyy-MM-dd-HH-mm-ss}.log";
            var dest = Path.Combine(LogDirectory, fileName);
            File.WriteAllText(dest, error.ToString());
        }

        testfailed.Should().BeFalse(msg.ToString());
    }

    // Egg test file paths
    private const string EggPK2 = "Egg Tests/pk2.txt";
    private const string EggPK3 = "Egg Tests/pk3.txt";
    private const string EggPK4 = "Egg Tests/pk4.txt";
    private const string EggPK5 = "Egg Tests/pk5.txt";
    private const string EggPK6 = "Egg Tests/pk6.txt";
    private const string EggPK7 = "Egg Tests/pk7.txt";
    private const string EggPK8 = "Egg Tests/pk8.txt";
    private const string EggPB8 = "Egg Tests/pb8.txt";
    private const string EggPK9 = "Egg Tests/pk9.txt";

    public record TestResult(GameVersion Version)
    {
        public readonly List<FailedTest> Failed = [];
        public readonly List<ShowdownSet> Legal = [];
    }

    public record FailedTest(ShowdownSet Set, LegalityAnalysis Result);
}
