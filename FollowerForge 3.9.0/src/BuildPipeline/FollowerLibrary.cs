using System.Text.Json;
using FollowerForge.Domain;
using FollowerForge.ModManagers;
using FollowerForge.SkyrimRecords;
using Mutagen.Bethesda.Skyrim;

namespace FollowerForge.BuildPipeline;

/// <summary>One follower that was already built, found on disk.</summary>
/// <param name="FolderPath">The follower's package folder (what Vortex/MO2 installs).</param>
/// <param name="NeedsRebuild">
/// Her plugin carries a value every build before 3.8.0 wrote wrong (speed multiplier or height of
/// zero). Read from the plugin itself, because a manifest from those builds does not say which
/// version wrote it.
/// </param>
public sealed record LibraryEntry(
    string Name,
    string PluginName,
    string ProfilePath,
    string FolderPath,
    DateTimeOffset BuiltUtc,
    bool NeedsRebuild,
    string? GeneratorVersion);

/// <summary>
/// Finds the followers a user has built, so they can be reopened, rebuilt or packed without
/// anyone having to remember where a build went.
///
/// A follower is a folder holding exactly one rebuild profile, either under FollowerForge\ (3.9.0
/// and later) or at its root (earlier builds). A folder holding several is a pack, not a follower.
/// </summary>
public static class FollowerLibrary
{
    /// <summary>Where the window publishes builds when no output folder was chosen.</summary>
    public static string DefaultBuildsRoot => Path.Combine(AppUserSettings.DefaultWorkspaceRoot, "builds");

    /// <summary>Every place this user's builds can be: the chosen output folder, then the default.</summary>
    public static IReadOnlyList<string> BuildRoots(string? configuredRoot)
    {
        var roots = new List<string>();
        if (!string.IsNullOrWhiteSpace(configuredRoot)) roots.Add(Path.GetFullPath(configuredRoot.Trim().Trim('"')));
        roots.Add(DefaultBuildsRoot);
        return roots.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Followers directly inside any of <paramref name="roots"/>, newest first. A root that is itself
    /// one follower's folder is accepted too. Unreadable profiles are reported, not thrown.
    /// </summary>
    public static IReadOnlyList<LibraryEntry> Scan(IEnumerable<string> roots, List<string>? problems = null)
    {
        var entries = new List<LibraryEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots.Where(Directory.Exists))
        {
            var candidates = new[] { root }.Concat(SafeSubdirectories(root));
            foreach (var folder in candidates)
            {
                var profiles = PackageLayout.FindAll(folder, PackageLayout.ProfileFile);
                if (profiles.Count != 1 || !seen.Add(Path.GetFullPath(folder))) continue;
                try
                {
                    entries.Add(Describe(folder, profiles[0]));
                }
                catch (Exception ex) when (ex is JsonException or IOException or InvalidDataException
                                               or UnauthorizedAccessException)
                {
                    problems?.Add($"{profiles[0]}: {ex.Message}");
                }
            }
        }
        return entries.OrderByDescending(e => e.BuiltUtc).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IEnumerable<string> SafeSubdirectories(string root)
    {
        try { return Directory.EnumerateDirectories(root).ToList(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return []; }
    }

    private static LibraryEntry Describe(string folder, string profilePath)
    {
        var profile = ProfileIo.Load(profilePath);
        var built = File.GetLastWriteTimeUtc(profilePath);
        string? version = null;
        if (Path.Combine(Path.GetDirectoryName(profilePath)!, PackageLayout.ManifestFile) is var manifest
            && File.Exists(manifest))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
                if (doc.RootElement.TryGetProperty("GeneratedAtUtc", out var at)
                    && DateTimeOffset.TryParse(at.GetString(), out var parsed))
                    built = parsed.UtcDateTime;
                if (doc.RootElement.TryGetProperty("GeneratorVersion", out var v))
                    version = v.GetString();
            }
            catch (JsonException) { /* the profile is what matters; the date falls back to the file's */ }
        }

        var plugin = Path.Combine(folder, profile.PluginName);
        return new LibraryEntry(profile.Name, profile.PluginName, profilePath, folder,
            new DateTimeOffset(built, TimeSpan.Zero), NeedsRebuild(plugin), version);
    }

    /// <summary>
    /// True when a plugin holds an NPC with a speed multiplier or height of zero: what every build
    /// before 3.8.0 wrote. False when the plugin is missing or cannot be read.
    /// </summary>
    public static bool NeedsRebuild(string pluginPath)
    {
        if (!File.Exists(pluginPath)) return false;
        try
        {
            using var mod = SkyrimMod.CreateFromBinaryOverlay(pluginPath, VanillaForms.Release);
            return mod.Npcs.Any(npc => npc.FormKey.ModKey == mod.ModKey
                                       && (npc.Configuration.SpeedMultiplier == 0 || npc.Height <= 0f));
        }
        catch (Exception)
        {
            return false;
        }
    }
}
