using FollowerForge.BuildPipeline;
using FollowerForge.Domain;
using FollowerForge.SkyrimRecords;
using FollowerForge.Validation;
using Serilog;

namespace FollowerForge.Tests;

public sealed class FollowerBuilderTests : IDisposable
{
    private static readonly ILogger Log = new LoggerConfiguration().CreateLogger();
    private readonly string _workspace = Path.Combine(Path.GetTempPath(), "ff_bld_" + Guid.NewGuid().ToString("N"));

    // Environment whose protected roots are elsewhere, so the temp workspace stays writable.
    private EnvironmentSnapshot FakeEnv() => new()
    {
        Manager = ModManagerKind.Vortex,
        ManagerLabel = "Vortex",
        GameRootPath = Path.Combine(Path.GetTempPath(), "ff_fake_game"),
        GameDataPath = Path.Combine(Path.GetTempPath(), "ff_fake_game", "Data"),
        PluginDataPath = Path.Combine(Path.GetTempPath(), "ff_fake_game", "Data"),
        InstancePath = Path.Combine(Path.GetTempPath(), "ff_fake_vortex"),
        StagingPath = Path.Combine(Path.GetTempPath(), "ff_fake_vortex", "mods"),
        ProfilesPath = Path.Combine(Path.GetTempPath(), "ff_fake_vortex", "profiles"),
        RuntimePluginsTxtPath = Path.Combine(Path.GetTempPath(), "ff_fake_runtime.txt"),
    };

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
