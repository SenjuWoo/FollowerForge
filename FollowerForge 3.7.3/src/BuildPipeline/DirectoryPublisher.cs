using Serilog;

namespace FollowerForge.BuildPipeline;

internal static class DirectoryPublisher
{
    // ponytail: rollback for IO failures, not crash-atomic or concurrent same-target builds.
    internal static void Publish(string staging, string finalDir, ILogger log)
    {
        var finalParent = Path.GetDirectoryName(finalDir)!;
        // Prepare on the destination volume before touching a previous successful build.
        Directory.CreateDirectory(finalParent);
        var incoming = Path.Combine(finalParent, ".ff-incoming-" + Guid.NewGuid().ToString("N"));
        var backup = Path.Combine(finalParent, ".ff-backup-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(incoming);
            foreach (var directory in Directory.EnumerateDirectories(staging, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(incoming, Path.GetRelativePath(staging, directory)));
            foreach (var file in Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(incoming, Path.GetRelativePath(staging, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
            if (Directory.Exists(finalDir)) Directory.Move(finalDir, backup);
            try { Directory.Move(incoming, finalDir); }
            catch
            {
                if (Directory.Exists(backup)) Directory.Move(backup, finalDir);
                throw;
            }
        }
        finally
        {
            if (Directory.Exists(incoming)) Directory.Delete(incoming, recursive: true);
        }
        // Publication succeeded. Cleanup failure must not misreport it as a failed build.
        foreach (var obsolete in new[] { backup, staging })
        {
            try { if (Directory.Exists(obsolete)) Directory.Delete(obsolete, recursive: true); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { log.Warning(ex, "Published successfully; could not remove {Directory}", obsolete); }
        }
    }
}
