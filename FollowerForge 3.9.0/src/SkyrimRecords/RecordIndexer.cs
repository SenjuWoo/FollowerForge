using FollowerForge.Domain;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace FollowerForge.SkyrimRecords;

/// <summary>
/// Enumerates every winning record of the indexed types from an opened load order.
/// Winner = first occurrence of a FormKey when walking mods in priority order
/// (last loaded first), which is exactly the game's conflict rule.
/// </summary>
public sealed class RecordIndexer(ILogger log)
{
    /// <summary>Getter interface per indexed type; enumeration is deep (cells included).</summary>
    private static readonly (IndexedRecordType Type, Func<ISkyrimModGetter, IEnumerable<IMajorRecordGetter>> Enumerate)[]
        Extractors =
        [
            (IndexedRecordType.Npc, m => m.EnumerateMajorRecords<INpcGetter>()),
            (IndexedRecordType.Race, m => m.EnumerateMajorRecords<IRaceGetter>()),
            (IndexedRecordType.CombatStyle, m => m.EnumerateMajorRecords<ICombatStyleGetter>()),
            (IndexedRecordType.Class, m => m.EnumerateMajorRecords<IClassGetter>()),
            (IndexedRecordType.VoiceType, m => m.EnumerateMajorRecords<IVoiceTypeGetter>()),
            (IndexedRecordType.HeadPart, m => m.EnumerateMajorRecords<IHeadPartGetter>()),
            (IndexedRecordType.Outfit, m => m.EnumerateMajorRecords<IOutfitGetter>()),
            (IndexedRecordType.Armor, m => m.EnumerateMajorRecords<IArmorGetter>()),
            (IndexedRecordType.ArmorAddon, m => m.EnumerateMajorRecords<IArmorAddonGetter>()),
            (IndexedRecordType.Weapon, m => m.EnumerateMajorRecords<IWeaponGetter>()),
            (IndexedRecordType.Spell, m => m.EnumerateMajorRecords<ISpellGetter>()),
            (IndexedRecordType.Perk, m => m.EnumerateMajorRecords<IPerkGetter>()),
            (IndexedRecordType.Package, m => m.EnumerateMajorRecords<IPackageGetter>()),
            (IndexedRecordType.Faction, m => m.EnumerateMajorRecords<IFactionGetter>()),
            (IndexedRecordType.Relationship, m => m.EnumerateMajorRecords<IRelationshipGetter>()),
            (IndexedRecordType.Cell, m => m.EnumerateMajorRecords<ICellGetter>()),
            (IndexedRecordType.Location, m => m.EnumerateMajorRecords<ILocationGetter>()),
            (IndexedRecordType.Keyword, m => m.EnumerateMajorRecords<IKeywordGetter>()),
            (IndexedRecordType.TextureSet, m => m.EnumerateMajorRecords<ITextureSetGetter>()),
            (IndexedRecordType.FormList, m => m.EnumerateMajorRecords<IFormListGetter>()),
            (IndexedRecordType.Book, m => m.EnumerateMajorRecords<IBookGetter>()),
            (IndexedRecordType.MiscItem, m => m.EnumerateMajorRecords<IMiscItemGetter>()),
            (IndexedRecordType.Ingestible, m => m.EnumerateMajorRecords<IIngestibleGetter>()),
            (IndexedRecordType.Ingredient, m => m.EnumerateMajorRecords<IIngredientGetter>()),
            (IndexedRecordType.Ammo, m => m.EnumerateMajorRecords<IAmmunitionGetter>()),
        ];

    /// <param name="loadOrder">Opened load order (see <see cref="LoadOrderBuilder"/>).</param>
    /// <param name="pluginSourceMods">Plugin file name → Vortex staging mod folder.</param>
    public IEnumerable<IndexedRecord> EnumerateWinningRecords(
        LoadOrderBuilder.BuiltLoadOrder loadOrder,
        IReadOnlyDictionary<string, string>? pluginSourceMods = null)
    {
        // Materialize priority-order mods once (mmap overlays stay lazy).
        var mods = new List<ISkyrimModGetter>();
        foreach (var listing in loadOrder.LoadOrder.PriorityOrder)
        {
            if (listing.Mod is not null) mods.Add(listing.Mod);
        }
        log.Information("Indexing winning records across {Count} plugins…", mods.Count);

        var masterCache = new Dictionary<ModKey, string[]>();
        var reported = new HashSet<ModKey>();

        foreach (var (type, enumerate) in Extractors)
        {
            var seen = new HashSet<FormKey>();
            long emitted = 0;
            foreach (var mod in mods)
            {
                // Mutagen reads records lazily, so a damaged record throws while it is being walked,
                // not when the walk is set up. Each plugin's records are therefore collected whole
                // before any is handed on: a plugin that fails part-way contributes nothing for this
                // type, the next plugin down still supplies its own copy of those records, and the
                // index carries on. One bad .esp used to abort the entire catalogue from here.
                List<IndexedRecord> batch;
                try
                {
                    batch = Collect(type, enumerate, mod, seen, masterCache, pluginSourceMods);
                }
                catch (Exception ex)
                {
                    var reason = UnreadablePlugin.RootMessage(ex);
                    log.Warning("Skipping {Type} records in {Mod}: {Error}", type, mod.ModKey, reason);
                    if (reported.Add(mod.ModKey))
                        _unreadable.Add(new UnreadablePlugin(mod.ModKey.FileName.String, $"{type}: {reason}"));
                    continue;
                }

                foreach (var record in batch)
                {
                    seen.Add(Mutagen.Bethesda.Plugins.FormKey.Factory(record.FormKey));
                    yield return record;
                }
                emitted += batch.Count;
            }
            log.Information("  {Type}: {Count} winning records", type, emitted);
        }
    }

    private readonly List<UnreadablePlugin> _unreadable = [];

    /// <summary>
    /// Plugins whose records could not be read while indexing. Complete once the sequence returned
    /// by <see cref="EnumerateWinningRecords"/> has been consumed.
    /// </summary>
    public IReadOnlyList<UnreadablePlugin> Unreadable => _unreadable;

    private static List<IndexedRecord> Collect(
        IndexedRecordType type,
        Func<ISkyrimModGetter, IEnumerable<IMajorRecordGetter>> enumerate,
        ISkyrimModGetter mod,
        HashSet<FormKey> alreadyWon,
        Dictionary<ModKey, string[]> masterCache,
        IReadOnlyDictionary<string, string>? pluginSourceMods)
    {
        var batch = new List<IndexedRecord>();
        var own = new HashSet<FormKey>();
        foreach (var rec in enumerate(mod))
        {
            if (alreadyWon.Contains(rec.FormKey) || !own.Add(rec.FormKey)) continue;

            if (!masterCache.TryGetValue(mod.ModKey, out var masters))
            {
                masters = mod.ModHeader.MasterReferences
                    .Select(r => r.Master.FileName.String)
                    .ToArray();
                masterCache[mod.ModKey] = masters;
            }

            var winningPlugin = mod.ModKey.FileName.String;
            batch.Add(new IndexedRecord
            {
                FormKey = rec.FormKey.ToString(),
                Type = type,
                EditorId = rec.EditorID,
                DisplayName = TryGetName(rec),
                SourcePlugin = rec.FormKey.ModKey.FileName.String,
                WinningPlugin = winningPlugin,
                RequiredMasters = masters,
                MajorFlags = (uint)rec.MajorRecordFlagsRaw,
                SourceMod = pluginSourceMods is not null
                    && pluginSourceMods.TryGetValue(winningPlugin, out var src) ? src : null,
                DetailJson = ComputeDetail(rec),
            });
        }
        return batch;
    }

    private static readonly System.Text.Json.JsonSerializerOptions DetailJson = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    /// <summary>Type-specific analysis stored alongside the record for later inspection.</summary>
    private static string? ComputeDetail(IMajorRecordGetter rec)
    {
        try
        {
            object? detail = rec switch
            {
                ICombatStyleGetter cs => CombatStyleAnalyzer.Analyze(cs),
                IRaceGetter race => RaceAnalyzer.Analyze(race),
                IArmorGetter armor => ArmorAnalyzer.Analyze(armor),
                // Voice files are verified on demand (the asset index is built after records).
                IVoiceTypeGetter vt => VoiceClassifier.Classify(vt),
                _ => null,
            };
            return detail is null ? null : System.Text.Json.JsonSerializer.Serialize(detail, DetailJson);
        }
        catch
        {
            // Analysis is best-effort; a malformed record must never abort the index.
            return null;
        }
    }

    private static string? TryGetName(IMajorRecordGetter rec)
    {
        try
        {
            return rec switch
            {
                ITranslatedNamedGetter tn => tn.Name?.String,
                INamedGetter n => n.Name,
                _ => null,
            };
        }
        catch
        {
            // Localized plugins without loaded string tables can throw on resolution;
            // a missing display name must never abort the index.
            return null;
        }
    }
}
