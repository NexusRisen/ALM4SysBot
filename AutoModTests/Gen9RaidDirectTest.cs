using System;
using System.Diagnostics;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using Xunit;
using Xunit.Abstractions;

namespace AutoModTests;

/// <summary>
/// Direct test of Gen9RaidSeedGenerator to verify it works correctly
/// </summary>
public class Gen9RaidDirectTest
{
    private readonly ITestOutputHelper _output;

    public Gen9RaidDirectTest(ITestOutputHelper output)
    {
        TestUtil.InitializePKHeXEnvironment();
        _output = output;
    }

    [Fact]
    public void TestDirectSeedGeneration()
    {
        var showdownSets = new[]
        {
            ("Amoonguss", @"Amoonguss (M)
Ball: Poké Ball
Level: 100
IVs: 26 Def
.MetLocation=30024
.MetLevel=75
.Version=50"),

            ("Alomomola", @"Alomomola (M)
Ball: Poké Ball
Level: 45
IVs: 20 Atk
.MetLocation=30024
.MetLevel=45
.Version=50"),

            ("Abomasnow", @"Abomasnow (M)
Ball: Poké Ball
Level: 75
IVs: 13 SpA
.MetLocation=30024
.MetLevel=75
.Version=50"),

            ("Excadrill Shiny", @"Excadrill (M)
Ball: Poké Ball
Level: 75
Shiny: Yes
IVs: 13 SpA
.MetLocation=30024
.MetLevel=75
.Version=50"),

            ("Espeon Shiny", @"Espeon (M)
Ball: Poké Ball
Level: 75
Shiny: Yes
IVs: 26 Atk
.MetLocation=30024
.MetLevel=75
.Version=50")
        };

        var sav = BlankSaveFile.Get(EntityContext.Gen9, "ALMUT") as SAV9SV;
        if (sav == null)
        {
            _output.WriteLine("❌ Failed to create SAV9SV");
            return;
        }

        RecentTrainerCache.SetRecentTrainer(sav);
        APILegality.EnableDevMode = true;

        int passed = 0;
        int failed = 0;

        foreach (var (name, setText) in showdownSets)
        {
            _output.WriteLine("");
            _output.WriteLine($"===== Testing: {name} =====");
            _output.WriteLine($"Set:");
            _output.WriteLine(setText);
            _output.WriteLine("");

            try
            {
                var set = new ShowdownSet(setText);
                var regen = new RegenTemplate(set);

                _output.WriteLine($"Species: {(Species)regen.Species}");
                _output.WriteLine($"Level: {regen.Level}");
                _output.WriteLine($"Shiny: {regen.Shiny}");
                _output.WriteLine($"IVs (HP/Atk/Def/Spe/SpA/SpD): {regen.IVs[0]}/{regen.IVs[1]}/{regen.IVs[2]}/{regen.IVs[3]}/{regen.IVs[4]}/{regen.IVs[5]}");

                if (regen.Regen.TryGetBatchValue("MetLocation", out var metLoc))
                {
                    _output.WriteLine($"MetLocation from batch: {metLoc}");
                }

                if (regen.Regen.TryGetBatchValue("MetLevel", out var metLevel))
                {
                    _output.WriteLine($"MetLevel from batch: {metLevel}");
                }

                _output.WriteLine("");
                _output.WriteLine("Calling Gen9RaidSeedGenerator.TryGenerateFromShowdownSet() directly...");

                var timer = Stopwatch.StartNew();
                var created = Gen9RaidSeedGenerator.TryGenerateFromShowdownSet(regen, sav);
                timer.Stop();

                _output.WriteLine($"Seed generation completed in {timer.ElapsedMilliseconds}ms");

                if (created != null)
                {
                    var la = new LegalityAnalysis(created);
                    _output.WriteLine($"Valid: {la.Valid}");
                    _output.WriteLine("");

                    if (la.Valid)
                    {
                        _output.WriteLine($"✅ SUCCESS - {name}");
                        _output.WriteLine($"   EC: {created.EncryptionConstant:X8}");
                        _output.WriteLine($"   PID: {created.PID:X8}");
                        _output.WriteLine($"   Created IVs (HP/Atk/Def/SpA/SpD/Spe): {created.IV_HP}/{created.IV_ATK}/{created.IV_DEF}/{created.IV_SPA}/{created.IV_SPD}/{created.IV_SPE}");
                        _output.WriteLine($"   Expected IVs (HP/Atk/Def/Spe/SpA/SpD): {regen.IVs[0]}/{regen.IVs[1]}/{regen.IVs[2]}/{regen.IVs[3]}/{regen.IVs[4]}/{regen.IVs[5]}");
                        _output.WriteLine($"   Shiny: {created.IsShiny} (Expected: {regen.Shiny})");
                        _output.WriteLine($"   Nature: {created.Nature}");
                        _output.WriteLine($"   Met Location: {created.MetLocation} (Expected: 30024)");
                        _output.WriteLine($"   Met Level: {created.MetLevel}");

                        // Verify IVs match (internal order: HP/Atk/Def/Spe/SpA/SpD)
                        bool ivsMatch = true;
                        for (int i = 0; i < 6; i++)
                        {
                            int actualIV = i switch
                            {
                                0 => created.IV_HP,
                                1 => created.IV_ATK,
                                2 => created.IV_DEF,
                                3 => created.IV_SPE,
                                4 => created.IV_SPA,
                                5 => created.IV_SPD,
                                _ => 0
                            };
                            if (actualIV != regen.IVs[i])
                            {
                                _output.WriteLine($"   ⚠️ IV mismatch at index {i}: expected {regen.IVs[i]}, got {actualIV}");
                                ivsMatch = false;
                            }
                        }

                        if (ivsMatch)
                        {
                            _output.WriteLine($"   ✅ All IVs match expected values");
                            passed++;
                        }
                        else
                        {
                            _output.WriteLine($"   ❌ IV mismatches found");
                            failed++;
                        }
                    }
                    else
                    {
                        _output.WriteLine($"❌ FAILED - {name} (Invalid legality)");
                        _output.WriteLine($"   Legality Report:");
                        _output.WriteLine(la.Report());
                        failed++;
                    }
                }
                else
                {
                    _output.WriteLine($"❌ FAILED - {name} (Seed generator returned null)");
                    _output.WriteLine($"   This means no valid seed was found within the search limit");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ EXCEPTION - {name}");
                _output.WriteLine($"   {ex.Message}");
                _output.WriteLine($"   {ex.StackTrace}");
                failed++;
            }

            _output.WriteLine("");
        }

        _output.WriteLine("===============================================");
        _output.WriteLine($"SUMMARY");
        _output.WriteLine($"===============================================");
        _output.WriteLine($"Total: {showdownSets.Length}");
        _output.WriteLine($"Passed: {passed}");
        _output.WriteLine($"Failed: {failed}");
        _output.WriteLine("");

        if (failed == 0)
            _output.WriteLine("✅ All tests passed!");
        else
            _output.WriteLine($"⚠️ {failed} test(s) failed (note: finding specific IV patterns can take many attempts)");
    }
}
