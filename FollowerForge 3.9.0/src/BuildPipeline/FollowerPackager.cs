using System.IO.Compression;
using System.Text;
using System.Text.Json;
using FollowerForge.Domain;
using Serilog;

namespace FollowerForge.BuildPipeline;

/// <summary>
/// Puts several built followers into one installable archive — asked for by people who make a set
/// of followers and want to share or install them as a single mod.
///
/// Followers built by 3.9.0 or later share no file path, so a pack is their union. Older builds
/// kept their reports at the package root under the same names; those are moved into each
/// follower's own FollowerForge\ folder inside the pack instead of overwriting one another.
/// Anything that would still land on the same path with different content stops the pack: a pack
/// that silently kept one of two files would ship a follower with someone else's face.
/// </summary>
public sealed class FollowerPackager(ILogger log)
{
    public sealed record PackResult(string ZipPath, IReadOnlyList<string> Followers, int Files, int Merged);

    private static readonly string[] PluginExtensions = [".esp", ".esm", ".esl"];

    /// <summary>A file a mod reads at a fixed name, merged line by line rather than refused.</summary>
    private const string RsvExclusion = "RSVexclude.ini";

    public PackResult Pack(IReadOnlyList<string> followerFolders, string zipPath)
    {
        if (followerFolders.Count == 0)
            throw new ArgumentException("Choose at least one follower folder to pack.", nameof(followerFolders));

        // Package-relative path → bytes, plus where each came from for the error message.
        var files = new SortedDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var conflicts = new List<string>();
        var followers = new List<string>();
        var merged = 0;
        var newest = DateTimeOffset.MinValue;

        foreach (var folder in followerFolders.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var plugins = Directory.EnumerateFiles(folder)
                .Where(f => PluginExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (plugins.Count == 0)
                throw new InvalidDataException($"'{folder}' holds no plugin, so it is not a built follower.");
            var label = Path.GetFileName(folder);
            followers.Add($"{label} ({string.Join(", ", plugins.Select(Path.GetFileName))})");
            var legacyReports = PackageLayout.ReportFolder(Path.GetFileName(plugins[0]));

            foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file).Equals(".zip", StringComparison.OrdinalIgnoreCase)) continue;
                var rel = Path.GetRelativePath(folder, file);
                var atRoot = !rel.Contains(Path.DirectorySeparatorChar);
                // An older build's root-level report goes into that follower's own report folder.
                if (atRoot
                    && !PluginExtensions.Contains(Path.GetExtension(rel), StringComparer.OrdinalIgnoreCase)
                    && !rel.Equals(RsvExclusion, StringComparison.OrdinalIgnoreCase))
                    rel = Path.Combine(legacyReports, rel);

                var bytes = File.ReadAllBytes(file);
                if (rel.EndsWith(PackageLayout.ManifestFile, StringComparison.OrdinalIgnoreCase))
                    newest = Max(newest, ManifestTime(bytes));

                if (!files.TryGetValue(rel, out var existing))
                {
                    files[rel] = bytes;
                    owners[rel] = label;
                    continue;
                }
                if (existing.AsSpan().SequenceEqual(bytes)) continue;
                if (Path.GetFileName(rel).Equals(RsvExclusion, StringComparison.OrdinalIgnoreCase))
                {
                    files[rel] = MergeIni(existing, bytes);
                    merged++;
                    continue;
                }
                conflicts.Add($"{rel} ({owners[rel]} and {label})");
            }
        }

        if (conflicts.Count > 0)
            throw new InvalidOperationException(
                "These followers would overwrite each other's files, so no pack was written: "
                + string.Join("; ", conflicts.Take(8))
                + (conflicts.Count > 8 ? $" and {conflicts.Count - 8} more" : "")
                + ". Two followers with the same plugin name cannot share a pack; rename one and rebuild it.");

        var packName = Path.GetFileNameWithoutExtension(zipPath);
        files[Path.Combine(PackageLayout.ReportsRoot, packName + " - contents.txt")] = Encoding.UTF8.GetBytes(
            $"{packName}\r\n\r\nFollowers in this pack:\r\n"
            + string.Concat(followers.Select(f => $"  - {f}\r\n"))
            + "\r\nInstall the archive once with Vortex or MO2 and enable every plugin it contains.\r\n");

        var stamp = newest == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow : newest;
        var local = stamp.ToLocalTime();
        if (local.Year < 1980) local = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(zipPath))!);
        var temp = zipPath + ".partial";
        if (File.Exists(temp)) File.Delete(temp);
        using (var zip = ZipFile.Open(temp, ZipArchiveMode.Create))
        {
            foreach (var (rel, bytes) in files)
            {
                var entry = zip.CreateEntry(rel.Replace('\\', '/'), CompressionLevel.Optimal);
                entry.LastWriteTime = local;
                using var stream = entry.Open();
                stream.Write(bytes);
            }
        }
        // Only a finished archive takes the real name, so a failure never leaves half a pack behind.
        File.Move(temp, zipPath, overwrite: true);

        log.Information("Packed {Count} follower(s) into {Zip} ({Files} files, {Merged} merged)",
            followers.Count, zipPath, files.Count, merged);
        return new PackResult(zipPath, followers, files.Count, merged);
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;

    private static DateTimeOffset ManifestTime(byte[] manifest)
    {
        try
        {
            using var doc = JsonDocument.Parse(manifest);
            return doc.RootElement.TryGetProperty("GeneratedAtUtc", out var at)
                   && DateTimeOffset.TryParse(at.GetString(), out var parsed)
                ? parsed
                : DateTimeOffset.MinValue;
        }
        catch (JsonException)
        {
            return DateTimeOffset.MinValue;
        }
    }

    /// <summary>The first file's lines, then every line of the second that the first lacks.</summary>
    private static byte[] MergeIni(byte[] first, byte[] second)
    {
        static string[] Lines(byte[] b) => Encoding.UTF8.GetString(b).Replace("\r\n", "\n").Split('\n');
        var lines = Lines(first).ToList();
        var known = new HashSet<string>(lines.Select(l => l.Trim()), StringComparer.OrdinalIgnoreCase);
        foreach (var line in Lines(second))
            if (line.Trim().Length > 0 && !line.TrimStart().StartsWith(';') && known.Add(line.Trim()))
                lines.Add(line);
        return Encoding.UTF8.GetBytes(string.Join("\r\n", lines).TrimEnd() + "\r\n");
    }
}
