namespace FollowerForge.Domain;

/// <summary>
/// Where a follower package keeps the files that are for people rather than for the game.
///
/// A package is installed into Data as it stands. Through 3.8.0 the build wrote its reports —
/// manifest, credits, build report, rebuild profile, test notes — at the package root, so every
/// follower dropped files with the same names into the root of Data. Vortex and MO2 report any two
/// followers as conflicting, and only the last one installed kept its rebuild profile. Reports now
/// live in FollowerForge\&lt;plugin name&gt;\, which is unique per follower and which the game never
/// reads.
/// </summary>
public static class PackageLayout
{
    /// <summary>Top-level folder for every follower's reports.</summary>
    public const string ReportsRoot = "FollowerForge";

    public const string ProfileFile = "rebuild-profile.json";
    public const string ManifestFile = "manifest.json";

    /// <summary>Package-relative folder holding one follower's reports.</summary>
    public static string ReportFolder(string pluginName) =>
        Path.Combine(ReportsRoot, Path.GetFileNameWithoutExtension(pluginName));

    /// <summary>
    /// Every copy of <paramref name="fileName"/> in a package: the current per-follower folders
    /// first, then the package root, where builds before 3.9.0 left it.
    /// </summary>
    public static IReadOnlyList<string> FindAll(string packageRoot, string fileName)
    {
        var found = new List<string>();
        if (!Directory.Exists(packageRoot)) return found;

        var reports = Path.Combine(packageRoot, ReportsRoot);
        if (Directory.Exists(reports))
            found.AddRange(Directory.EnumerateDirectories(reports)
                .Select(folder => Path.Combine(folder, fileName))
                .Where(File.Exists)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase));

        var legacy = Path.Combine(packageRoot, fileName);
        if (File.Exists(legacy)) found.Add(legacy);
        return found;
    }

    /// <summary>The single copy of a report in a one-follower package, or null.</summary>
    public static string? Find(string packageRoot, string fileName) =>
        FindAll(packageRoot, fileName).FirstOrDefault();
}
