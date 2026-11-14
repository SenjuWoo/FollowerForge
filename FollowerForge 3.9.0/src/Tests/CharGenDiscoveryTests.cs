using FollowerForge.AssetIndex;
using FollowerForge.Domain;
using FollowerForge.ModManagers;
using Serilog;

namespace FollowerForge.Tests;

public sealed class CharGenDiscoveryTests
{
    [Fact]
    public void Jslot_ParsesCompleteRecordAppearanceAndIdentifierOnlyDependency()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ff_jslot_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "Silvia.jslot");
        try
        {
            File.WriteAllText(path, """
                {
                  "actor": {
                    "hairColor": 5395287, "weight": 59.0,
                    "headTexture": "Skyrim.esm|051648"
                  },
                  "mods": [
                    { "index": 0, "name": "Skyrim.esm" },
                    { "index": 18, "name": "High Poly Head.esm" }
                  ],
                  "headParts": [
                    { "type": 0, "formId": 333071, "formIdentifier": "Skyrim.esm|05150F" },
                    { "type": 6, "formId": 4268042430, "formIdentifier": "Even More Eyes.esp|6528BE" }
                  ],
                  "morphs": {
                    "default": {
                      "morphs": [0.0, 0.2, -1.0],
                      "presets": [13, 4294967295, 16, 3]
                    }
                  },
                  "tintInfo": [
                    { "index": 0, "color": 4294966267,
                      "texture": "Actors\\Character\\Character Assets\\TintMasks\\SkinTone.dds" }
                  ]
                }
                """);

            var discovery = new CharGenDiscovery(new LoggerConfiguration().CreateLogger());
            var appearance = discovery.ReadJslotAppearance(path);
            var plugins = discovery.ReadJslotPlugins(path);

            Assert.NotNull(appearance);
            Assert.Equal(59f, appearance!.Weight);
            Assert.Equal(2, appearance.HeadParts.Count);
            Assert.Contains(appearance.HeadParts, p => p.FormKey == "0008BE:Even More Eyes.esp");
            Assert.Equal(0.2f, appearance.FaceMorphs[1]);
            Assert.Equal(uint.MaxValue, appearance.FacePresets[1]);
            Assert.Equal((uint)5395287, appearance.HairColorArgb);
            Assert.Equal("FFFBFBFF", appearance.SkinToneRgba);
            // Dropping this is what leaves the follower on her race's default complexion while
            // the exported head keeps the preset's — a skin-tone seam at the neck.
            Assert.Equal("051648:Skyrim.esm", appearance.HeadTextureSet?.FormKey);
            Assert.Contains("Even More Eyes.esp", plugins);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void Jslot_CountsHeadPartsItCannotResolveInsteadOfDroppingThemSilently()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ff_jslot_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "BaldRisk.jslot");
        try
        {
            // Hair entry: raw formId in the 02-range but no "mods" entry with index 2 and no
            // formIdentifier — the exact shape a preset carries when RaceMenu's mods table omits
            // the hair plugin. Before 3.7.2 this entry vanished without a trace.
            File.WriteAllText(path, """
                {
                  "mods": [
                    { "index": 0, "name": "Skyrim.esm" }
                  ],
                  "headParts": [
                    { "type": 0, "formId": 333071, "formIdentifier": "Skyrim.esm|05150F" },
                    { "type": 1, "formId": 34078720 }
                  ],
                  "tintInfo": []
                }
                """);

            var discovery = new CharGenDiscovery(new LoggerConfiguration().CreateLogger());
            var appearance = discovery.ReadJslotAppearance(path);

            Assert.NotNull(appearance);
            Assert.Single(appearance!.HeadParts);
            Assert.Equal(1, appearance.UnresolvedHeadParts);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void NifOnlyInMo2Overwrite_IsDiscoveredOnce()
    {
        var root = Path.Combine(Path.GetTempPath(), "ff_ow_" + Guid.NewGuid().ToString("N"));
        var game = Path.Combine(root, "game");
        var data = Path.Combine(game, "Data");
        var overwrite = Path.Combine(root, "overwrite");
        var exported = Path.Combine(overwrite, "SKSE", "Plugins", "CharGen", "Exported");
        var gameCharGen = Path.Combine(data, "SKSE", "Plugins", "CharGen");
        Directory.CreateDirectory(gameCharGen);
        Directory.CreateDirectory(exported);
        File.WriteAllText(Path.Combine(exported, "OnlyOverwrite.nif"), "");
        File.WriteAllText(Path.Combine(gameCharGen, "SameName.nif"), "");
        File.WriteAllText(Path.Combine(overwrite, "SKSE", "Plugins", "CharGen", "SameName.nif"), "");

        try
        {
            var env = new EnvironmentSnapshot
            {
                Manager = ModManagerKind.Mo2,
                ManagerLabel = "Mod Organizer 2",
                GameRootPath = game,
                GameDataPath = data,
                PluginDataPath = Path.Combine(root, "view"),
                InstancePath = root,
                StagingPath = Path.Combine(root, "mods"),
                ProfilesPath = Path.Combine(root, "profiles"),
                RuntimePluginsTxtPath = Path.Combine(root, "plugins.txt"),
                Mo2ModPriority = [Mo2PathResolver.OverwriteToken],
                Mo2OverwritePath = overwrite,
            };

            var exports = new CharGenDiscovery(new LoggerConfiguration().CreateLogger()).Discover(env);
            var names = exports.Select(e => e.Name).ToList();
            Assert.Equal(2, names.Count);
            Assert.Contains("OnlyOverwrite", names);
            Assert.Single(names, n => n == "SameName");
            var same = Assert.Single(exports, e => e.Name == "SameName");
            Assert.Contains("overwrite", same.NifPath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void EmptyCatalogue_NamesScannedFoldersAndPresetCount()
    {
        var root = Path.Combine(Path.GetTempPath(), "ff_empty_" + Guid.NewGuid().ToString("N"));
        var exported = Path.Combine(root, "SKSE", "Plugins", "CharGen", "Exported");
        var presets = Path.Combine(root, "SKSE", "Plugins", "CharGen", "Presets");
        Directory.CreateDirectory(exported);
        Directory.CreateDirectory(presets);
        File.WriteAllText(Path.Combine(exported, "One.jslot"), "{}");
        File.WriteAllText(Path.Combine(presets, "Two.jslot"), "{}");

        try
        {
            var note = new CharGenDiscovery(new LoggerConfiguration().CreateLogger())
                .EmptyCatalogueMessage(root);
            Assert.Contains(@"SKSE\Plugins\CharGen", note);
            Assert.Contains("Presets", note);
            Assert.Contains("Exported", note);
            Assert.Contains("2 preset", note);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch (IOException) { }
        }
    }
}
