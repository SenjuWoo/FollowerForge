using FollowerForge.AssetIndex;
using FollowerForge.BuildPipeline;
using FollowerForge.Domain;
using FollowerForge.SkyrimRecords;
using Serilog;

namespace FollowerForge.Tests;

/// <summary>
/// The head-part audit exists because GitHub issue #2 ("hair below the head") could not be
/// diagnosed from a screenshot: no build had ever stated which hair it wrote. These tests pin the
/// two arrangements the game cannot honour — two parts for one slot, and no Hair part at all —
/// and the slot labels a report is read by.
/// </summary>
public sealed class HeadPartAuditTests
{
    private static readonly ILogger Log = new LoggerConfiguration().CreateLogger();

    /// <summary>One head part per slot given, with a plausible FormID for each.</summary>
    private static AppearanceSpec Appearance(params int[] slots) => new()
    {
        HeadParts = slots
            .Select((_, index) => new RecordRef($"{0x800 + index:X6}:Hair.esp"))
            .ToList(),
        HeadPartTypes = slots,
    };

    [Fact]
    public void Each_head_part_is_reported_by_slot_and_form_id()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadParts(
            Appearance(0, 3), catalog: null, report);

        var infos = report.Findings
            .Where(f => f.Code == "PRESET_HEADPART" && f.Severity == ValidationSeverity.Info)
            .ToList();
        Assert.Equal(2, infos.Count);
        Assert.Contains(infos, f => f.Message.StartsWith("Hair = 000801:Hair.esp", StringComparison.Ordinal));
        Assert.Contains(infos, f => f.Message.StartsWith("Misc = 000800:Hair.esp", StringComparison.Ordinal));
    }

    [Fact]
    public void Two_parts_for_one_slot_are_flagged_because_the_game_drops_all_but_the_first()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadParts(Appearance(3, 3), catalog: null, report);

        var warning = Assert.Single(
            report.Findings, f => f.Code == "PRESET_HEADPART_DUPLICATE");
        Assert.Equal(ValidationSeverity.Warning, warning.Severity);
        Assert.Contains("Hair", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_preset_with_parts_but_no_hair_is_flagged_as_bald()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadParts(Appearance(1, 2, 6), catalog: null, report);

        Assert.Contains(report.Findings, f => f.Code == "PRESET_NO_HAIR_PART");
    }

    [Fact]
    public void A_preset_with_a_hair_part_is_not_flagged_as_bald()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadParts(Appearance(2, 3, 6), catalog: null, report);

        Assert.DoesNotContain(report.Findings, f => f.Code == "PRESET_NO_HAIR_PART");
    }

    [Fact]
    public void An_appearance_with_no_head_parts_says_nothing()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadParts(new AppearanceSpec(), catalog: null, report);

        Assert.Empty(report.Findings);
    }

    [Fact]
    public void A_head_part_restricted_to_another_race_is_named()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(
            Appearance(3), npcRace: "013746:Skyrim.esm",
            key => new HeadPartFacts("FemaleHairKapotun01", "Hair", "hair.nif",
                "000800:DWKaPoTunRace.esp", "KaPoTunRace"),
            report);

        var warning = Assert.Single(report.Findings, f => f.Code == "PRESET_HEADPART_OTHER_RACE");
        Assert.Equal(ValidationSeverity.Warning, warning.Severity);
        Assert.Contains("KaPoTunRace", warning.Message, StringComparison.Ordinal);
        Assert.Contains("FemaleHairKapotun01", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_head_part_with_no_race_restriction_is_not_flagged()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(
            Appearance(3), npcRace: "013746:Skyrim.esm",
            key => new HeadPartFacts("AnyHair", "Hair", "hair.nif", null, null),
            report);

        Assert.Empty(report.Findings);
    }

    [Fact]
    public void A_head_part_restricted_to_her_own_race_is_not_flagged()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(
            Appearance(3), npcRace: "013746:Skyrim.esm",
            key => new HeadPartFacts("NordHair", "Hair", "hair.nif", "013746:Skyrim.esm", "NordRace"),
            report);

        Assert.Empty(report.Findings);
    }

    [Fact]
    public void An_unrestricted_hair_is_called_out_when_the_preset_belongs_to_another_race()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(
            Appearance(1, 3), npcRace: "013746:Skyrim.esm",
            key => key.StartsWith("000800", StringComparison.Ordinal)
                ? new HeadPartFacts("FemaleHeadKapotun", "Face", "head.nif",
                    "00083D:DWKaPoTunRace.esp", "KaPoTunRace")
                : new HeadPartFacts("FemaleHairKapotun01", "Hair",
                    @"actors\character\DWRaces\TigerKhajiit\Hair\FemaleHairKapotun01.nif", null, null),
            report);

        var hair = Assert.Single(report.Findings, f => f.Code == "PRESET_HAIR_OTHER_RACE");
        Assert.Contains("FemaleHairKapotun01", hair.Message, StringComparison.Ordinal);
        Assert.Contains("TigerKhajiit", hair.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unrestricted_hair_on_its_own_is_not_flagged()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(
            Appearance(3), npcRace: "013746:Skyrim.esm",
            key => new HeadPartFacts("AnyHair", "Hair", "hair.nif", null, null),
            report);

        Assert.Empty(report.Findings);
    }

    /// <summary>
    /// The slot and the body-shape count come out of the same read. Splitting them is what made a
    /// build open and parse every preset twice.
    /// </summary>
    [Fact]
    public void Preset_read_returns_head_part_slots_and_the_body_shape_count_together()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ff_headparts_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "Slotted.jslot");
        try
        {
            File.WriteAllText(path, """
                {
                  "headParts": [
                    { "formIdentifier": "Skyrim.esm|000123", "formId": 291,  "type": 3 },
                    { "formIdentifier": "Skyrim.esm|000456", "formId": 1110, "type": 6 }
                  ],
                  "bodyMorphs": [
                    { "name": "HipBone",  "keys": [ { "key": "OBody", "value": 0.659 } ] },
                    { "name": "Untouched", "keys": [ { "key": "OBody", "value": 0.0 } ] }
                  ]
                }
                """);

            var appearance = new CharGenDiscovery(Log).ReadJslotAppearance(path);

            Assert.NotNull(appearance);
            Assert.Equal(new[] { 3, 6 }, appearance!.HeadPartTypes);
            Assert.Equal(2, appearance.HeadParts.Count);
            Assert.Equal(1, appearance.BodyMorphCount);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
