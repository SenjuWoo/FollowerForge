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
    public HeadPartFacts? ResolveHeadPart(string formKeyString)
    {
        var formKey = FormKey.Factory(formKeyString);
        var mod = Open(formKey.ModKey);
        var match = mod?.HeadParts.FirstOrDefault(p => p.FormKey == formKey);
        if (match is null)
        {
            log.Warning("Head part {Key} not found in {Plugin}", formKeyString, formKey.ModKey.FileName);
            return null;
        }

        string? listFormKey = null;
        string? listEditorId = null;
        IReadOnlyList<string>? races = null;
        if (!match.ValidRaces.IsNull)
        {
            listFormKey = match.ValidRaces.FormKey.ToString();
            var list = mod!.FormLists.FirstOrDefault(f => f.FormKey == match.ValidRaces.FormKey);
            if (list is null)
            {
                log.Warning("Head part {Key} names race list {List}, which {Plugin} does not define",
                    formKeyString, listFormKey, formKey.ModKey.FileName);
            }
            else
            {
                listEditorId = list.EditorID;
                // Entries carry no type guarantee: a malformed list can point anywhere. Membership
                // is a FormKey comparison, so every entry is usable and nothing is assumed.
                races = list.Items.Select(item => item.FormKey.ToString()).ToList();
            }
        }

        return new HeadPartFacts(match.EditorID, match.Type.ToString(),
            match.Model?.File.GivenPath, listFormKey, listEditorId, races);
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
    IReadOnlyList<string>? ValidRaceFormKeys);
