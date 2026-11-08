using FollowerForge.Domain;
using FollowerForge.FaceGen;
using nifly;
using Serilog;

namespace FollowerForge.Tests;

/// <summary>
/// The missing-texture report used to deduplicate a path before deciding whether it mattered, so a
/// path seen first in an optional slot was reported as harmless even when a diffuse slot used it
/// too. Severity is now decided from every slot that uses the path.
/// </summary>
public sealed class FaceTextureSeverityTests : IDisposable
{
    private static readonly ILogger Log = new LoggerConfiguration().CreateLogger();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ff_tex_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch (IOException) { }
    }

    // Deliberately not under CharGen: a slot-6 CharGen path is a tint reference and gets claimed,
    // which is correct and covered by the tint tests.
    private const string Missing = @"textures\actors\character\hair\gone.dds";
    private const string Present = @"textures\actors\character\malehead.dds";

    /// <summary>Two shapes, so a path can appear in an optional slot before a required one.</summary>
    private string WriteExport(Action<NifFile, NiShape, NiShape> place)
    {
        var nif = new NifFile();
        nif.Create(NiVersion.getSSE());
        var first = AddShape(nif, "MaleHair", 120);
        var head = AddShape(nif, "MaleHeadBreton", 140);
        nif.SetTextureSlot(head, Present, 0);
        place(nif, first, head);

        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, "export.nif");
        using var options = new NifSaveOptions { optimize = false, sortBlocks = false };
        if (nif.Save(path, options) != 0) throw new InvalidOperationException("could not write export");
        return path;
    }

    private string WriteDds()
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, "tint.dds");
        var bytes = new byte[128];
        bytes[0] = (byte)'D'; bytes[1] = (byte)'D'; bytes[2] = (byte)'S'; bytes[3] = (byte)' ';
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static NiShape AddShape(NifFile nif, string name, int vertices)
    {
        var verts = new vectorVector3();
        var tris = new vectorTriangle();
        for (var i = 0; i < vertices; i++) verts.Add(new Vector3 { x = i, y = 0, z = 0 });
        for (ushort i = 0; i < vertices - 2; i++)
            tris.Add(new Triangle { p1 = i, p2 = (ushort)(i + 1), p3 = (ushort)(i + 2) });
        return nif.CreateShapeFromData(name, verts, tris, new vectorVector2());
    }

    /// <summary>Only the tint is present; anything else resolves as missing.</summary>
    private FaceGenResult Swap(string nifPath)
    {
        var req = new FaceGenSwapper.Request(
            SourceNifPath: nifPath,
            SourceDdsPath: WriteDds(),
            PluginName: "FF_Tex.esp",
            NpcFormId: 0x800,
            DataRoot: _root,
            ActorEditorId: "FF_Tex_NPC",
            NpcFormKey: "000800:FF_Tex.esp");
        return new FaceGenSwapper(Log).Swap(req,
            resolver: tex => tex.Equals(@"textures\actors\character\malehead.dds", StringComparison.OrdinalIgnoreCase)
                ? (true, "test")
                : (false, null));
    }

    private static string[] Codes(FaceGenResult r, string code) =>
        r.Findings.Where(f => f.Code == code).Select(f => f.Message).ToArray();

    [Fact]
    public void A_missing_texture_used_only_in_an_optional_slot_is_informational()
    {
        var nif = WriteExport((n, hair, head) => n.SetTextureSlot(hair, Missing, 6));
        var result = Swap(nif);

        Assert.Single(Codes(result, "FACEGEN_TEX_OPTIONAL"));
        Assert.Empty(Codes(result, "FACEGEN_TEX_MISSING"));
    }

    [Fact]
    public void A_missing_diffuse_is_a_warning()
    {
        var nif = WriteExport((n, hair, head) => n.SetTextureSlot(head, Missing, 0));
        var result = Swap(nif);

        Assert.Single(Codes(result, "FACEGEN_TEX_MISSING"));
        Assert.Empty(Codes(result, "FACEGEN_TEX_OPTIONAL"));
    }

    /// <summary>The reported bug: optional slot first, diffuse later, same path.</summary>
    [Fact]
    public void A_path_in_an_optional_slot_first_and_a_diffuse_slot_later_is_required()
    {
        var nif = WriteExport((n, hair, head) =>
        {
            n.SetTextureSlot(hair, Missing, 6);      // optional, and iterated first
            n.SetTextureSlot(head, Missing, 0);      // diffuse, same path
        });
        var result = Swap(nif);

        var missing = Codes(result, "FACEGEN_TEX_MISSING");
        Assert.Single(missing);
        Assert.Contains(Missing, missing[0], StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Codes(result, "FACEGEN_TEX_OPTIONAL"));
    }

    [Fact]
    public void A_path_in_a_normal_slot_first_and_an_optional_slot_later_is_required()
    {
        var nif = WriteExport((n, hair, head) =>
        {
            n.SetTextureSlot(head, Missing, 1);      // normal
            n.SetTextureSlot(hair, Missing, 6);      // optional, same path
        });
        var result = Swap(nif);

        Assert.Single(Codes(result, "FACEGEN_TEX_MISSING"));
        Assert.Empty(Codes(result, "FACEGEN_TEX_OPTIONAL"));
    }

    [Fact]
    public void A_resolved_texture_produces_no_missing_warning()
    {
        var nif = WriteExport((n, hair, head) => n.SetTextureSlot(head, Present, 0));
        var result = Swap(nif);

        Assert.Empty(Codes(result, "FACEGEN_TEX_MISSING"));
        Assert.Empty(Codes(result, "FACEGEN_TEX_OPTIONAL"));
        Assert.Contains(result.Textures, t => t.Path.Equals(Present, StringComparison.OrdinalIgnoreCase) && t.Resolved);
    }

    [Fact]
    public void One_missing_path_gets_one_finding_however_many_slots_use_it()
    {
        var nif = WriteExport((n, hair, head) =>
        {
            n.SetTextureSlot(head, Missing, 0);
            n.SetTextureSlot(head, Missing, 1);
            n.SetTextureSlot(hair, Missing, 6);
        });
        var result = Swap(nif);

        Assert.Single(Codes(result, "FACEGEN_TEX_MISSING"));
        Assert.Single(result.Findings, f => f.Message.Contains(Missing, StringComparison.OrdinalIgnoreCase));
    }
}
