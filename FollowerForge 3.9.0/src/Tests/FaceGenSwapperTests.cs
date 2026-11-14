using FollowerForge.Domain;
using FollowerForge.FaceGen;
using nifly;
using Serilog;

namespace FollowerForge.Tests;

/// <summary>
/// Integration tests against real RaceMenu CharGen exports on this machine. Skipped cleanly
/// when the game / exports are not present (so the suite passes on any box).
/// </summary>
public sealed class FaceGenSwapperTests : IDisposable
{
    private static readonly ILogger Log = new LoggerConfiguration().CreateLogger();
    // Resolved, never hardcoded: Steam is not on C: for plenty of people, and a test that
    // assumes it is silently stops testing anything on their machine.
    private static string? CharGenDir =>
        ModManagers.GameRootResolver.Find() is { } root
            ? Path.Combine(root, "Data", "SKSE", "Plugins", "CharGen")
            : null;

    private readonly string _dataRoot = Path.Combine(Path.GetTempPath(), "ff_fg_" + Guid.NewGuid().ToString("N"));

    /// <summary>Every export with both halves present, so one odd head cannot fail the suite.</summary>
    private static IEnumerable<(string Nif, string Dds)> RealExports()
    {
        if (CharGenDir is not { } dir || !Directory.Exists(dir)) yield break;
        foreach (var nif in Directory.EnumerateFiles(dir, "*.nif")
                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var dds = Path.ChangeExtension(nif, ".dds");
            if (File.Exists(dds)) yield return (nif, dds);
        }
    }

    [Fact]
    public void Swap_RealExport_ProducesReopenableFaceGeomWithTintPath()
    {
        // Live coverage is a separate claim from the synthetic tests beside it. With no export on
        // this machine the run is NOT RUN, and it says so instead of passing quietly; with exports
        // present, none converting is a failure rather than a silent pass.
        var exports = RealExports().ToList();
        if (exports.Count == 0)
        {
            Console.WriteLine("FaceGen live export: NOT RUN (no CharGen NIF+DDS pair found). "
                + "A real RaceMenu head export is still a manual acceptance test.");
            return;
        }
        Console.WriteLine($"FaceGen live export: RUN against {exports.Count} export(s).");

        foreach (var export in exports)
        {
            if (TrySwap(export))
            {
                Console.WriteLine($"FaceGen live export: converted {Path.GetFileName(export.Nif)}.");
                return;
            }
        }

        Assert.Fail($"FaceGen live export: {exports.Count} export(s) present but none converted.");
    }

    /// <summary>
    /// The handoff report has to reach the user. It used to be written one directory above the
    /// staging root, which the publisher never copies, so the warning named a file that no longer
    /// existed once the follower folder was moved into place.
    /// </summary>
    [Fact]
    public void Ck_handoff_report_is_published_inside_the_follower_folder()
    {
        var staging = Path.Combine(Path.GetTempPath(), "ff_fg_stage_" + Guid.NewGuid().ToString("N"));
        var finalRoot = Path.Combine(Path.GetTempPath(), "ff_fg_final_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            var req = new FaceGenSwapper.Request(
                SourceNifPath: Path.Combine(staging, "missing.nif"),
                SourceDdsPath: null,
                PluginName: "FF_Publish.esp",
                NpcFormId: 0x800,
                DataRoot: staging,
                ActorEditorId: "FF_Publish_NPC",
                NpcFormKey: "000800:FF_Publish.esp");

            var result = new FaceGenSwapper(Log).Swap(req);
            Assert.Equal("ck-handoff-report.json", result.CkHandoffPath);
            Assert.True(File.Exists(Path.Combine(staging, "ck-handoff-report.json")),
                "the report must be written inside the package root");

            var final = Path.Combine(finalRoot, "FF Publish");
            BuildPipeline.DirectoryPublisher.Publish(staging, final, Log);
            Assert.True(File.Exists(Path.Combine(final, "ck-handoff-report.json")),
                "the report must survive publishing");
        }
        finally
        {
            foreach (var dir in new[] { staging, finalRoot })
                try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch (IOException) { }
        }
    }

    private bool TrySwap((string Nif, string Dds) export)
    {
        var req = new FaceGenSwapper.Request(
            SourceNifPath: export.Nif,
            SourceDdsPath: export.Dds,
            PluginName: "FF_FaceGenTest.esp",
            NpcFormId: 0x800,
            DataRoot: _dataRoot,
            ActorEditorId: "FF_FaceGenTest_NPC",
            NpcFormKey: "000800:FF_FaceGenTest.esp");

        var result = new FaceGenSwapper(Log).Swap(req, resolver: _ => (true, "test"));
        if (result.NeedsCreationKit) return false;

        var geom = Path.Combine(_dataRoot, result.FaceGeomPath!);
        var tint = Path.Combine(_dataRoot, result.FaceTintPath!);
        Assert.True(File.Exists(geom), "FaceGeom NIF not written");
        Assert.True(File.Exists(tint), "FaceTint DDS not copied");

        // Path is FormID-keyed and plugin-folder-scoped.
        Assert.EndsWith(@"facegeom\FF_FaceGenTest.esp\00000800.nif", result.FaceGeomPath!.Replace('/', '\\'),
            StringComparison.OrdinalIgnoreCase);

        // Reopen independently and confirm the tint path is present in some texture slot.
        using var check = new NifHeadFile();
        check.Load(geom);
        var expectedTint = result.FaceTintPath!.Replace('/', '\\');
        return check.AllSlots().Any(s => s.Path.Equals(expectedTint, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Swap_MissingNif_WritesCkHandoff()
    {
        var req = new FaceGenSwapper.Request(
            SourceNifPath: Path.Combine(_dataRoot, "does-not-exist.nif"),
            SourceDdsPath: null,
            PluginName: "FF_Missing.esp",
            NpcFormId: 0x800,
            DataRoot: Path.Combine(_dataRoot, "Data"),
            ActorEditorId: "FF_Missing_NPC",
            NpcFormKey: "000800:FF_Missing.esp");

        var result = new FaceGenSwapper(Log).Swap(req);
        Assert.True(result.NeedsCreationKit);
        Assert.False(result.Success);
        Assert.NotNull(result.CkHandoffPath);
        // Package-relative: the published folder is where the user finds it.
        Assert.Equal("ck-handoff-report.json", result.CkHandoffPath);
        Assert.True(File.Exists(Path.Combine(Path.Combine(_dataRoot, "Data"), result.CkHandoffPath!)));
        var message = Assert.Single(result.Findings).Message;
        Assert.Contains("Ctrl+F4", message);
        Assert.Contains(result.CkHandoffPath!, message);
        Assert.DoesNotContain("FACEGEN_MANUAL", message);
    }

    /// <summary>
    /// Rick7's MaleHeadBreton: the tint slot was empty, the warning named the fallback,
    /// then FACEGEN_TINT_NOT_SET. nifly refuses to write a slot past the end of a short
    /// texture list, so a 6-slot head never stores slot 6.
    /// </summary>
    [Fact]
    public void ShortTextureSet_PersistsTintOnSlot6()
    {
        var nif = WriteHead(textureCount: 6, specular: null);
        var dds = Path.ChangeExtension(nif, ".dds");
        File.WriteAllBytes(dds, [0x44, 0x44, 0x53, 0x20]);

        var result = Swap(nif, dds);
        Assert.DoesNotContain(result.Findings, f => f.Code == "FACEGEN_TINT_NOT_SET");
        Assert.False(result.NeedsCreationKit);

        using var check = new NifHeadFile();
        check.Load(Path.Combine(_dataRoot, result.FaceGeomPath!));
        var shape = Assert.Single(check.Shapes());
        var tint = result.FaceTintPath!.Replace('/', '\\');
        Assert.Equal(tint, check.GetSlot(shape, 6).Replace('/', '\\'), StringComparer.OrdinalIgnoreCase);
        Assert.True(string.IsNullOrWhiteSpace(check.GetSlot(shape, 7)));
        Assert.Equal(9u, TextureCount(Path.Combine(_dataRoot, result.FaceGeomPath!)));
    }

    /// <summary>A normal 9-slot head keeps the specular map while the empty tint slot is filled.</summary>
    [Fact]
    public void NineSlotHead_WritesTintAndLeavesSpecular()
    {
        const string specular = @"actors\character\malehead_s.dds";
        var nif = WriteHead(textureCount: 9, specular: specular);
        var dds = Path.ChangeExtension(nif, ".dds");
        File.WriteAllBytes(dds, [0x44, 0x44, 0x53, 0x20]);

        var result = Swap(nif, dds);
        Assert.DoesNotContain(result.Findings, f => f.Code == "FACEGEN_TINT_NOT_SET");

        using var check = new NifHeadFile();
        check.Load(Path.Combine(_dataRoot, result.FaceGeomPath!));
        var shape = Assert.Single(check.Shapes());
        var tint = result.FaceTintPath!.Replace('/', '\\');
        Assert.Equal(tint, check.GetSlot(shape, 6).Replace('/', '\\'), StringComparer.OrdinalIgnoreCase);
        Assert.Contains("malehead_s.dds", check.GetSlot(shape, 7), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// GravithX had no face mesh because a head we refuse to guess a tint for never gets saved.
    /// The mesh still has the eyes and hair shaders, so it has to be published.
    /// </summary>
    [Fact]
    public void HeadWithNoClaimableTint_StillWritesFaceGeom()
    {
        var nif = WriteHead(textureCount: 9, specular: null, shapeName: "Rock", vertices: 20,
            diffuse: @"actors\character\rocks.dds", slot6: @"actors\character\malebody.dds");
        var result = Swap(nif, dds: null);

        Assert.False(result.Success);
        Assert.Equal(FaceGenStatus.Partial, result.Status);
        Assert.True(result.NeedsCreationKit);
        Assert.DoesNotContain(result.Findings, f => f.Code == "FACEGEN_TINT_NOT_SET");
        Assert.True(File.Exists(Path.Combine(_dataRoot, result.FaceGeomPath!)));
        var handoff = Assert.Single(result.Findings, f => f.Code == "FACEGEN_CK_HANDOFF");
        Assert.Contains("copied", handoff.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("was not copied", handoff.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            "copied. The head mesh is in the follower; the tint path was left as exported. See the warning.",
            FaceGenPlayerText.Summary(result.Status));
    }

    private FaceGenResult Swap(string nif, string? dds)
    {
        var req = new FaceGenSwapper.Request(
            SourceNifPath: nif,
            SourceDdsPath: dds,
            PluginName: "FF_FaceGenTest.esp",
            NpcFormId: 0x800,
            DataRoot: _dataRoot,
            ActorEditorId: "FF_FaceGenTest_NPC",
            NpcFormKey: "000800:FF_FaceGenTest.esp");
        return new FaceGenSwapper(Log).Swap(req, resolver: _ => (true, "test"));
    }

    private string WriteHead(uint textureCount, string? specular, string shapeName = "MaleHeadBreton",
        int vertices = 140, string diffuse = @"actors\character\malehead.dds", string? slot6 = null)
    {
        var nif = new NifFile();
        nif.Create(NiVersion.getSSE());
        var verts = new vectorVector3();
        var tris = new vectorTriangle();
        var uvs = new vectorVector2();
        for (var i = 0; i < vertices; i++) verts.Add(new Vector3 { x = i, y = 0, z = 0 });
        for (ushort i = 0; i < vertices - 2; i++)
            tris.Add(new Triangle { p1 = i, p2 = (ushort)(i + 1), p3 = (ushort)(i + 2) });
        var shape = nif.CreateShapeFromData(shapeName, verts, tris, uvs);
        nif.SetTextureSlot(shape, diffuse, 0);
        if (specular is not null) nif.SetTextureSlot(shape, specular, 7);
        if (slot6 is not null) nif.SetTextureSlot(shape, slot6, 6);
        var shader = nif.GetShader(shape);
        var set = (BSShaderTextureSet)nif.GetHeader().GetBlockById(shader.TextureSetRef().index);
        set.textures.resize(textureCount);

        var path = Path.Combine(_dataRoot, shapeName + textureCount + ".nif");
        Directory.CreateDirectory(_dataRoot);
        using var options = new NifSaveOptions { optimize = false, sortBlocks = false };
        var rc = nif.Save(path, options);
        if (rc != 0) throw new InvalidOperationException($"Could not write test head (code {rc})");
        return path;
    }

    private static uint TextureCount(string path)
    {
        var nif = new NifFile();
        if (nif.Load(path) != 0) throw new InvalidDataException(path);
        using var shapes = nif.GetShapes();
        var shader = nif.GetShader(shapes[0]);
        var set = (BSShaderTextureSet)nif.GetHeader().GetBlockById(shader.TextureSetRef().index);
        return set.textures.size();
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dataRoot)) Directory.Delete(_dataRoot, recursive: true); }
        catch (IOException) { }
    }
}
