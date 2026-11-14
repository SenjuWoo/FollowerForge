using System.IO.Compression;
using FollowerForge.BuildPipeline;
using FollowerForge.Domain;
using FollowerForge.SkyrimRecords;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace FollowerForge.Tests;

/// <summary>
/// The tools that work on followers already built: finding them, rebuilding them, packing several
/// into one archive, and repairing a plugin an older version wrote.
/// </summary>
[Collection("builds")]
public sealed class FollowerToolsTests : IDisposable
{
    private static readonly ILogger Log = new LoggerConfiguration().CreateLogger();
    private readonly string _ws = Path.Combine(Path.GetTempPath(), "ff_tools_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(_ws, recursive: true); } catch (IOException) { }
    }

    private EnvironmentSnapshot Env() => new()
    {
        Manager = ModManagerKind.Vortex,
        ManagerLabel = "Vortex",
        GameRootPath = Path.Combine(_ws, "game"),
        GameDataPath = Path.Combine(_ws, "game", "Data"),
        PluginDataPath = Path.Combine(_ws, "game", "Data"),
        InstancePath = Path.Combine(_ws, "vortex"),
        StagingPath = Path.Combine(_ws, "vortex", "mods"),
        ProfilesPath = Path.Combine(_ws, "vortex", "profiles"),
        RuntimePluginsTxtPath = Path.Combine(_ws, "runtime.txt"),
    };

    private static FollowerProfile Profile(string name) => new()
    {
        Name = name,
        PluginName = "FF_" + new string(name.Where(char.IsLetterOrDigit).ToArray()) + ".esp",
        Race = new RecordRef(VanillaForms.NordRace.ToString()),
        VoiceType = new RecordRef(VanillaForms.FemaleEvenTonedVoice.ToString()),
        Class = new RecordRef(VanillaForms.CombatWarrior1HClass.ToString()),
        Placement = new PlacementSpec { Cell = new RecordRef(VanillaForms.WhiterunWorldPersistentCell.ToString()) },
    };

    private string Builds => Path.Combine(_ws, "builds");

    private FollowerBuilder.BuildResult Build(string name) =>
        new FollowerBuilder(Log).Build(Profile(name), Env(), _ws, location: null, publishRoot: Builds);

    /// <summary>A plugin exactly as builds before 3.8.0 wrote it.</summary>
    private string OldStylePlugin(string dir, string name = "Old Timer")
    {
        var result = new FollowerCompiler(Log).Compile(Profile(name), location: null);
        foreach (var npc in result.Mod.Npcs)
        {
            npc.Configuration.SpeedMultiplier = 0;
            npc.Configuration.DispositionBase = 0;
            npc.Height = 0f;
            npc.NAM5 = 0xFF00;
            npc.SoundLevel = SoundLevel.Loud;
            npc.Configuration.Level = new PcLevelMult { LevelMult = 16.96f };
        }
        var path = Path.Combine(dir, result.Mod.ModKey.FileName);
        new PluginWriter(Log).Write(result.Mod, path);
        return path;
    }

    private static Worldspace Whiterun()
    {
        var ws = new Worldspace(VanillaForms.WhiterunWorld, VanillaForms.Release)
        {
            EditorID = "WhiterunWorld",
            Flags = Worldspace.Flag.SmallWorld,
            DistantLodMultiplier = 0.25f,
        };
        ws.Location.SetTo(new FormKey(VanillaForms.Skyrim, 0x018A56));
        return ws;
    }

    // ---------------- library ----------------

    [Fact]
    public void Library_FindsEveryBuiltFollower_NewestFirst()
    {
        Build("Anna");
        Build("Bera");

        var entries = FollowerLibrary.Scan([Builds]);

        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.False(e.NeedsRebuild));
        Assert.Contains(entries, e => e.Name == "Anna" && e.PluginName == "FF_Anna.esp");
        Assert.All(entries, e => Assert.Equal(AppVersion.Current, e.GeneratorVersion));
    }

    [Fact]
    public void Library_FindsABuildFromBefore390_ByItsRootProfile()
    {
        var folder = Path.Combine(Builds, "Old Timer");
        Directory.CreateDirectory(folder);
        OldStylePlugin(folder);
        ProfileIo.Save(Path.Combine(folder, "rebuild-profile.json"), Profile("Old Timer"));

        var entry = Assert.Single(FollowerLibrary.Scan([Builds]));

        Assert.Equal("Old Timer", entry.Name);
        Assert.True(entry.NeedsRebuild, "speed multiplier 0 is what every build before 3.8.0 wrote");
        Assert.Null(entry.GeneratorVersion);
    }

    [Fact]
    public void Library_IgnoresFoldersThatAreNotFollowers()
    {
        Directory.CreateDirectory(Path.Combine(Builds, "random folder"));
        File.WriteAllText(Path.Combine(Builds, "random folder", "notes.txt"), "x");
        Assert.Empty(FollowerLibrary.Scan([Builds, Path.Combine(_ws, "missing")]));
    }

    [Fact]
    public void Batch_LoadsProfilesStraightFromABuildsFolder()
    {
        Build("Anna");
        Build("Bera");
        var names = BatchBuilder.LoadProfiles(Builds).Select(p => p.Name).Order().ToList();
        Assert.Equal(["Anna", "Bera"], names);
    }

    // ---------------- rebuild ----------------

    [Fact]
    public void Rebuild_ReplacesAnOldBuildInPlace_WithAFixedPlugin()
    {
        var folder = Path.Combine(Builds, "Old Timer");
        Directory.CreateDirectory(folder);
        OldStylePlugin(folder);
        File.WriteAllText(Path.Combine(folder, "credits.md"), "old root-level report");
        ProfileIo.Save(Path.Combine(folder, "rebuild-profile.json"), Profile("Old Timer"));

        var result = new BatchBuilder(Log).Rebuild(FollowerLibrary.Scan([Builds]), Env(), _ws);

        var item = Assert.Single(result.Items);
        Assert.True(item.Success);
        Assert.Equal(Path.GetFullPath(folder), Path.GetFullPath(item.OutputDirectory));
        Assert.False(FollowerLibrary.NeedsRebuild(Path.Combine(folder, "FF_OldTimer.esp")));
        Assert.False(File.Exists(Path.Combine(folder, "credits.md")), "the old root-level report is gone");
        Assert.True(File.Exists(Path.Combine(folder, "FollowerForge", "FF_OldTimer", "credits.md")));
        Assert.Single(Directory.EnumerateDirectories(Builds));
    }

    // ---------------- pack ----------------

    [Fact]
    public void Pack_PutsSeveralFollowersInOneArchive()
    {
        var a = Build("Anna");
        var b = Build("Bera");
        var zip = Path.Combine(_ws, "packs", "My pack.zip");

        var result = new FollowerPackager(Log).Pack([a.OutputDirectory, b.OutputDirectory], zip);

        Assert.Equal(2, result.Followers.Count);
        using var archive = ZipFile.OpenRead(zip);
        var names = archive.Entries.Select(e => e.FullName).ToList();
        Assert.Contains("FF_Anna.esp", names);
        Assert.Contains("FF_Bera.esp", names);
        Assert.Contains("FollowerForge/FF_Anna/rebuild-profile.json", names);
        Assert.Contains("FollowerForge/FF_Bera/rebuild-profile.json", names);
        Assert.Contains("FollowerForge/My pack - contents.txt", names);
        Assert.DoesNotContain(names, n => n.Contains('\\'));
        Assert.False(File.Exists(zip + ".partial"));
    }

    [Fact]
    public void Pack_MovesAnOldBuildsRootReports_SoTheyDoNotCollide()
    {
        var folders = new List<string>();
        foreach (var name in new[] { "Old One", "Old Two" })
        {
            var folder = Path.Combine(Builds, name);
            Directory.CreateDirectory(folder);
            OldStylePlugin(folder, name);
            File.WriteAllText(Path.Combine(folder, "credits.md"), $"credits for {name}");
            folders.Add(folder);
        }
        var zip = Path.Combine(_ws, "old.zip");

        new FollowerPackager(Log).Pack(folders, zip);

        using var archive = ZipFile.OpenRead(zip);
        var names = archive.Entries.Select(e => e.FullName).ToList();
        Assert.Contains("FollowerForge/FF_OldOne/credits.md", names);
        Assert.Contains("FollowerForge/FF_OldTwo/credits.md", names);
        Assert.DoesNotContain("credits.md", names);
    }

    [Fact]
    public void Pack_RefusesTwoFollowersWithTheSamePlugin_AndWritesNothing()
    {
        var first = Build("Anna");
        var copy = Path.Combine(_ws, "elsewhere", "Anna");
        Directory.CreateDirectory(copy);
        foreach (var file in Directory.EnumerateFiles(first.OutputDirectory, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(copy, Path.GetRelativePath(first.OutputDirectory, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        File.AppendAllText(Path.Combine(copy, "FF_Anna.esp"), "different");
        var zip = Path.Combine(_ws, "clash.zip");

        var error = Assert.Throws<InvalidOperationException>(() =>
            new FollowerPackager(Log).Pack([first.OutputDirectory, copy], zip));

        Assert.Contains("FF_Anna.esp", error.Message);
        Assert.False(File.Exists(zip));
    }

    [Fact]
    public void Pack_RefusesAFolderWithNoPlugin()
    {
        var empty = Path.Combine(_ws, "empty");
        Directory.CreateDirectory(empty);
        Assert.Throws<InvalidDataException>(() =>
            new FollowerPackager(Log).Pack([empty], Path.Combine(_ws, "x.zip")));
    }

    // ---------------- repair ----------------

    [Fact]
    public void Repair_FixesWhatOldBuildsWroteWrong_InACopy()
    {
        var source = Path.Combine(_ws, "installed");
        Directory.CreateDirectory(source);
        var original = OldStylePlugin(source);
        var before = File.ReadAllBytes(original);
        var whiterun = Whiterun();

        var result = new PluginRepairer(Log).Repair(original, Path.Combine(_ws, "repaired"),
            key => key == whiterun.FormKey ? whiterun : null);

        Assert.True(result.Changed);
        Assert.Empty(result.Unresolved);
        Assert.False(result.Gate.HasErrors);
        Assert.Equal(before, File.ReadAllBytes(original));

        using var fixedMod = SkyrimMod.CreateFromBinaryOverlay(result.OutputPath!, VanillaForms.Release);
        using var oldMod = SkyrimMod.CreateFromBinaryOverlay(original, VanillaForms.Release);
        var npc = fixedMod.Npcs.First();
        Assert.Equal(100, npc.Configuration.SpeedMultiplier);
        Assert.Equal(1f, npc.Height);
        Assert.Equal(35, npc.Configuration.DispositionBase);
        Assert.Equal(255, npc.NAM5);
        Assert.Equal(SoundLevel.Normal, npc.SoundLevel);
        Assert.Equal(1f, Assert.IsAssignableFrom<IPcLevelMultGetter>(npc.Configuration.Level).LevelMult, 3);
        Assert.Equal("WhiterunWorld", fixedMod.Worldspaces.First().EditorID);
        // Same records, same IDs, same masters: only the values changed.
        Assert.Equal(oldMod.EnumerateMajorRecords().Select(r => r.FormKey).Order(),
            fixedMod.EnumerateMajorRecords().Select(r => r.FormKey).Order());
        Assert.Equal(oldMod.ModHeader.MasterReferences.Select(m => m.Master),
            fixedMod.ModHeader.MasterReferences.Select(m => m.Master));
        Assert.True(fixedMod.ModHeader.Flags.HasFlag(SkyrimModHeader.HeaderFlag.Small));
        Assert.Single(fixedMod.Worldspaces.First().TopCell!.Persistent);
    }

    [Fact]
    public void Repair_LeavesACurrentPluginAlone()
    {
        var dir = Path.Combine(_ws, "current");
        Directory.CreateDirectory(dir);
        var result = new FollowerCompiler(Log).Compile(Profile("Fresh"), location: null,
            worldspaceSource: _ => Whiterun());
        var path = Path.Combine(dir, result.Mod.ModKey.FileName);
        new PluginWriter(Log).Write(result.Mod, path);

        var repair = new PluginRepairer(Log).Repair(path, Path.Combine(_ws, "repaired"));

        Assert.False(repair.Changed);
        Assert.Null(repair.OutputPath);
    }

    [Fact]
    public void Repair_SaysSoWhenTheWorldspaceCannotBeRead()
    {
        var dir = Path.Combine(_ws, "installed");
        Directory.CreateDirectory(dir);
        var repair = new PluginRepairer(Log).Repair(OldStylePlugin(dir), Path.Combine(_ws, "repaired"));

        Assert.True(repair.Changed);
        Assert.Contains(repair.Unresolved, u => u.Contains("worldspace"));
    }

    [Fact]
    public void Repair_RefusesToOverwriteTheOriginal()
    {
        var dir = Path.Combine(_ws, "installed");
        Directory.CreateDirectory(dir);
        var original = OldStylePlugin(dir);
        Assert.Throws<ArgumentException>(() => new PluginRepairer(Log).Repair(original, dir));
    }

    [Theory]
    [InlineData(16.96f, 1.0f)]   // 1.0 stored twice-scaled
    [InlineData(29.104f, 0.75f)]
    [InlineData(41.248f, 0.5f)]
    public void TheDoubleScaledLevelMultiplierIsUndone(float stored, float meant)
    {
        Assert.Equal(meant, PluginRepairer.UndoDoubleScaledMultiplier(stored)!.Value, 3);
    }
}
