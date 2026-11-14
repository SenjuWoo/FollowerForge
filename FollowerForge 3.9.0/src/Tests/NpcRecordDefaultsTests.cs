using FollowerForge.Domain;
using FollowerForge.SkyrimRecords;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace FollowerForge.Tests;

/// <summary>
/// The fields a fresh Mutagen NPC leaves at zero, and that the game reads literally.
///
/// Measured against Skyrim.esm: 5,105 of its 5,118 NPCs carry speed multiplier 100, height above
/// zero and NAM5 = 255, and none carries a zero speed multiplier or a zero height. Through 3.7.11
/// every built follower shipped speed 0 (the Creation Kit refuses to save her: "Speed multiplier
/// is zero"; in game she runs on the spot unless a framework overwrites the value), height 0, and
/// a level multiplier of 16.96 where 1.0 was asked for.
/// </summary>
public sealed class NpcRecordDefaultsTests
{
    private static readonly ILogger Log = new LoggerConfiguration().CreateLogger();

    private static FollowerProfile Profile(LevelScaling? level = null) => new()
    {
        Name = "Test Follower",
        PluginName = "FF_TestFollower.esp",
        Race = new RecordRef(VanillaForms.NordRace.ToString()),
        VoiceType = new RecordRef(VanillaForms.FemaleEvenTonedVoice.ToString()),
        Class = new RecordRef(VanillaForms.CombatWarrior1HClass.ToString()),
        Placement = new PlacementSpec { Cell = new RecordRef(VanillaForms.WhiterunWorldPersistentCell.ToString()) },
        Level = level ?? new LevelScaling(),
    };

    private static T WriteAndReopen<T>(FollowerProfile profile, Func<ISkyrimModGetter, byte[], T> read)
    {
        var dir = Path.Combine(Path.GetTempPath(), "ff_npc_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var result = new FollowerCompiler(Log).Compile(profile, location: null);
            var path = Path.Combine(dir, result.Mod.ModKey.FileName);
            new PluginWriter(Log).Write(result.Mod, path);
            var bytes = File.ReadAllBytes(path);
            using var mod = SkyrimMod.CreateFromBinaryOverlay(path, VanillaForms.Release);
            return read(mod, bytes);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
        }
    }

    /// <summary>The raw ACBS block of the first NPC: what the game and the Creation Kit read.</summary>
    private static ReadOnlySpan<byte> Acbs(byte[] plugin)
    {
        var at = plugin.AsSpan().IndexOf("ACBS"u8);
        Assert.True(at > 0, "no ACBS subrecord in the written plugin");
        Assert.Equal(24, BitConverter.ToUInt16(plugin, at + 4));
        return plugin.AsSpan(at + 6, 24);
    }

    [Fact]
    public void SpeedMultiplier_IsOneHundred_NotZero()
    {
        var raw = WriteAndReopen(Profile(), (mod, bytes) =>
        {
            Assert.Equal(100, mod.Npcs.First().Configuration.SpeedMultiplier);
            return BitConverter.ToUInt16(Acbs(bytes).Slice(14, 2));
        });
        Assert.Equal(100, raw);
    }

    [Fact]
    public void Height_IsOne_NotZero()
    {
        var height = WriteAndReopen(Profile(), (mod, _) => mod.Npcs.First().Height);
        Assert.Equal(1f, height);
    }

    [Fact]
    public void RemainingDefaults_MatchVanillaNpcs()
    {
        WriteAndReopen(Profile(), (mod, _) =>
        {
            var npc = mod.Npcs.First();
            Assert.Equal(35, npc.Configuration.DispositionBase);
            Assert.Equal(SoundLevel.Normal, npc.SoundLevel);
            Assert.Equal(255, npc.NAM5);
            return 0;
        });
    }

    [Theory]
    [InlineData(1.0, 1000)]
    [InlineData(0.75, 750)]
    [InlineData(1.5, 1500)]
    public void LevelMultiplier_IsWrittenOnce_NotScaledTwice(double requested, ushort storedThousandths)
    {
        var level = new LevelScaling { ScaleWithPlayer = true, PlayerLevelMult = requested };
        var raw = WriteAndReopen(Profile(level), (mod, bytes) =>
        {
            var read = Assert.IsAssignableFrom<IPcLevelMultGetter>(mod.Npcs.First().Configuration.Level);
            Assert.Equal((float)requested, read.LevelMult, 3);
            return BitConverter.ToUInt16(Acbs(bytes).Slice(8, 2));
        });
        // 1.0 used to be stored as 16960: 1000 scaled by 1000 again and wrapped at sixteen bits.
        Assert.Equal(storedThousandths, raw);
    }

    [Fact]
    public void EnemyForm_InheritsTheSameDefaults()
    {
        var profile = Profile() with
        {
            EnemyToAlly = new EnemyToAllySpec { Enabled = true, LocationIds = ["spot"] },
        };
        var spot = new SpawnLocation
        {
            Id = "spot", Name = "spot", Kind = LocationKind.Interior,
            CellFormKey = "013870:Skyrim.esm", RequiredPlugin = "Skyrim.esm",
        };
        var result = new FollowerCompiler(Log).Compile(profile, location: null,
            alternateSpawns: [(spot, null)]);

        Assert.Equal(2, result.Mod.Npcs.Count);
        Assert.All(result.Mod.Npcs, npc =>
        {
            Assert.Equal(100, npc.Configuration.SpeedMultiplier);
            Assert.Equal(1f, npc.Height);
        });
    }

    [Fact]
    public void Validator_RejectsAZeroSpeedMultiplierAndAZeroHeight()
    {
        var result = new FollowerCompiler(Log).Compile(Profile(), location: null);
        var npc = result.Mod.Npcs.First();
        npc.Configuration.SpeedMultiplier = 0;
        npc.Height = 0f;

        var report = new ValidationReport();
        FollowerValidator.Validate(result, Profile(), report);

        Assert.Contains(report.Findings, f => f.Code == "NPC_SPEED_ZERO" && f.Severity == ValidationSeverity.Error);
        Assert.Contains(report.Findings, f => f.Code == "NPC_HEIGHT_ZERO" && f.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void Validator_RejectsALevelMultiplierThatIsNotTheOneAskedFor()
    {
        var result = new FollowerCompiler(Log).Compile(Profile(), location: null);
        result.Mod.Npcs.First().Configuration.Level = new PcLevelMult { LevelMult = 16.96f };

        var report = new ValidationReport();
        FollowerValidator.Validate(result, Profile(), report);

        Assert.Contains(report.Findings, f => f.Code == "LEVEL_MULT_MISMATCH" && f.Severity == ValidationSeverity.Error);
    }
}
