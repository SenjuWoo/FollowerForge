using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace FollowerForge.SkyrimRecords;

/// <summary>
/// Opens individual source plugins read-only to fetch a specific record getter (e.g. a combat
/// style to clone). Disposable: keeps the overlays alive until the caller is done reading.
/// </summary>
public sealed class RecordResolver(string gameDataPath, ILogger log) : IDisposable
{
    private readonly Dictionary<ModKey, ISkyrimModDisposableGetter> _open = new();

    /// <summary>Resolves a combat style by its FormKey string ("123ABC:Plugin.esp").</summary>
    public ICombatStyleGetter? ResolveCombatStyle(string formKeyString)
    {
        var formKey = FormKey.Factory(formKeyString);
        var mod = Open(formKey.ModKey);
        if (mod is null) return null;
        var match = mod.CombatStyles.FirstOrDefault(c => c.FormKey == formKey);
        if (match is null)
            log.Warning("Combat style {Key} not found in {Plugin}", formKeyString, formKey.ModKey.FileName);
        return match;
    }

    /// <summary>
    /// Resolves a voice type's EditorID. It names the folder the game looks in for voice files
    /// (sound\voice\&lt;plugin&gt;\&lt;voice type&gt;\), so guessing it produces silent dialogue.
    /// </summary>
    public string? ResolveVoiceTypeEditorId(string formKeyString)
    {
        var formKey = FormKey.Factory(formKeyString);
        var mod = Open(formKey.ModKey);
        var match = mod?.VoiceTypes.FirstOrDefault(v => v.FormKey == formKey);
        if (match?.EditorID is null)
            log.Warning("Voice type {Key} has no resolvable EditorID", formKeyString);
        return match?.EditorID;
    }

    /// <summary>
    /// What a head part is allowed to be used on, and the mesh behind it.
    ///
    /// `ValidRaces` is a FormID, but not of a race: Skyrim stores an FLST there, and the FLST holds
    /// the permitted races. Reading it as a race is what made 3.6 blame the wrong record, and what
    /// made 3.7.5-3.7.6 compare a FormList against a race and always find a mismatch.
    ///
    /// <paramref name="ValidRaceFormKeys"/> is null in two situations that must not be conflated:
    /// the part has no restriction at all (<paramref name="ValidRacesFormList"/> null), or it names
    /// a list this method could not read (<paramref name="ValidRacesFormList"/> set, membership
    /// unknown). Callers report UNKNOWN rather than guessing.
    /// </summary>
    public HeadPartFacts? ResolveHeadPart(
        string formKeyString, Func<string, string?>? winningPluginOf = null,
        Func<string, bool>? catalogueKnows = null)
    {
        var formKey = FormKey.Factory(formKeyString);
        // A catalogue that exists but holds no entry for this record is not evidence that nothing
        // overrides it — it is absent evidence. Track that separately from "no catalogue at all".
        var overrideUnchecked = catalogueKnows is not null && !catalogueKnows(formKeyString);

        // The head part record itself can be overridden, and an override may change its race list
        // or its mesh. Read the winning copy when the catalogue can name one, keeping the original
        // FormKey — an override carries the record it replaces.
        var partModKey = formKey.ModKey;
        string? partOverriddenBy = null;
        if (winningPluginOf?.Invoke(formKeyString) is { Length: > 0 } partWinner
            && !string.Equals(partWinner, partModKey.FileName.String, StringComparison.OrdinalIgnoreCase))
        {
            partOverriddenBy = partWinner;
            partModKey = ModKey.FromFileName(partWinner);
        }

        var mod = Open(partModKey);
        var match = mod?.HeadParts.FirstOrDefault(p => p.FormKey == formKey);
        if (match is null)
        {
            log.Warning("Head part {Key} not found in {Plugin}", formKeyString, partModKey.FileName);
            // Falling back to the defining record here would report a race verdict from a record the
            // game does not use, so an override that cannot be opened is UNKNOWN, not the base copy.
            if (partOverriddenBy is not null)
                return new HeadPartFacts(null, null, null, null, null, null, null, partOverriddenBy,
                    RecordUnreadable: true);
            // Nothing was found: say which plugin was searched, and whether that plugin even exists.
            // A missing record is a preset fault the user can fix; silence let it pass unreported.
            return new HeadPartFacts(null, null, null, null, null, null, null, null,
                Missing: true, MissingIn: partModKey.FileName.String, MissingPlugin: mod is null);
        }

        string? listFormKey = null;
        string? listEditorId = null;
        IReadOnlyList<string>? races = null;
        string? overriddenBy = null;
        if (!match.ValidRaces.IsNull)
        {
            listFormKey = match.ValidRaces.FormKey.ToString();
            // A FormKey names its own plugin, and a race list very often lives in a shared lists
            // plugin or a master rather than beside the head part. Looking it up in the head part's
            // file alone reported UNKNOWN for ordinary mods; open the plugin the FormKey names.
            var listModKey = match.ValidRaces.FormKey.ModKey;
            var listMod = listModKey == partModKey ? mod : Open(listModKey);
            var list = listMod?.FormLists.FirstOrDefault(f => f.FormKey == match.ValidRaces.FormKey);

            // A later plugin in the load order may override that list, in which case the base
            // record is not what the game reads. The catalogue already records which plugin wins,
            // so the winner can be opened directly instead of scanning the order.
            if (winningPluginOf?.Invoke(listFormKey) is { Length: > 0 } winner
                && !string.Equals(winner, listModKey.FileName.String, StringComparison.OrdinalIgnoreCase))
            { 
                overriddenBy = winner;
                var winnerKey = ModKey.FromFileName(winner);
                var winnerMod = winnerKey == listModKey ? listMod : Open(winnerKey);
                list = winnerMod?.FormLists.FirstOrDefault(f => f.FormKey == match.ValidRaces.FormKey);
                if (list is null)
                    log.Warning("Race list {List} is overridden by {Winner}, which could not be read",
                        listFormKey, winner);
            }

            if (list is null)
            {
                log.Warning("Head part {Key} names race list {List}, which {Plugin} does not define",
                    formKeyString, listFormKey, overriddenBy ?? listModKey.FileName.String);
            }
            else
            {
                listEditorId = list.EditorID;
                // Entries carry no type guarantee: a malformed list can point anywhere. Membership
                // is a FormKey comparison, so every entry is usable and nothing is assumed.
                races = list.Items.Select(item => item.FormKey.ToString()).ToList();
            }

            // The list is only override-checked if the catalogue actually holds it. A missing entry
            // is absent evidence, not proof that the defining record wins.
            if (catalogueKnows is not null && !catalogueKnows(listFormKey)) overrideUnchecked = true;
        }

        return new HeadPartFacts(match.EditorID, match.Type.ToString(),
            match.Model?.File.GivenPath, listFormKey, listEditorId, races, overriddenBy, partOverriddenBy,
            OverrideUnchecked: overrideUnchecked);
    }

    private ISkyrimModDisposableGetter? Open(ModKey modKey)
    {
        if (_open.TryGetValue(modKey, out var existing)) return existing;
        var path = Path.Combine(gameDataPath, modKey.FileName);
        if (!File.Exists(path))
        {
            log.Warning("Source plugin not found for resolve: {Path}", path);
            return null;
        }
        var mod = SkyrimMod.CreateFromBinaryOverlay(path, VanillaForms.Release);
        _open[modKey] = mod;
        return mod;
    }

    public void Dispose()
    {
        foreach (var m in _open.Values) m.Dispose();
        _open.Clear();
    }
}

/// <summary>
/// A head part's race restriction and mesh. <paramref name="ValidRacesFormList"/> is null when the
/// record places no restriction at all, which the engine reads as "every race";
/// <paramref name="ValidRaceFormKeys"/> is null when the list exists but could not be read, which
/// callers must report as unknown rather than as compatible or incompatible.
/// </summary>
public sealed record HeadPartFacts(
    string? EditorId,
    string? Type,
    string? ModelPath,
    string? ValidRacesFormList,
    string? ValidRacesFormListEditorId,
    IReadOnlyList<string>? ValidRaceFormKeys,
    /// <summary>
    /// Plugin that wins the race list in the load order, when it is not the plugin that defines it.
    /// Null means either "nothing overrides it" or "no catalogue was available to ask" — a caller
    /// must not read null as proof that the list is uncontested.
    /// </summary>
    string? ValidRacesOverriddenBy = null,
    /// <summary>
    /// Plugin that wins the head part record itself, when it is not the plugin that defines it.
    /// Same caveat as <paramref name="ValidRacesOverriddenBy"/>: null does not prove it is uncontested.
    /// </summary>
    string? PartOverriddenBy = null,
    /// <summary>
    /// True when the winning head part override could not be opened, so nothing about this part —
    /// race list, type, mesh — is known. Callers must report that as unknown rather than falling
    /// back to the defining record.
    /// </summary>
    bool RecordUnreadable = false,
    /// <summary>
    /// True when a catalogue existed but held no entry for this head part or its race list, so
    /// overrides were never checked. Missing evidence is not evidence of absence.
    /// </summary>
    bool OverrideUnchecked = false,
    /// <summary>
    /// True when the record could not be found at all: either its plugin is missing or the plugin
    /// does not contain it. Distinct from a record that exists but could not be read.
    /// </summary>
    bool Missing = false,
    /// <summary>The plugin that was searched, for the message.</summary>
    string? MissingIn = null,
    /// <summary>True when that plugin file itself is absent, not merely short of the record.</summary>
    bool MissingPlugin = false);
