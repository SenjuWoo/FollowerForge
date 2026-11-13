using FollowerForge.BuildPipeline;
using FollowerForge.Domain;

namespace FollowerForge.Ui;

/// <summary>
/// Turns a follower that was already built back into something the window can show.
///
/// Every build writes <c>rebuild-profile.json</c> beside the plugin — the complete input it was
/// built from. Until 3.8.0 nothing could read it back, so changing one thing about a finished
/// follower meant choosing all of her again. This is the part of that which needs no window: where
/// the file is, how her belongings divide between the pickers, and which settings the window has
/// no control for and would therefore drop on the next build. Kept pure so it can be tested.
/// </summary>
public static class FollowerReopen
{
    public const string ProfileFileName = "rebuild-profile.json";

    /// <summary>
    /// Accepts the profile file itself or the follower's folder, because the folder is what the
    /// build shows and what "Open output folder" opens.
    /// </summary>
    public static string? FindProfileFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var trimmed = path.Trim().Trim('"');
        if (File.Exists(trimmed)) return trimmed;
        if (!Directory.Exists(trimmed)) return null;
        var inside = Path.Combine(trimmed, ProfileFileName);
        return File.Exists(inside) ? inside : null;
    }

    public static FollowerProfile Load(string path)
    {
        var file = FindProfileFile(path)
            ?? throw new FileNotFoundException(
                $"No {ProfileFileName} was found at '{path}'. Choose the file itself, or the folder of a "
                + "follower FollowerForge built.", path);
        return ProfileIo.Load(file);
    }

    /// <summary>How a built follower's belongings divide between the window's pickers.</summary>
    public sealed record Loadout(
        IReadOnlyList<string> Armor,
        IReadOnlyList<string> Weapons,
        IReadOnlyList<string> Belongings,
        int BelongingsCount,
        IReadOnlyList<string> Ammo,
        int AmmoCount,
        IReadOnlyList<string> Spells,
        IReadOnlyList<string> Perks,
        IReadOnlyList<string> NoLongerInstalled);

    /// <summary>
    /// A saved profile holds one inventory list; the window keeps armor, weapons and belongings in
    /// separate pickers. Anything the catalogue no longer knows is reported rather than carried:
    /// it could not be shown, so it could not be removed, and it would fail the next build.
    /// </summary>
    /// <param name="kindOf">What the catalogue says a record is, or null when it is not installed.</param>
    public static Loadout SplitLoadout(FollowerProfile profile, Func<string, IndexedRecordType?> kindOf)
    {
        var armor = new List<string>();
        var weapons = new List<string>();
        var belongings = new List<string>();
        var ammo = new List<string>();
        var spells = new List<string>();
        var perks = new List<string>();
        var missing = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var belongingsCount = 1;

        foreach (var item in profile.EquippedArmor.Concat(profile.InventoryItems))
        {
            if (!seen.Add(item.FormKey)) continue;
            switch (kindOf(item.FormKey))
            {
                case null: missing.Add(item.FormKey); break;
                case IndexedRecordType.Armor: armor.Add(item.FormKey); break;
                case IndexedRecordType.Weapon: weapons.Add(item.FormKey); break;
                default:
                    belongings.Add(item.FormKey);
                    belongingsCount = Math.Max(belongingsCount, item.Count);
                    break;
            }
        }

        Sort(profile.Ammo, ammo);
        Sort(profile.Spells, spells);
        Sort(profile.Perks, perks);
        var ammoCount = profile.Ammo.Count == 0 ? 100 : Math.Max(1, profile.Ammo.Max(a => a.Count));

        return new Loadout(armor, weapons, belongings, belongingsCount, ammo, ammoCount, spells, perks, missing);

        void Sort(IReadOnlyList<RecordRef> source, List<string> target)
        {
            foreach (var item in source)
            {
                if (!seen.Add(item.FormKey)) continue;
                if (kindOf(item.FormKey) is null) missing.Add(item.FormKey);
                else target.Add(item.FormKey);
            }
        }
    }

    /// <summary>0 protected, 1 essential, 2 mortal — the order the window lists them in.</summary>
    public static int MortalityIndex(FollowerProfile profile) =>
        profile.Essential ? 1 : profile.Protected ? 0 : 2;

    /// <summary>
    /// Settings a profile can hold that the window has no control for. They are legitimate — the
    /// command line and hand-edited profiles use them — but a build from the window writes what
    /// the window shows, so each of these would silently go back to its default. Said up front.
    /// </summary>
    public static IReadOnlyList<string> SettingsTheWindowCannotShow(FollowerProfile profile)
    {
        var notes = new List<string>();
        var placement = profile.Placement;

        if (placement.LocationId is null && placement.Cell is { } cell
            && (placement.Worldspace is not null
                || placement is not { X: 0f, Y: 0f, Z: 0f }
                || !cell.FormKey.Equals("01A270:Skyrim.esm", StringComparison.OrdinalIgnoreCase)))
            notes.Add($"a hand-placed position (cell {cell.FormKey}) — choose a place, or she moves to Whiterun");

        var derived = "FF_" + new string(profile.Name.Where(char.IsLetterOrDigit).ToArray()) + ".esp";
        if (!profile.PluginName.Equals(derived, StringComparison.OrdinalIgnoreCase))
            notes.Add($"the plugin name {profile.PluginName} — the window names it {derived}, which is a new plugin");
        if (!string.IsNullOrWhiteSpace(profile.EditorIdPrefix))
            notes.Add($"the EditorID prefix {profile.EditorIdPrefix}");
        if (profile.Level.ScaleWithPlayer && Math.Abs(profile.Level.PlayerLevelMult - 1.0) > 0.0005)
            notes.Add($"a level multiplier of {profile.Level.PlayerLevelMult:0.###}");
        if (profile.Behavior.ExtraPackages.Count > 0)
            notes.Add($"{profile.Behavior.ExtraPackages.Count} extra AI package(s)");
        if (profile.Strategy != OutputStrategy.PackLocalReference || profile.HubPluginName is not null)
            notes.Add("the shared-hub output strategy");
        if (profile.Appearance.CharGenNifPath is not null || profile.Appearance.JslotPath is not null)
            notes.Add("an explicit head mesh or preset path");
        if (profile.Ai is not { Assistance: 2, Morality: 0, Energy: 50 }
            || profile.Ai.Aggression != (profile.Ai.Confidence == 4 ? 1 : 0))
            notes.Add("custom AI values other than temperament");
        if (profile.EnemyToAlly.TomeName is { Length: > 0 })
            notes.Add("a custom spell tome name");
        if (profile.RedistributionPermission is not null)
            notes.Add("the redistribution declaration — tick it again yourself if it still holds");
        if (profile.BuildTimestampUnix > 0)
            notes.Add("a pinned build timestamp");
        return notes;
    }

    /// <summary>The line shown after a follower is loaded: what came back, and what did not.</summary>
    public static string Describe(
        FollowerProfile profile, IReadOnlyList<string> notInstalled, IReadOnlyList<string> notShown)
    {
        var text = $"Loaded {profile.Name}. Change what you like and build again.";
        if (notInstalled.Count > 0)
            text += $" {notInstalled.Count} choice(s) are not in your load order any more and were left out: "
                    + string.Join(", ", notInstalled.Take(4))
                    + (notInstalled.Count > 4 ? $" and {notInstalled.Count - 4} more" : "") + ".";
        if (notShown.Count > 0)
            text += " This window has no control for, and will not keep: " + string.Join("; ", notShown) + ".";
        return text;
    }
}
