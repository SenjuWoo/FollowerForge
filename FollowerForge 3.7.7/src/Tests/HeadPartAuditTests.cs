using FollowerForge.AssetIndex;
using FollowerForge.BuildPipeline;
using FollowerForge.Domain;
using FollowerForge.SkyrimRecords;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace FollowerForge.Tests;

/// <summary>
/// The head-part audit exists because GitHub issue #2 ("hair below the head") could not be
/// diagnosed from a screenshot: no build had ever stated which hair it wrote.
///
/// It also exists because the first two attempts got the record model wrong. A head part's
/// `ValidRaces` field holds a **FormList of races**, not a race. 3.7.5 compared that FormList's
/// FormID against a race's and therefore always found a mismatch; 3.7.6 papered over that by
/// treating two records from the same plugin as interchangeable, which is wrong for two unrelated
/// races in one plugin. These tests pin membership in the list, which is what the engine acts on.
/// </summary>
public sealed class HeadPartAuditTests
{
    private static readonly ILogger Log = new LoggerConfiguration().CreateLogger();
    private const string Her = "013746:Skyrim.esm";
    private const string OtherRace = "000801:DWKaPoTunRace.esp";

    /// <summary>A hair part naming <paramref name="races"/> through a list, or no list at all.</summary>
    private static HeadPartFacts Hair(IReadOnlyList<string>? races, string? list = "00083D:Hair.esp") =>
        new("Hair01", "Hair", "hair.nif", races is null ? null : list,
            races is null ? null : "HeadPartsList", races);

    private static string[] Messages(ValidationReport report, string code) =>
        report.Findings.Where(f => f.Code == code).Select(f => f.Message).ToArray();

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

    // ---- membership in the race list --------------------------------------

    [Fact]
    public void A_list_that_contains_her_race_is_silent()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(Appearance(3), Her, key => Hair([Her]), report);

        Assert.Empty(report.Findings);
    }

    [Fact]
    public void A_list_that_excludes_her_race_names_the_list_and_its_contents()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(Appearance(3), Her, key => Hair([OtherRace]), report);

        var warning = Assert.Single(Messages(report, "PRESET_HEADPART_OTHER_RACE"));
        Assert.Contains("HeadPartsList", warning, StringComparison.Ordinal);
        Assert.Contains(OtherRace, warning, StringComparison.Ordinal);
        Assert.Contains(Her, warning, StringComparison.Ordinal);
    }

    [Fact]
    public void Several_races_in_one_list_are_fine_when_hers_is_one_of_them()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(
            Appearance(3), Her, key => Hair([OtherRace, Her, "000802:DWKaPoTunRace.esp"]), report);

        Assert.Empty(report.Findings);
    }

    /// <summary>
    /// The case the 3.7.6 plugin-name shortcut got wrong: two unrelated races can live in one ESP,
    /// so "same plugin" is not evidence of anything.
    /// </summary>
    [Fact]
    public void Two_unrelated_races_in_one_plugin_are_still_a_mismatch()
    {
        var otherInSamePlugin = "000A12:DWKaPoTunRace.esp";
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(Appearance(3), OtherRace, key => Hair([otherInSamePlugin]), report);

        Assert.Single(Messages(report, "PRESET_HEADPART_OTHER_RACE"));
    }

    [Fact]
    public void A_vampire_race_is_audited_like_any_other_and_can_be_excluded()
    {
        var vampire = "00083C:DWKaPoTunRace.esp";
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(Appearance(3), vampire, key => Hair([OtherRace]), report);

        Assert.Single(Messages(report, "PRESET_HEADPART_OTHER_RACE"));
    }

    [Fact]
    public void A_vampire_race_in_the_list_is_silent()
    {
        var vampire = "00083C:DWKaPoTunRace.esp";
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(Appearance(3), vampire, key => Hair([OtherRace, vampire]), report);

        Assert.Empty(report.Findings);
    }

    [Fact]
    public void A_race_and_its_list_in_different_plugins_is_judged_by_membership()
    {
        // Her race lives in Skyrim.esm; the list lives with the hair mod and does not name her.
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(
            Appearance(3), Her, key => Hair([OtherRace], "00083D:HairMod.esp"), report);

        Assert.Single(Messages(report, "PRESET_HEADPART_OTHER_RACE"));
    }

    /// <summary>
    /// A list that cannot be read is reported as unknown. Saying "compatible" would be a guess and
    /// saying "incompatible" would be a different guess.
    /// </summary>
    [Fact]
    public void An_unreadable_list_reports_unknown_and_does_not_claim_a_mismatch()
    {
        var report = new ValidationReport();
        var unreadable = new HeadPartFacts("Hair01", "Hair", "hair.nif",
            "00083D:HairMod.esp", "HeadPartsList", null);
        FollowerBuilder.ReportHeadPartRaces(Appearance(3), Her, key => unreadable, report);

        Assert.Single(Messages(report, "PRESET_HEADPART_RACE_UNKNOWN"));
        Assert.Empty(Messages(report, "PRESET_HEADPART_OTHER_RACE"));
    }

    [Fact]
    public void A_part_with_no_race_list_is_not_flagged()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(Appearance(3), Her, key => Hair(null), report);

        Assert.Empty(report.Findings);
    }

    [Fact]
    public void An_unrestricted_hair_is_called_out_when_the_preset_belongs_to_another_race()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(
            Appearance(1, 3), Her,
            key => key.StartsWith("000800", StringComparison.Ordinal)
                ? new HeadPartFacts("FemaleHeadKapotun", "Face", "head.nif",
                    "00083D:DWKaPoTunRace.esp", "HeadPartsKapotun", [OtherRace])
                : new HeadPartFacts("FemaleHairKapotun01", "Hair",
                    @"actors\character\DWRaces\TigerKhajiit\Hair\FemaleHairKapotun01.nif",
                    null, null, null),
            report);

        var hair = Assert.Single(Messages(report, "PRESET_HAIR_OTHER_RACE"));
        Assert.Contains("FemaleHairKapotun01", hair, StringComparison.Ordinal);
        Assert.Contains("TigerKhajiit", hair, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unrestricted_hair_on_its_own_is_not_flagged()
    {
        var report = new ValidationReport();
        FollowerBuilder.ReportHeadPartRaces(Appearance(3), Her, key => Hair(null), report);

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

    // ---- real HDPT -> FLST -> RACE, read from disk -------------------------

    /// <summary>
    /// The facts above are the shape the builder sees; these prove the resolver derives that shape
    /// from real records rather than from an assumption about what the field points at.
    /// </summary>
    [Fact]
    public void Resolver_reads_race_membership_from_a_real_head_part_and_form_list()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ff_hdpt_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            const string plugin = "FF_HeadPartFixture.esp";
            var mod = new SkyrimMod(ModKey.FromFileName(plugin), SkyrimRelease.SkyrimSE);
            var race = mod.Races.AddNew("FixtureRace");
            var list = mod.FormLists.AddNew("FixtureValidRaces");
            list.Items.Add(race.FormKey.ToLink<ISkyrimMajorRecordGetter>());
            var part = mod.HeadParts.AddNew("FixtureHair");
            part.Type = HeadPart.TypeEnum.Hair;
            part.ValidRaces.SetTo(list.FormKey);
            mod.WriteToBinary(Path.Combine(directory, plugin));

            using var resolver = new RecordResolver(directory, Log);
            var facts = resolver.ResolveHeadPart(part.FormKey.ToString());

            Assert.NotNull(facts);
            Assert.Equal("Hair", facts!.Type);
            Assert.Equal("FixtureValidRaces", facts.ValidRacesFormListEditorId);
            Assert.NotNull(facts.ValidRaceFormKeys);
            Assert.Contains(race.FormKey.ToString(), facts.ValidRaceFormKeys!);
        }
        finally { try { Directory.Delete(directory, recursive: true); } catch (IOException) { } }
    }

    [Fact]
    public void Resolver_reports_unknown_when_the_named_list_is_not_in_the_plugin()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ff_hdpt_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            const string plugin = "FF_HeadPartMissing.esp";
            var mod = new SkyrimMod(ModKey.FromFileName(plugin), SkyrimRelease.SkyrimSE);
            var part = mod.HeadParts.AddNew("OrphanHair");
            part.Type = HeadPart.TypeEnum.Hair;
            part.ValidRaces.SetTo(FormKey.Factory($"000FFF:{plugin}"));
            mod.WriteToBinary(Path.Combine(directory, plugin));

            using var resolver = new RecordResolver(directory, Log);
            var facts = resolver.ResolveHeadPart(part.FormKey.ToString());

            Assert.NotNull(facts);
            Assert.NotNull(facts!.ValidRacesFormList);
            Assert.Null(facts.ValidRaceFormKeys);
        }
        finally { try { Directory.Delete(directory, recursive: true); } catch (IOException) { } }
    }

    [Fact]
    public void Resolver_reports_no_restriction_when_valid_races_is_null()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ff_hdpt_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            const string plugin = "FF_HeadPartNull.esp";
            var mod = new SkyrimMod(ModKey.FromFileName(plugin), SkyrimRelease.SkyrimSE);
            var part = mod.HeadParts.AddNew("UnrestrictedHair");
            part.Type = HeadPart.TypeEnum.Hair;
            mod.WriteToBinary(Path.Combine(directory, plugin));

            using var resolver = new RecordResolver(directory, Log);
            var facts = resolver.ResolveHeadPart(part.FormKey.ToString());

            Assert.NotNull(facts);
            Assert.Null(facts!.ValidRacesFormList);
            Assert.Null(facts.ValidRaceFormKeys);
        }
        finally { try { Directory.Delete(directory, recursive: true); } catch (IOException) { } }
    }

    /// <summary>
    /// A FormKey names its plugin, so the race list can live somewhere other than the head part's
    /// own file — a shared list plugin, or a master. Looking it up only in the defining plugin of
    /// the head part reports UNKNOWN for perfectly ordinary mods.
    /// </summary>
    [Fact]
    public void Resolver_reads_a_form_list_defined_in_another_plugin()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ff_hdpt_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var shared = new SkyrimMod(ModKey.FromFileName("FF_SharedLists.esp"), SkyrimRelease.SkyrimSE);
            var race = shared.Races.AddNew("SharedRace");
            var list = shared.FormLists.AddNew("SharedValidRaces");
            list.Items.Add(race.FormKey.ToLink<ISkyrimMajorRecordGetter>());
            shared.WriteToBinary(Path.Combine(directory, "FF_SharedLists.esp"));

            var hair = new SkyrimMod(ModKey.FromFileName("FF_CrossPluginHair.esp"), SkyrimRelease.SkyrimSE);
            hair.ModHeader.MasterReferences.Add(new MasterReference
            {
                Master = ModKey.FromFileName("FF_SharedLists.esp"),
            });
            var part = hair.HeadParts.AddNew("CrossPluginHair");
            part.Type = HeadPart.TypeEnum.Hair;
            part.ValidRaces.SetTo(list.FormKey);
            hair.WriteToBinary(Path.Combine(directory, "FF_CrossPluginHair.esp"));

            using var resolver = new RecordResolver(directory, Log);
            var facts = resolver.ResolveHeadPart(part.FormKey.ToString());

            Assert.NotNull(facts);
            Assert.NotNull(facts!.ValidRaceFormKeys);
            Assert.Contains(race.FormKey.ToString(), facts.ValidRaceFormKeys!);
        }
        finally { try { Directory.Delete(directory, recursive: true); } catch (IOException) { } }
    }

    [Fact]
    public void Resolver_reads_a_form_list_defined_in_a_master_plugin()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ff_hdpt_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var master = new SkyrimMod(ModKey.FromFileName("FF_FixtureMaster.esm"), SkyrimRelease.SkyrimSE);
            var race = master.Races.AddNew("MasterRace");
            var list = master.FormLists.AddNew("MasterValidRaces");
            list.Items.Add(race.FormKey.ToLink<ISkyrimMajorRecordGetter>());
            master.WriteToBinary(Path.Combine(directory, "FF_FixtureMaster.esm"));

            var user = new SkyrimMod(ModKey.FromFileName("FF_MasterUser.esp"), SkyrimRelease.SkyrimSE);
            user.ModHeader.MasterReferences.Add(new MasterReference
            {
                Master = ModKey.FromFileName("FF_FixtureMaster.esm"),
            });
            var part = user.HeadParts.AddNew("MasterListHair");
            part.Type = HeadPart.TypeEnum.Hair;
            part.ValidRaces.SetTo(list.FormKey);
            user.WriteToBinary(Path.Combine(directory, "FF_MasterUser.esp"));

            using var resolver = new RecordResolver(directory, Log);
            var facts = resolver.ResolveHeadPart(part.FormKey.ToString());

            Assert.NotNull(facts);
            Assert.NotNull(facts!.ValidRaceFormKeys);
            Assert.Contains(race.FormKey.ToString(), facts.ValidRaceFormKeys!);
        }
        finally { try { Directory.Delete(directory, recursive: true); } catch (IOException) { } }
    }

    /// <summary>
    /// The catalogue records which plugin wins a FormList, so an override can be opened directly
    /// instead of scanning the load order. When it is opened, membership must come from the
    /// override — the base record is not what the game reads.
    /// </summary>
    [Fact]
    public void Resolver_follows_a_winning_override_of_the_race_list()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ff_hdpt_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var baseMod = new SkyrimMod(ModKey.FromFileName("FF_BaseLists.esp"), SkyrimRelease.SkyrimSE);
            var baseRace = baseMod.Races.AddNew("BaseRace");
            var baseList = baseMod.FormLists.AddNew("ContestedRaces");
            baseList.Items.Add(baseRace.FormKey.ToLink<ISkyrimMajorRecordGetter>());
            var part = baseMod.HeadParts.AddNew("ContestedHair");
            part.Type = HeadPart.TypeEnum.Hair;
            part.ValidRaces.SetTo(baseList.FormKey);
            baseMod.WriteToBinary(Path.Combine(directory, "FF_BaseLists.esp"));

            var lateMod = new SkyrimMod(ModKey.FromFileName("FF_LateOverride.esp"), SkyrimRelease.SkyrimSE);
            lateMod.ModHeader.MasterReferences.Add(new MasterReference
            {
                Master = ModKey.FromFileName("FF_BaseLists.esp"),
            });
            var lateRace = lateMod.Races.AddNew("LateRace");
            // An override carries the master's FormKey; that is what makes it an override rather
            // than a second list.
            var overridden = new FormList(baseList.FormKey, SkyrimRelease.SkyrimSE)
            {
                EditorID = baseList.EditorID,
            };
            overridden.Items.Add(lateRace.FormKey.ToLink<ISkyrimMajorRecordGetter>());
            lateMod.FormLists.Add(overridden);
            lateMod.WriteToBinary(Path.Combine(directory, "FF_LateOverride.esp"));

            using var resolver = new RecordResolver(directory, Log);
            var facts = resolver.ResolveHeadPart(part.FormKey.ToString(), _ => "FF_LateOverride.esp");

            Assert.NotNull(facts);
            Assert.Equal("FF_LateOverride.esp", facts!.ValidRacesOverriddenBy);
            Assert.NotNull(facts.ValidRaceFormKeys);
            Assert.Contains(lateRace.FormKey.ToString(), facts.ValidRaceFormKeys!);
            Assert.DoesNotContain(baseRace.FormKey.ToString(), facts.ValidRaceFormKeys!);
        }
        finally { try { Directory.Delete(directory, recursive: true); } catch (IOException) { } }
    }

    [Fact]
    public void Resolver_reports_unknown_when_the_winning_override_cannot_be_opened()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ff_hdpt_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            const string plugin = "FF_OverrideGone.esp";
            var mod = new SkyrimMod(ModKey.FromFileName(plugin), SkyrimRelease.SkyrimSE);
            var race = mod.Races.AddNew("SoloRace");
            var list = mod.FormLists.AddNew("SoloRaces");
            list.Items.Add(race.FormKey.ToLink<ISkyrimMajorRecordGetter>());
            var part = mod.HeadParts.AddNew("SoloHair");
            part.Type = HeadPart.TypeEnum.Hair;
            part.ValidRaces.SetTo(list.FormKey);
            mod.WriteToBinary(Path.Combine(directory, plugin));

            using var resolver = new RecordResolver(directory, Log);
            var facts = resolver.ResolveHeadPart(part.FormKey.ToString(), _ => "FF_NotOnDisk.esp");

            Assert.NotNull(facts);
            Assert.Equal("FF_NotOnDisk.esp", facts!.ValidRacesOverriddenBy);
            Assert.Null(facts.ValidRaceFormKeys);
        }
        finally { try { Directory.Delete(directory, recursive: true); } catch (IOException) { } }
    }
}
