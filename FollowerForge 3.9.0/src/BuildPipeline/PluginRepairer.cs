using FollowerForge.Domain;
using FollowerForge.SkyrimRecords;
using FollowerForge.Validation;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace FollowerForge.BuildPipeline;

/// <summary>
/// Fixes a follower plugin that an older FollowerForge built, without its profile.
///
/// Rebuilding from <c>rebuild-profile.json</c> is the better route and the window offers it first.
/// This is for the plugins that have no profile any more: followers shared on Nexus by other
/// people, or ones whose build folder is gone. It corrects exactly what builds before 3.8.0 wrote
/// wrong and leaves every other byte of meaning alone:
/// <list type="bullet">
/// <item>speed multiplier 0 and height 0, on NPCs the plugin itself defines;</item>
/// <item>disposition, sound level and NAM5, only when the NPC carries the whole set of values a
/// fresh, never-configured record has (an author who chose "loud" keeps it);</item>
/// <item>a level multiplier above 10, which no follower is given on purpose: the old double
/// conversion stored 1.0 as 16.96. It is undone only when exactly one sensible value explains it;</item>
/// <item>a worldspace override with no EditorID, replaced by the worldspace's own record with the
/// plugin's cells moved across, when the plugin that defines it can be read.</item>
/// </list>
/// The original file is never written. The repaired copy goes to a separate folder.
/// </summary>
public sealed class PluginRepairer(ILogger log)
{
    public sealed record RepairResult(
        string? OutputPath,
        IReadOnlyList<string> Changes,
        IReadOnlyList<string> Unresolved,
        ValidationReport Gate)
    {
        public bool Changed => Changes.Count > 0;
    }

    /// <param name="worldspaceSource">The defining record of a worldspace, or null when unavailable.</param>
    public RepairResult Repair(string pluginPath, string outputDirectory,
        Func<FormKey, IWorldspaceGetter?>? worldspaceSource = null)
    {
        var input = Path.GetFullPath(pluginPath);
        var outputPath = Path.Combine(Path.GetFullPath(outputDirectory), Path.GetFileName(input));
        if (string.Equals(input, outputPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The repaired copy must go to a different folder than the original.");

        var mod = SkyrimMod.CreateFromBinary(input, VanillaForms.Release);
        var recordsBefore = mod.EnumerateMajorRecords().Count();
        var changes = new List<string>();
        var unresolved = new List<string>();

        foreach (var npc in mod.Npcs.Where(n => n.FormKey.ModKey == mod.ModKey))
        {
            var name = npc.EditorID ?? npc.FormKey.ToString();
            var fresh = npc.Configuration.SpeedMultiplier == 0 && npc.Height <= 0f
                        && npc.Configuration.DispositionBase == 0 && npc.NAM5 == 0xFF00;

            if (npc.Configuration.SpeedMultiplier == 0)
            {
                npc.Configuration.SpeedMultiplier = 100;
                changes.Add($"{name}: speed multiplier 0 -> 100");
            }
            if (npc.Height <= 0f)
            {
                npc.Height = 1f;
                changes.Add($"{name}: height 0 -> 1.0");
            }
            if (fresh)
            {
                npc.Configuration.DispositionBase = 35;
                npc.NAM5 = 255;
                if (npc.SoundLevel == SoundLevel.Loud) npc.SoundLevel = SoundLevel.Normal;
                changes.Add($"{name}: disposition, sound level and NAM5 set to the values vanilla NPCs carry");
            }

            if (npc.Configuration.Level is PcLevelMult scaled && scaled.LevelMult > 10f)
            {
                if (UndoDoubleScaledMultiplier(scaled.LevelMult) is { } meant)
                {
                    changes.Add($"{name}: level multiplier {scaled.LevelMult:0.###} -> {meant:0.###}");
                    scaled.LevelMult = meant;
                }
                else
                {
                    unresolved.Add($"{name}: level multiplier {scaled.LevelMult:0.###} looks wrong but no single "
                                   + "value explains it; set it in the Creation Kit or rebuild her");
                }
            }
        }

        foreach (var broken in mod.Worldspaces
                     .Where(w => w.FormKey.ModKey != mod.ModKey && string.IsNullOrEmpty(w.EditorID))
                     .ToList())
        {
            if (worldspaceSource?.Invoke(broken.FormKey) is not { } source)
            {
                unresolved.Add($"worldspace {broken.FormKey} is overridden by an empty record and its "
                               + $"plugin ({broken.FormKey.ModKey.FileName}) could not be read here");
                continue;
            }
            var whole = CellPlacer.CopyOfWorldspace(broken.FormKey, source);
            whole.TopCell = broken.TopCell;
            whole.SubCells.AddRange(broken.SubCells);
            mod.Worldspaces.Remove(broken.FormKey);
            mod.Worldspaces.Add(whole);
            changes.Add($"worldspace {source.EditorID ?? broken.FormKey.ToString()}: empty override replaced "
                        + "by the worldspace's own record");
        }

        var gate = new ValidationReport();
        if (changes.Count == 0)
        {
            log.Information("Repair: nothing to change in {Plugin}", input);
            return new RepairResult(null, changes, unresolved, gate);
        }

        if (mod.EnumerateMajorRecords().Count() != recordsBefore)
            throw new InvalidOperationException("Repair changed the number of records; nothing was written.");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        // The master list is the plugin's own. Rebuilding it from content could drop a master a
        // script property relies on, which the content scan cannot see.
        mod.WriteToBinary(outputPath, new BinaryWriteParameters
        {
            MastersListContent = MastersListContentOption.NoCheck,
        });
        HeaderFixer.FixRecordCount(outputPath);
        EspHeaderValidator.Validate(outputPath, gate, requireEsl: mod.ModHeader.Flags.HasFlag(SkyrimModHeader.HeaderFlag.Small));

        log.Information("Repair: {Count} change(s) to {Plugin} -> {Output}", changes.Count, input, outputPath);
        return new RepairResult(outputPath, changes, unresolved, gate);
    }

    /// <summary>
    /// The multiplier an old build meant. It stored round(m × 1000) × 1000 in a sixteen-bit field,
    /// so the stored value is (m × 1,000,000) mod 65,536 thousandths. Candidates are every m in
    /// (0, 10] that produces it; a follower's multiplier is a value someone typed, so only a
    /// candidate with at most two decimals counts, and only a unique one is used.
    /// </summary>
    internal static float? UndoDoubleScaledMultiplier(float stored)
    {
        var thousandths = (long)Math.Round(stored * 1000.0);
        var candidates = new List<double>();
        for (var wraps = 0; wraps <= 200; wraps++)
        {
            var meant = (thousandths + 65536.0 * wraps) / 1_000_000.0;
            if (meant > 10.0) break;
            if (meant > 0 && Math.Abs(meant * 100 - Math.Round(meant * 100)) < 1e-6)
                candidates.Add(Math.Round(meant, 2));
        }
        return candidates.Count == 1 ? (float)candidates[0] : null;
    }
}
