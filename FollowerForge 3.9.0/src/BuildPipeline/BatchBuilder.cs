using FollowerForge.AssetIndex;
using FollowerForge.Domain;
using FollowerForge.ModManagers;
using Serilog;

namespace FollowerForge.BuildPipeline;

/// <summary>
/// Builds many followers in one pass, sharing the placement worldspace and catalogue so a
/// batch is not paying per-follower open costs. Each follower is still an independent atomic
/// build; one failure does not abort the rest.
/// </summary>
public sealed class BatchBuilder(ILogger log)
{
    public sealed record BatchItemResult(string Name, bool Success, string OutputDirectory, int Errors, int Warnings);
    public sealed record BatchResult(IReadOnlyList<BatchItemResult> Items)
    {
        public int Succeeded => Items.Count(i => i.Success);
        public int Failed => Items.Count(i => !i.Success);
    }

    public BatchResult Build(
        IReadOnlyList<FollowerProfile> profiles, EnvironmentSnapshot env, string workspaceRoot,
        CatalogDb? catalog = null, string? publishRoot = null)
        => Run(profiles.Select(p => (p, publishRoot)).ToList(), env, workspaceRoot, catalog);

    /// <summary>
    /// Rebuilds followers that were already built, each into the folder it was found in, so an
    /// installed mod folder is replaced rather than duplicated. This is how every follower built
    /// before 3.8.0 gets its speed, height and level multiplier fixed in one go.
    /// </summary>
    public BatchResult Rebuild(
        IReadOnlyList<LibraryEntry> entries, EnvironmentSnapshot env, string workspaceRoot,
        CatalogDb? catalog = null)
    {
        var work = new List<(FollowerProfile, string?)>();
        var failed = new List<BatchItemResult>();
        foreach (var entry in entries)
        {
            try
            {
                work.Add((ProfileIo.Load(entry.ProfilePath), Path.GetDirectoryName(entry.FolderPath)));
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException)
            {
                log.Error(ex, "Could not read {Profile}", entry.ProfilePath);
                failed.Add(new BatchItemResult(entry.Name, false, entry.FolderPath, 1, 0));
            }
        }
        var result = Run(work, env, workspaceRoot, catalog);
        return new BatchResult([.. failed, .. result.Items]);
    }

    private BatchResult Run(
        IReadOnlyList<(FollowerProfile Profile, string? PublishRoot)> work, EnvironmentSnapshot env,
        string workspaceRoot, CatalogDb? catalog)
    {
        var builder = new FollowerBuilder(log);
        var items = new List<BatchItemResult>();
        foreach (var (profile, publishRoot) in work)
        {
            try
            {
                // Each follower resolves its own LocationId from the shared library.
                var result = builder.Build(profile, env, workspaceRoot, location: null, catalog, publishRoot);
                items.Add(new BatchItemResult(
                    profile.Name, result.Success, result.OutputDirectory,
                    result.Validation.Findings.Count(f => f.Severity == ValidationSeverity.Error),
                    result.Validation.Findings.Count(f => f.Severity == ValidationSeverity.Warning)));
                log.Information("Batch [{Name}]: {Status}", profile.Name, result.Success ? "OK" : "FAILED");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Batch item {Name} threw", profile.Name);
                items.Add(new BatchItemResult(profile.Name, false, "", 1, 0));
            }
        }
        log.Information("Batch complete: {Ok}/{Total} succeeded", items.Count(i => i.Success), items.Count);
        return new BatchResult(items);
    }

    /// <summary>
    /// Loads every *.json profile in a file or directory, plus the rebuild profile of every built
    /// follower found in it — so a builds folder can be handed over as it is.
    /// </summary>
    public static IReadOnlyList<FollowerProfile> LoadProfiles(string pathOrDir)
    {
        if (File.Exists(pathOrDir)) return [ProfileIo.Load(pathOrDir)];
        if (Directory.Exists(pathOrDir))
        {
            var loose = Directory.EnumerateFiles(pathOrDir, "*.json", SearchOption.TopDirectoryOnly)
                .Where(p => !Path.GetFileName(p).Equals(PackageLayout.ManifestFile, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p, StringComparer.Ordinal);
            var built = FollowerLibrary.Scan([pathOrDir])
                .Select(e => e.ProfilePath)
                .OrderBy(p => p, StringComparer.Ordinal);
            return loose.Concat(built)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(ProfileIo.Load)
                .ToList();
        }
        throw new FileNotFoundException($"No profile file or directory at {pathOrDir}");
    }

}
