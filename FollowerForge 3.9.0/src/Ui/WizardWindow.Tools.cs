using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FollowerForge.AssetIndex;
using FollowerForge.BuildPipeline;
using FollowerForge.Domain;
using FollowerForge.ModManagers;
using FollowerForge.SkyrimRecords;

namespace FollowerForge.Ui;

/// <summary>
/// Working with followers that already exist: the library behind Open, drafts, rebuilding every
/// follower at once, packing several into one archive, and repairing a plugin an older version
/// wrote. All of it reuses the command palette as its chooser, so there is one list to learn.
/// </summary>
public partial class WizardWindow
{
    /// <summary>Unfinished followers saved without building, kept where Open finds them.</summary>
    internal static string DraftsRoot => Path.Combine(AppUserSettings.DefaultWorkspaceRoot, "drafts");

    private IReadOnlyList<string> LibraryRoots() =>
        FollowerLibrary.BuildRoots(AppUserSettings.Load(warning: m => Log.Warning("{Warning}", m)).WorkspaceRoot);

    private bool RequireSetup(string what)
    {
        if (IsSetupLoaded) return true;
        SetStatus($"Still reading your setup — {what} once the lists have loaded.");
        return false;
    }

    /// <summary>Shows <paramref name="options"/> in the palette instead of the commands.</summary>
    private void OpenChooser(string placeholder, IReadOnlyList<CommandOption> options)
    {
        _paletteSource = options;
        Ctl<Border>("CommandPaletteOverlay").IsVisible = true;
        var search = Ctl<TextBox>("CommandSearch");
        search.Text = "";
        search.PlaceholderText = placeholder;
        ShowCommands(options);
        search.Focus();
    }

    private static string Ago(DateTimeOffset when)
    {
        var age = DateTimeOffset.UtcNow - when;
        return age.TotalHours < 1 ? "just now"
            : age.TotalDays < 1 ? $"{(int)age.TotalHours} h ago"
            : age.TotalDays < 60 ? $"{(int)age.TotalDays} d ago"
            : when.ToLocalTime().ToString("yyyy-MM-dd");
    }

    // ---------- open ----------

    private void OnOpenFollower(object? sender, RoutedEventArgs e)
    {
        if (!RequireSetup("open a follower")) return;

        var options = new List<CommandOption>();
        foreach (var entry in FollowerLibrary.Scan(LibraryRoots()))
        {
            var captured = entry;
            options.Add(new CommandOption(
                captured.Name,
                $"{captured.PluginName} · built {Ago(captured.BuiltUtc)}"
                + (captured.NeedsRebuild ? " · NEEDS REBUILD" : "")
                + $" · {DiagnosticsReport.Redact(captured.FolderPath)}",
                () => LoadIntoWindow(captured.ProfilePath)));
        }
        if (Directory.Exists(DraftsRoot))
            foreach (var draft in Directory.EnumerateFiles(DraftsRoot, "*.json")
                         .OrderByDescending(File.GetLastWriteTimeUtc))
            {
                var path = draft;
                options.Add(new CommandOption(
                    "Draft: " + Path.GetFileNameWithoutExtension(path),
                    $"saved {Ago(File.GetLastWriteTimeUtc(path))} · not built yet",
                    () => LoadIntoWindow(path)));
            }
        options.Add(new CommandOption("Browse for a profile file…",
            "Any rebuild-profile.json or saved draft, wherever it is", () => BrowseForProfile(null, new RoutedEventArgs())));

        OpenChooser(options.Count > 1
            ? $"Open one of {options.Count - 1} follower(s) — type to filter…"
            : "No built followers found yet — browse for a profile file…", options);
    }

    private void LoadIntoWindow(string profilePath)
    {
        try
        {
            SetStatus(OpenFollower(FollowerReopen.Load(profilePath)));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not open {Profile}", profilePath);
            SetStatus("Could not open that follower: " + ex.Message);
        }
    }

    // ---------- save draft ----------

    private async void OnSaveDraft(object? sender, RoutedEventArgs e)
    {
        try
        {
            var profile = BuildProfile();
            Directory.CreateDirectory(DraftsRoot);
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save this follower without building her",
                SuggestedFileName = (string.IsNullOrWhiteSpace(profile.Name) ? "Follower" : profile.Name) + ".json",
                DefaultExtension = "json",
                FileTypeChoices = [new FilePickerFileType("Follower profile") { Patterns = ["*.json"] }],
                SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(DraftsRoot),
            });
            if (file?.TryGetLocalPath() is not { } path) return;
            ProfileIo.Save(path, profile);
            Log.Information("Saved draft {Path}", path);
            SetStatus($"Saved {profile.Name}. Open follower… lists drafts saved in the default folder.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not save a draft");
            SetStatus("Could not save: " + ex.Message);
        }
    }

    // ---------- rebuild ----------

    private void OnRebuildFollowers(object? sender, RoutedEventArgs e)
    {
        if (!RequireSetup("rebuild")) return;

        var entries = FollowerLibrary.Scan(LibraryRoots());
        if (entries.Count == 0)
        {
            SetStatus("No built followers were found in your output folders.");
            return;
        }
        var needed = entries.Where(x => x.NeedsRebuild).ToList();
        var options = new List<CommandOption>();
        if (needed.Count > 0)
            options.Add(new CommandOption($"Rebuild the {needed.Count} that need it",
                "Built before 3.8.0: speed multiplier 0, height 0, wrong level multiplier", () => RunRebuild(needed)));
        options.Add(new CommandOption($"Rebuild all {entries.Count}",
            "Every follower, each into the folder it is in now, from her own profile", () => RunRebuild(entries)));
        foreach (var entry in entries)
        {
            var one = entry;
            options.Add(new CommandOption("Rebuild " + one.Name,
                $"{one.PluginName}{(one.NeedsRebuild ? " · NEEDS REBUILD" : "")} · {DiagnosticsReport.Redact(one.FolderPath)}",
                () => RunRebuild([one])));
        }
        OpenChooser("Rebuild followers — choose which…", options);
    }

    private async void RunRebuild(IReadOnlyList<LibraryEntry> entries)
    {
        var log = Ctl<TextBox>("BuildLog");
        OpenSection(WorkspaceSection.ReviewValidationBuild);
        Ctl<Button>("BuildButton").IsEnabled = false;
        var zip = Ctl<CheckBox>("ZipBox").IsChecked == true;
        log.Text = $"Rebuilding {entries.Count} follower(s)…";
        try
        {
            var text = await Task.Run(() =>
            {
                var dbPath = CatalogBuilder.DefaultDbPath;
                using var catalog = File.Exists(dbPath) ? new CatalogDb(dbPath, Log) : null;
                var result = new BatchBuilder(Log).Rebuild(entries, _env!, AppUserSettings.DefaultWorkspaceRoot, catalog);
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Rebuilt {result.Succeeded} of {result.Items.Count}.");
                sb.AppendLine();
                foreach (var item in result.Items)
                {
                    sb.AppendLine($"{(item.Success ? "OK  " : "FAIL")}  {item.Name}  ({item.Errors} error(s), {item.Warnings} warning(s))");
                    sb.AppendLine($"      {item.OutputDirectory}");
                    var plugin = entries.FirstOrDefault(x => x.Name == item.Name)?.PluginName;
                    if (item.Success && zip && plugin is not null)
                        sb.AppendLine("      zip: " + new VortexPackager(Log)
                            .Package(item.OutputDirectory, Path.GetFileNameWithoutExtension(plugin)));
                }
                sb.AppendLine();
                sb.AppendLine(result.Failed == 0
                    ? "Reinstall or redeploy them in your mod manager. Open follower… shows any one of them in full."
                    : "Open a failed follower with Open follower… and press Build to see what stopped it.");
                return (sb.ToString(), result.Failed);
            });
            log.Text = text.Item1;
            SetStatus(text.Failed == 0 ? $"Rebuilt {entries.Count} follower(s)." : $"{text.Failed} rebuild(s) failed — see the log below.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Rebuild failed");
            log.Text = "Rebuild failed: " + ex.Message;
        }
        finally
        {
            Ctl<Button>("BuildButton").IsEnabled = true;
        }
    }

    /// <summary>
    /// Said once when the window is ready: how many of this user's followers carry the speed-0
    /// defect, and the one click that fixes them. Empty when none do.
    /// </summary>
    private string RebuildNotice()
    {
        try
        {
            var entries = FollowerLibrary.Scan(LibraryRoots());
            var count = entries.Count(x => x.NeedsRebuild);
            Ctl<TextBlock>("LibrarySummary").Text = entries.Count == 0
                ? "No built followers yet"
                : $"{entries.Count} follower(s) built"
                  + (count > 0 ? $" · {count} need a rebuild (built before 3.8.0)" : " · all current");
            return count == 0 ? "" : $"  {count} of your followers were built before 3.8.0 and may run on the spot — "
                                     + "Rebuild followers… on the Studio page fixes them.";
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not scan built followers");
            return "";
        }
    }

    // ---------- pack ----------

    private void OnMakePack(object? sender, RoutedEventArgs e)
    {
        var entries = FollowerLibrary.Scan(LibraryRoots());
        var options = new List<CommandOption>
        {
            new("Choose followers…", "Pick the follower folders to put in one archive",
                () => ChoosePackFolders(null, new RoutedEventArgs())),
        };
        if (entries.Count > 1)
            options.Insert(0, new CommandOption($"Pack all {entries.Count} followers",
                string.Join(", ", entries.Take(6).Select(x => x.Name)) + (entries.Count > 6 ? ", …" : ""),
                () => SavePack(entries.Select(x => x.FolderPath).ToList())));
        OpenChooser("Make one installable archive from several followers…", options);
    }

    private async void ChoosePackFolders(object? sender, RoutedEventArgs e)
    {
        try
        {
            var start = LibraryRoots().FirstOrDefault(Directory.Exists);
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choose the follower folders to pack (Ctrl+click for several)",
                AllowMultiple = true,
                SuggestedStartLocation = start is null ? null : await StorageProvider.TryGetFolderFromPathAsync(start),
            });
            var paths = folders.Select(f => f.TryGetLocalPath()).OfType<string>().ToList();
            if (paths.Count > 0) SavePack(paths);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not choose pack folders");
            SetStatus("Could not choose folders: " + ex.Message);
        }
    }

    private async void SavePack(IReadOnlyList<string> folders)
    {
        var log = Ctl<TextBox>("BuildLog");
        try
        {
            var start = LibraryRoots().FirstOrDefault(Directory.Exists);
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save the follower pack",
                SuggestedFileName = "My followers.zip",
                DefaultExtension = "zip",
                FileTypeChoices = [new FilePickerFileType("Zip archive") { Patterns = ["*.zip"] }],
                SuggestedStartLocation = start is null ? null : await StorageProvider.TryGetFolderFromPathAsync(start),
            });
            if (file?.TryGetLocalPath() is not { } zip) return;

            OpenSection(WorkspaceSection.ReviewValidationBuild);
            log.Text = $"Packing {folders.Count} follower(s)…";
            var result = await Task.Run(() => new FollowerPackager(Log).Pack(folders, zip));
            log.Text = $"Pack written: {result.ZipPath}\n\n"
                       + string.Concat(result.Followers.Select(f => $"  - {f}\n"))
                       + $"\n{result.Files} files. Install it with Vortex or MO2 like any other mod.";
            _lastOutputDir = Path.GetDirectoryName(result.ZipPath);
            Ctl<Button>("OpenFolderButton").IsEnabled = true;
            SetStatus($"Packed {result.Followers.Count} follower(s).");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Pack failed");
            log.Text = "No pack was written. " + ex.Message;
            SetStatus("Pack failed — see the log below.");
        }
    }

    // ---------- repair ----------

    private async void OnRepairPlugin(object? sender, RoutedEventArgs e)
    {
        var log = Ctl<TextBox>("BuildLog");
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose a follower plugin an older FollowerForge built",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Skyrim plugin") { Patterns = ["*.esp", "*.esl", "*.esm"] }],
            });
            if (files.Count == 0 || files[0].TryGetLocalPath() is not { } plugin) return;

            OpenSection(WorkspaceSection.ReviewValidationBuild);
            log.Text = "Repairing…";
            var outDir = Path.Combine(AppUserSettings.DefaultWorkspaceRoot, "repaired");
            var env = _env;
            var result = await Task.Run(() =>
            {
                CellSourceResolver? resolver = env is null
                    ? null
                    : new CellSourceResolver(FollowerBuilder.EnsurePluginReadRoot(env, Log), Log);
                try
                {
                    return new PluginRepairer(Log).Repair(plugin, outDir, resolver is null ? null : resolver.ResolveWorldspace);
                }
                finally { resolver?.Dispose(); }
            });

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(Path.GetFileName(plugin));
            sb.AppendLine();
            if (!result.Changed) sb.AppendLine("Nothing to repair: this plugin has none of the values older builds wrote wrong.");
            foreach (var c in result.Changes) sb.AppendLine("  fixed  " + c);
            foreach (var u in result.Unresolved) sb.AppendLine("  left   " + u);
            foreach (var f in result.Gate.Findings) sb.AppendLine($"  [{f.Severity}] {f.Code}: {f.Message}");
            if (result.OutputPath is { } output)
            {
                sb.AppendLine();
                sb.AppendLine("Repaired copy: " + output);
                sb.AppendLine("Replace the plugin in your installed follower with this file, then redeploy.");
                sb.AppendLine("The original was not changed. If you still have her rebuild-profile.json,");
                sb.AppendLine("Rebuild followers… is the better fix: it also corrects her other records.");
                _lastOutputDir = Path.GetDirectoryName(output);
                Ctl<Button>("OpenFolderButton").IsEnabled = true;
            }
            log.Text = sb.ToString();
            SetStatus(result.Changed ? $"Repaired {result.Changes.Count} value(s)." : "Nothing to repair.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Repair failed");
            log.Text = "Repair failed: " + ex.Message;
        }
    }
}
