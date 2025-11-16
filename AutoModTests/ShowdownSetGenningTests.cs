using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using Xunit;

namespace AutoModTests;

public static class ShowdownSetGenningTests
{
    static ShowdownSetGenningTests() => TestUtil.InitializePKHeXEnvironment();

    [Theory]
    [InlineData(GameVersion.US, Meowstic)]
    [InlineData(GameVersion.US, Darkrai)]
    [InlineData(GameVersion.B2, Genesect)]
    [InlineData(GameVersion.ZA, Xerneas)]
    public static void VerifyManually(GameVersion game, string txt)
    {
        var dev = APILegality.EnableDevMode;
        APILegality.EnableDevMode = true;

        var sav = BlankSaveFile.Get(game, "ALM");
        TrainerSettings.Register(sav);

        var trainer = TrainerSettings.GetSavedTrainerData(game.GetGeneration(), game);
        RecentTrainerCache.SetRecentTrainer(trainer);

        var set = new ShowdownSet(txt);
        var almres = sav.GetLegalFromSet(set);
        APILegality.EnableDevMode = dev;

        var la = new LegalityAnalysis(almres.Created);
        la.Valid.Should().BeTrue();
    }

    private const string Darkrai =
        @"Darkrai
IVs: 7 Atk
Ability: Bad Dreams
Shiny: Yes
Timid Nature
- Hypnosis
- Feint Attack
- Nightmare
- Double Team";

    private const string Genesect =
        @"Genesect
Ability: Download
Shiny: Yes
Hasty Nature
- Extreme Speed
- Techno Blast
- Blaze Kick
- Shift Gear";

    private const string Meowstic =
        @"Meowstic-F @ Life Orb
Ability: Competitive
EVs: 4 Def / 252 SpA / 252 Spe
Timid Nature
- Psyshock
- Signal Beam
- Hidden Power Ground
- Calm Mind";

    private const string Xerneas =
        @"Xerneas @ Life Orb
Ball: Moon Ball
Level: 75
Shiny: No
OT: Benyamin
TID: 689563
SID: 3536
OTGender: Male
Language: English
EVs: 6 HP / 252 SpA / 252 Spe
Modest Nature
.MetLocation=210
.MetLevel=75
.Version=52
.MetDate=20251030";
}
