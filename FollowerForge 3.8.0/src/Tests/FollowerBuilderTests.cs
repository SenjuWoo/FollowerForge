using FollowerForge.BuildPipeline;
using FollowerForge.Domain;
using FollowerForge.SkyrimRecords;
using FollowerForge.Validation;
using Serilog;

namespace FollowerForge.Tests;

[Collection("builds")]
public sealed class FollowerBuilderTests : IDisposable
{
    private static readonly ILogger Log = new LoggerConfiguration().CreateLogger();
    private readonly string _workspace = Path.Combine(Path.GetTempPath(), "ff_bld_" + Guid.NewGuid().ToString("N"));

    // Environment whose protected roots are elsewhere, so the temp workspace stays writable.
    // Derived from this instance's workspace: a fixed temp path let two test classes write the same
    // fake game folder in parallel, which failed a Hub test once and passed on re-run.
    private EnvironmentSnapshot FakeEnv()
    {
        var game = Path.Combine(_workspace, "game");
        return new()
        {
            Manager = ModManagerKind.Vortex,
            ManagerLabel = "Vortex",
            GameRootPath = game,
            GameDataPath = Path.Combine(game, "Data"),
            PluginDataPath = Path.Combine(game, "Data"),
            InstancePath = Path.Combine(_workspace, "vortex"),
            StagingPath = Path.Combine(_workspace, "vortex", "mods"),
            ProfilesPath = Path.Combine(_workspace, "vortex", "profiles"),
            RuntimePluginsTxtPath = Path.Combine(_workspace, "runtime.txt"),
        };
    }

    private static FollowerProfile Profile() => new()
    {
        Name = "Builder Test",
        PluginName = "FF_BuilderTest.esp",
        Race = new RecordRef(VanillaForms.NordRace.ToString()),
        VoiceType = new RecordRef(VanillaForms.FemaleEvenTonedVoice.ToString()),
        Class = new RecordRef(VanillaForms.CombatWarrior1HClass.ToString()),
        Outfit = new RecordRef(VanillaForms.FarmClothesOutfit.ToString()),
        Placement = new PlacementSpec { Cell = new RecordRef(VanillaForms.WhiterunWorldPersistentCell.ToString()) },
    };

    // ---- the manifest, the asset inventory and the console must agree --------------

    private static FollowerBuilder.FaceGenOutcome Face(FaceGenStatus status) =>
        new(new FaceGenResult
        {
            Status = status,
            FaceGeomPath = Path.Combine("meshes", "actors", "character", "facegendata", "facegeom", "FF_X.esp", "00000800.nif"),
            FaceTintPath = Path.Combine("textures", "actors", "character", "facegendata", "facetint", "FF_X.esp", "00000800.dds"),
            CkHandoffPath = status is FaceGenStatus.Partial or FaceGenStatus.NeedsManualRepair
                ? "ck-handoff-report.json" : null,
        }, []);

    [Fact]
    public void A_partial_face_lists_its_files_and_calls_them_incomplete()
    {
        var assets = FollowerBuilder.BuildSourceAssets(Profile(), Face(FaceGenStatus.Partial), hub: null);

        var roles = assets.Assets.Select(a => a.Role).ToList();
        Assert.Contains(roles, r => r.Contains("FaceGen NIF") && r.Contains("incomplete"));
        Assert.Contains(roles, r => r.Contains("FaceGen tint DDS") && r.Contains("incomplete"));
    }

    [Fact]
    public void A_complete_face_is_not_labelled_incomplete()
    {
        var assets = FollowerBuilder.BuildSourceAssets(Profile(), Face(FaceGenStatus.Complete), hub: null);

        Assert.DoesNotContain(assets.Assets, a => a.Role.Contains("incomplete"));
        Assert.Contains(assets.Assets, a => a.Role == "FaceGen NIF");
    }

    [Theory]
    [InlineData(FaceGenStatus.Complete, true, "complete")]
    [InlineData(FaceGenStatus.Partial, true, "partial")]
    [InlineData(FaceGenStatus.NeedsManualRepair, false, "needs manual repair")]
    [InlineData(FaceGenStatus.Failed, false, "failed")]
    public void Manifest_inventory_and_console_agree(
        FaceGenStatus status, bool hasFaceGen, string word)
    {
        var profile = Profile();
        var compiled = new FollowerCompiler(Log).Compile(profile, location: null);
        var face = Face(status);

        var manifest = FollowerBuilder.BuildManifest(profile, compiled, ["Skyrim.esm"],
            face.Result, DateTimeOffset.UnixEpoch);
        var assets = FollowerBuilder.BuildSourceAssets(profile, face, hub: null);

        Assert.Equal(hasFaceGen, manifest.HasFaceGen);
        Assert.Equal(word, manifest.FaceGenStatus);
        // The console and the HTML report both print this string.
        Assert.Equal(
            FaceGenPlayerText.Summary(status),
            FaceGenPlayerText.Summary(manifest.FaceGenStatus));
        // A face in the manifest is a face in the inventory, or neither.
        Assert.Equal(
            hasFaceGen,
            assets.Assets.Any(a => a.Role.StartsWith("FaceGen", StringComparison.Ordinal)));
    }

    [Fact]
    public void A_build_with_no_export_has_no_face_in_manifest_or_inventory()
    {
        var profile = Profile();
        var compiled = new FollowerCompiler(Log).Compile(profile, location: null);

        var manifest = FollowerBuilder.BuildManifest(profile, compiled, ["Skyrim.esm"],
            faceGen: null, DateTimeOffset.UnixEpoch);
        var assets = FollowerBuilder.BuildSourceAssets(profile, faceGen: null, hub: null);

        Assert.False(manifest.HasFaceGen);
        Assert.Equal("not attempted", manifest.FaceGenStatus);
        Assert.DoesNotContain(assets.Assets, a => a.Role.StartsWith("FaceGen", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_Manifest_reports_a_clone_only_when_the_plugin_holds_one()
    {
        var profile = Profile();
        // Cloning asked for, and no plugin on disk here to resolve the source from, so the compiler
        // references the style instead. The manifest describes the plugin, not the request.
        var asked = new FollowerProfile
        {
            Name = profile.Name,
            PluginName = profile.PluginName,
            Race = profile.Race,
            VoiceType = profile.VoiceType,
            Class = profile.Class,
            Outfit = profile.Outfit,
            Placement = profile.Placement,
            CombatStyle = new CombatStyleChoice
            {
                Style = new RecordRef("00003D:Skyrim.esm"),
                CloneIntoPlugin = true,
            },
        };

        var result = new FollowerBuilder(Log).Build(asked, FakeEnv(), _workspace, location: null);

        Assert.False(result.Manifest.CombatStyleCloned);
        Assert.Contains(result.Validation.Findings, f => f.Code == "CSTY_CLONE_UNRESOLVED");
    }

    [Fact]
    public void Build_PublishesAllRequiredOutputs()
    {
        var result = new FollowerBuilder(Log).Build(Profile(), FakeEnv(), _workspace, location: null);

        Assert.True(result.Success);
        Assert.False(result.Validation.HasErrors);
        foreach (var name in new[]
        {
            "FF_BuilderTest.esp", "manifest.json", "source-assets.json", "dependency-report.json",
            "rebuild-profile.json", "build-report.html", "credits.md", "SHARE-CHECKLIST.txt",
        })
            Assert.True(File.Exists(Path.Combine(result.OutputDirectory, name)), $"missing {name}");
    }

    [Fact]
    public void Build_MissingExplicitFace_DoesNotPublishFacelessReplacement()
    {
        var profile = Profile() with
        {
            Appearance = new AppearanceSpec
            {
                CharGenNifPath = Path.Combine(_workspace, "missing-head.nif"),
            },
        };
        var result = new FollowerBuilder(Log).Build(profile, FakeEnv(), _workspace);
        Assert.False(result.Success);
        Assert.Contains(result.Validation.Findings, f => f.Code == "FACE_NIF_MISSING");
    }

    [Fact]
    public void Build_TintedAppearanceWithoutComplexion_WarnsThatFaceAndBodyMayNotMatch()
    {
        var profile = Profile() with
        {
            Appearance = new AppearanceSpec
            {
                TintLayers = [new TintLayerSpec(0, 0xFFB08060)],
            },
        };
        var result = new FollowerBuilder(Log).Build(profile, FakeEnv(), _workspace);
        Assert.Contains(result.Validation.Findings, f => f.Code == "PRESET_NO_HEAD_TEXTURE");
    }

    [Fact]
    public void Build_UnresolvableHeadParts_WarnsAboutMissingHairInGame()
    {
        var profile = Profile() with
        {
            Appearance = new AppearanceSpec
            {
                UnresolvedHeadParts = 2,
            },
        };
        var result = new FollowerBuilder(Log).Build(profile, FakeEnv(), _workspace);
        Assert.Contains(result.Validation.Findings, f => f.Code == "PRESET_HEADPART_UNRESOLVED");
    }

    [Fact]
    public void Build_GameDataPublishRoot_IsRejectedBeforeWriting()
    {
        var env = FakeEnv() with
        {
            GameRootPath = Path.Combine(_workspace, "game"),
            GameDataPath = Path.Combine(_workspace, "game", "Data"),
        };
        Assert.Throws<UnauthorizedAccessException>(() =>
            new FollowerBuilder(Log).Build(Profile(), env, Path.Combine(_workspace, "scratch"),
                publishRoot: env.GameDataPath));
        Assert.False(Directory.Exists(env.GameDataPath));
    }

    [Fact]
    public void Build_PluginPassesShipGateHeaderRules()
    {
        var result = new FollowerBuilder(Log).Build(Profile(), FakeEnv(), _workspace, location: null);
        var report = new ValidationReport();
        EspHeaderValidator.Validate(result.PluginPath, report, requireEsl: true);
        Assert.False(report.HasErrors, string.Join("; ", report.Findings.Select(f => f.Code + ":" + f.Message)));
        Assert.Contains(report.Findings, f => f.Code == "SHIP_GATE_PASS");
    }

    [Fact]
    public void Build_PublishRootInsideManagerMods_IsAllowedWhenTheUserChoseIt()
    {
        var env = FakeEnv();
        Directory.CreateDirectory(env.StagingPath);
        var result = new FollowerBuilder(Log).Build(
            Profile(), env, _workspace, location: null, catalog: null, publishRoot: env.StagingPath);

        Assert.True(result.Success);
        Assert.Equal(Path.Combine(env.StagingPath, "Builder Test"), result.OutputDirectory);
        Assert.True(File.Exists(Path.Combine(result.OutputDirectory, "FF_BuilderTest.esp")));
        Assert.True(Directory.Exists(Path.Combine(_workspace, ".staging")));
    }

    [Fact]
    public void Build_PublishRootOnAnotherVolume_ReplacesPreviousBuild()
    {
        var destination = Environment.GetEnvironmentVariable("FFORGE_TEST_PUBLISH_ROOT");
        if (string.IsNullOrWhiteSpace(destination)) destination = Path.Combine(_workspace, "output");
        var output = Path.Combine(destination, Guid.NewGuid().ToString("N"));
        try
        {
            var previous = Path.Combine(output, "Builder Test");
            Directory.CreateDirectory(previous);
            File.WriteAllText(Path.Combine(previous, "old.txt"), "previous build");
            var result = new FollowerBuilder(Log).Build(Profile(), FakeEnv(), _workspace, publishRoot: output);
            Assert.True(result.Success);
            Assert.True(File.Exists(result.PluginPath));
            Assert.False(File.Exists(Path.Combine(previous, "old.txt")));
        }
        finally { if (Directory.Exists(output)) Directory.Delete(output, true); }
    }

    [Fact]
    public void Build_LockedPreviousOutput_IsPreserved()
    {
        var destination = Path.Combine(_workspace, "builds", "Builder Test");
        Directory.CreateDirectory(destination);
        var sentinel = Path.Combine(destination, "old.txt");
        File.WriteAllText(sentinel, "previous build");
        using (File.Open(sentinel, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.ThrowsAny<IOException>(() => new FollowerBuilder(Log).Build(Profile(), FakeEnv(), _workspace));
        Assert.Equal("previous build", File.ReadAllText(sentinel));
        Assert.Empty(Directory.EnumerateDirectories(Path.GetDirectoryName(destination)!, ".ff-*"));
    }

    [Fact]
    public void Build_RefusesToWriteInsideProtectedRoots()
    {
        var env = FakeEnv();
        // Point the workspace inside the protected staging root.
        var badWorkspace = Path.Combine(env.StagingPath, "sneaky");
        Assert.Throws<UnauthorizedAccessException>(() =>
            new FollowerBuilder(Log).Build(Profile(), env, badWorkspace, location: null));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_workspace)) Directory.Delete(_workspace, recursive: true); }
        catch (IOException) { }
    }
}
