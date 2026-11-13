using FollowerForge.Domain;
using FollowerForge.ModManagers;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;

namespace FollowerForge.SkyrimRecords;

/// <summary>
/// Builds the game's actual load order from the active manager profile:
/// loadorder.txt gives the order; plugins.txt gives enablement; the base game
/// masters, Creation Club plugins and _ResourcePack.esl are implicitly enabled.
/// Vortex plugins live in game Data; MO2 plugins are hardlinked into a FollowerForge view.
/// </summary>
public sealed class LoadOrderBuilder(ILogger log)
{
    /// <param name="Unreadable">
    /// Enabled plugins that exist but could not be opened, and were left out so the rest of the
    /// load order could still be read.
    /// </param>
    public sealed record BuiltLoadOrder(
        ILoadOrder<IModListing<ISkyrimModGetter>> LoadOrder,
        IReadOnlyList<LoadOrderEntry> Entries,
        IReadOnlyList<string> MissingPlugins,
        IReadOnlyList<UnreadablePlugin>? Unreadable = null);

    /// <summary>Computes the effective entry list without opening any plugin.</summary>
    public (IReadOnlyList<LoadOrderEntry> Entries, IReadOnlyList<string> Missing) BuildEntryList(
        EnvironmentSnapshot env)
    {
        if (env.ActiveProfileId is null)
            throw new InvalidOperationException("No active mod-manager profile detected.");

        var profDir = Path.Combine(env.ProfilesPath, env.ActiveProfileId);
        var loadOrderTxt = Path.Combine(profDir, "loadorder.txt");
        var pluginsTxt = Path.Combine(profDir, "plugins.txt");
        if (!File.Exists(loadOrderTxt) || !File.Exists(pluginsTxt))
            throw new FileNotFoundException($"Profile {env.ActiveProfileId} lacks loadorder.txt/plugins.txt");

        var order = PluginLists.ParseLoadOrderTxt(loadOrderTxt);
        var enabled = PluginLists.ParsePluginsTxt(pluginsTxt)
            .Where(e => e.Enabled)
            .Select(e => e.PluginFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var entries = new List<LoadOrderEntry>(order.Count);
        var missing = new List<string>();
        foreach (var name in order)
        {
            var isEnabled = enabled.Contains(name) || IsImplicitlyEnabled(name);
            if (isEnabled && !PluginExists(env, name))
            {
                missing.Add(name);
                isEnabled = false;
            }
            entries.Add(new LoadOrderEntry(name, isEnabled, entries.Count));
        }
        if (missing.Count > 0)
            log.Warning("{Count} enabled plugins missing from Data/mods: {First}…", missing.Count, missing[0]);
        return (entries, missing);
    }

    /// <summary>Opens the enabled plugins with Mutagen (lazy binary overlays).</summary>
    public BuiltLoadOrder Build(EnvironmentSnapshot env)
    {
        var (entries, missing) = BuildEntryList(env);

        var pluginDir = env.GameDataPath;
        if (env.Manager == ModManagerKind.Mo2)
        {
            new Mo2DataView(log).Ensure(env, entries);
            pluginDir = env.PluginDataPath;
        }

        var names = entries.Where(e => e.Enabled).Select(e => e.PluginFileName).ToList();

        log.Information("Importing load order ({Manager}): {Count} enabled plugins from {Dir}…",
            env.ManagerLabel, names.Count, pluginDir);
        var lo = ImportTolerant(pluginDir, names, log, out var unreadable);
        return new BuiltLoadOrder(lo, entries, missing, unreadable);
    }

    /// <summary>
    /// Opens the load order, and when that fails finds out which plugins are responsible instead
    /// of giving up on all of them.
    ///
    /// The fast path is one import. Only when it throws is every plugin probed on its own; the ones
    /// that cannot be opened are named and left out, and the import is repeated without them. If
    /// probing finds nothing wrong the original failure is rethrown, because hiding it would turn an
    /// error nobody understands into an index that is quietly incomplete.
    /// </summary>
    internal static ILoadOrder<IModListing<ISkyrimModGetter>> ImportTolerant(
        string pluginDir, IReadOnlyList<string> pluginFileNames, ILogger log,
        out IReadOnlyList<UnreadablePlugin> unreadable)
    {
        ILoadOrder<IModListing<ISkyrimModGetter>> Import(IEnumerable<string> names) =>
            LoadOrder.Import<ISkyrimModGetter>(
                new DirectoryPath(pluginDir),
                names.Select(name => new LoadOrderListing(ModKey.FromFileName(name), enabled: true)),
                GameRelease.SkyrimSE);

        try
        {
            unreadable = [];
            return Import(pluginFileNames);
        }
        catch (Exception first)
        {
            log.Warning("Load order import failed ({Error}); checking each plugin on its own…",
                UnreadablePlugin.RootMessage(first));

            var readable = new List<string>(pluginFileNames.Count);
            var bad = new List<UnreadablePlugin>();
            foreach (var name in pluginFileNames)
            {
                try
                {
                    using var probe = SkyrimMod.CreateFromBinaryOverlay(
                        Path.Combine(pluginDir, name), SkyrimRelease.SkyrimSE);
                    _ = probe.ModHeader.MasterReferences.Count;
                    readable.Add(name);
                }
                catch (Exception ex)
                {
                    var reason = UnreadablePlugin.RootMessage(ex);
                    bad.Add(new UnreadablePlugin(name, reason));
                    log.Warning("Plugin {Plugin} cannot be opened and was left out: {Error}", name, reason);
                }
            }

            if (bad.Count == 0) throw;
            unreadable = bad;
            return Import(readable);
        }
    }

    private static bool PluginExists(EnvironmentSnapshot env, string pluginFileName) =>
        env.Manager == ModManagerKind.Mo2
            ? Mo2PathResolver.ResolvePlugin(env, pluginFileName) is not null
            : File.Exists(Path.Combine(env.GameDataPath, pluginFileName));

    public static bool IsImplicitlyEnabled(string pluginFileName) =>
        PluginLists.ImplicitBaseMasters.Contains(pluginFileName, StringComparer.OrdinalIgnoreCase)
        || pluginFileName.StartsWith("cc", StringComparison.OrdinalIgnoreCase)
           && (pluginFileName.EndsWith(".esm", StringComparison.OrdinalIgnoreCase)
               || pluginFileName.EndsWith(".esl", StringComparison.OrdinalIgnoreCase))
        || string.Equals(pluginFileName, "_ResourcePack.esl", StringComparison.OrdinalIgnoreCase);
}

/// <summary>A plugin in the load order that could not be read, and the underlying reason.</summary>
public sealed record UnreadablePlugin(string Plugin, string Reason)
{
    /// <summary>
    /// The message of the exception that actually went wrong. Mutagen reaches records through
    /// reflection and tasks, so the outermost message is "Exception has been thrown by the target of
    /// an invocation" or "One or more errors occurred" — true, and useless to the person reading it.
    /// </summary>
    public static string RootMessage(Exception ex)
    {
        var current = ex;
        while (current.InnerException is { } inner
               && current is System.Reflection.TargetInvocationException or AggregateException
                   or TypeInitializationException)
            current = inner;
        return current.Message;
    }
}
