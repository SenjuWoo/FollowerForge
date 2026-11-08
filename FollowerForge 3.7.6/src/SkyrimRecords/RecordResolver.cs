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
    /// What a head part is allowed to be used on, and the mesh behind it. ValidRaces is the field
    /// the engine itself filters on: a part whose list exists but omits the NPC's race is hidden
    /// without a word, and a part with no list is applied to every race — which is how a hair
    /// modelled for a beast-race skull ends up sitting around a Nord's jaw.
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

        var raceEditorId = match.ValidRaces.IsNull
            ? null
            : mod!.Races.FirstOrDefault(r => r.FormKey == match.ValidRaces.FormKey)?.EditorID;
        return new HeadPartFacts(match.EditorID, match.Type.ToString(),
            match.Model?.File.GivenPath, match.ValidRaces.IsNull ? null : match.ValidRaces.FormKey.ToString(),
            raceEditorId);
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
/// A head part's race restriction and mesh. <paramref name="ValidRaceFormKey"/> is null when the
/// record places no restriction at all, which the engine reads as "every race".
/// </summary>
public sealed record HeadPartFacts(
    string? EditorId,
    string? Type,
    string? ModelPath,
    string? ValidRaceFormKey,
    string? ValidRaceEditorId);
