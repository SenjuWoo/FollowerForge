using FollowerForge.Domain;
using FollowerForge.SkyrimRecords;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace FollowerForge.Tests;

/// <summary>
/// A follower placed outdoors lives in the worldspace's persistent cell, so her plugin has to carry
/// the worldspace record as the parent of that cell. Through 3.7.11 it carried an empty one — no
/// EditorID, flags 0, zeroed map and bounds — which replaces WhiterunWorld (or Tamriel) for every
/// plugin loaded before hers. Shipped follower mods carry the whole record; so does this now.
/// </summary>
public sealed class WorldspaceOverrideTests
{
    private static readonly ILogger Log = new LoggerConfiguration().CreateLogger();

    private static FollowerProfile Profile() => new()
    {
        Name = "Test Follower",
        PluginName = "FF_TestFollower.esp",
        Race = new RecordRef(VanillaForms.NordRace.ToString()),
        VoiceType = new RecordRef(VanillaForms.FemaleEvenTonedVoice.ToString()),
        Class = new RecordRef(VanillaForms.CombatWarrior1HClass.ToString()),
        Placement = new PlacementSpec { Cell = new RecordRef(VanillaForms.WhiterunWorldPersistentCell.ToString()) },
    };

    private static Worldspace SourceWorldspace()
    {
        var source = new Worldspace(VanillaForms.WhiterunWorld, VanillaForms.Release)
        {
            EditorID = "WhiterunWorld",
            Flags = Worldspace.Flag.SmallWorld | Worldspace.Flag.NoGrass,
            DistantLodMultiplier = 0.25f,
            FormVersion = 40,
        };
        source.Location.SetTo(new FormKey(VanillaForms.Skyrim, 0x018A56));
        // The source's own contents must never be copied: that would duplicate every object in it.
        source.TopCell = new Cell(VanillaForms.WhiterunWorldPersistentCell, VanillaForms.Release);
        source.TopCell.Persistent.Add(new PlacedObject(new FormKey(VanillaForms.Skyrim, 0x02158F), VanillaForms.Release));
        return source;
    }

    [Fact]
    public void WorldspaceOverride_CarriesTheSourceRecord_NotABlankOne()
    {
        var source = SourceWorldspace();
        var result = new FollowerCompiler(Log).Compile(Profile(), location: null,
            worldspaceSource: key => key == source.FormKey ? source : null);

        var written = Assert.Single(result.Mod.Worldspaces);
        Assert.Equal("WhiterunWorld", written.EditorID);
        Assert.True(written.Flags.HasFlag(Worldspace.Flag.SmallWorld));
        Assert.True(written.Flags.HasFlag(Worldspace.Flag.NoGrass));
        Assert.Equal(0.25f, written.DistantLodMultiplier);
        Assert.Equal(new FormKey(VanillaForms.Skyrim, 0x018A56), written.Location.FormKey);
        // A new SE/AE plugin holds form version 44 records, whatever the master used.
        Assert.Equal(44, written.FormVersion);
    }

    [Fact]
    public void WorldspaceOverride_CopiesNoneOfTheSourceContents()
    {
        var source = SourceWorldspace();
        var result = new FollowerCompiler(Log).Compile(Profile(), location: null,
            worldspaceSource: _ => source);

        var written = Assert.Single(result.Mod.Worldspaces);
        var placed = Assert.Single(written.TopCell!.Persistent);
        Assert.Equal(result.PlacedFormKey, placed.FormKey);
        Assert.Empty(written.SubCells);
    }

    [Fact]
    public void Validator_WarnsWhenTheWorldspaceCouldNotBeRead()
    {
        var result = new FollowerCompiler(Log).Compile(Profile(), location: null);

        var report = new ValidationReport();
        FollowerValidator.Validate(result, Profile(), report);

        Assert.Contains(report.Findings, f =>
            f.Code == "WORLDSPACE_OVERRIDE_BLANK" && f.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void Validator_IsQuietWhenTheWorldspaceIsComplete()
    {
        var source = SourceWorldspace();
        var result = new FollowerCompiler(Log).Compile(Profile(), location: null,
            worldspaceSource: _ => source);

        var report = new ValidationReport();
        FollowerValidator.Validate(result, Profile(), report);

        Assert.DoesNotContain(report.Findings, f => f.Code == "WORLDSPACE_OVERRIDE_BLANK");
    }
}
