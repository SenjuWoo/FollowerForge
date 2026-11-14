using FollowerForge.Domain;
using FollowerForge.FaceGen;
using nifly;
using Serilog;

namespace FollowerForge.Tests;

/// <summary>
/// The locator's job is to repoint one slot. These tests are the adversarial half: an export whose
/// other textures also live under CharGen, and a second shape that carries hair rather than a face
/// tint. Getting this wrong replaces a diffuse or a normal map with a tint path, which produces the
/// broken face the step exists to prevent.
/// </summary>
public sealed class FaceTintAdversarialTests : IDisposable
{
    private static readonly ILogger Log = new LoggerConfiguration().CreateLogger();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ff_tint_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch (IOException) { }
    }

    private const string CharGenDiffuse = @"textures\actors\character\chargen\head_diffuse.dds";
    private const string CharGenNormal = @"textures\actors\character\chargen\head_normal.dds";
    private const string CharGenSpecular = @"textures\actors\character\chargen\head_specular.dds";
    private const string HairDiffuse = @"actors\character\hair\hairdiffuse.dds";

    /// <summary>Writes a head, optionally with a second hair shape, and the given slot paths.</summary>
    private string WriteExport(string name, string? hairShapeName = null, string? hairSlot6 = null)
    {
        var nif = new NifFile();
        nif.Create(NiVersion.getSSE());
        var head = AddShape(nif, name, 140);
        nif.SetTextureSlot(head, CharGenDiffuse, 0);
        nif.SetTextureSlot(head, CharGenNormal, 1);
        nif.SetTextureSlot(head, CharGenSpecular, 7);
        // Slot 6 deliberately left empty: the tint has to be claimed there without touching the rest.

        if (hairShapeName is not null)
        {
            var hair = AddShape(nif, hairShapeName, 120);
            nif.SetTextureSlot(hair, HairDiffuse, 0);
            if (hairSlot6 is not null) nif.SetTextureSlot(hair, hairSlot6, 6);
        }

        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, name + ".nif");
        using var options = new NifSaveOptions { optimize = false, sortBlocks = false };
        if (nif.Save(path, options) != 0) throw new InvalidOperationException("could not write test export");
        return path;
    }

    /// <summary>A DDS header is enough for the copy; nothing reads its pixels.</summary>
    private string WriteTintDds(string name = "tint.dds")
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, name);
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

    private FaceGenResult Swap(string nifPath, string ddsPath, string plugin = "FF_Tint.esp")
    {
        var req = new FaceGenSwapper.Request(
            SourceNifPath: nifPath,
            SourceDdsPath: ddsPath,
            PluginName: plugin,
            NpcFormId: 0x800,
            DataRoot: _root,
            ActorEditorId: "FF_Tint_NPC",
            NpcFormKey: "000800:" + plugin);
        return new FaceGenSwapper(Log).Swap(req, resolver: _ => (true, "test"));
    }

    private static Dictionary<string, string[]> SlotsByName(string path)
    {
        using var nif = new NifHeadFile();
        nif.Load(path);
        return nif.Shapes().ToDictionary(
            nif.ShapeName,
            shape => Enumerable.Range(0, (int)NifHeadFile.SlotCount)
                .Select(i => nif.GetSlot(shape, (uint)i))
                .ToArray());
    }

    [Fact]
    public void CharGen_textures_outside_the_tint_slot_are_never_repointed()
    {
        var source = WriteExport("HeadNoTint");
        var before = SlotsByName(source);

        var result = Swap(source, WriteTintDds());
        Assert.Equal(FaceGenStatus.Complete, result.Status);

        var after = SlotsByName(Path.Combine(_root, result.FaceGeomPath!));
        const string name = "HeadNoTint";
        // Byte-for-byte strings: the same characters the export carried, not a normalised copy.
        foreach (var slot in new[] { 0, 1, 7 })
            Assert.Equal(before[name][slot], after[name][slot]);

        // And the tint went where it belongs.
        Assert.Contains(@"facetint", after[name][6], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_hair_shape_keeps_its_own_textures()
    {
        var source = WriteExport("HeadAndHair", hairShapeName: "MaleHair", hairSlot6: @"hair\hair_flow.dds");
        var before = SlotsByName(source);

        var result = Swap(source, WriteTintDds());
        var after = SlotsByName(Path.Combine(_root, result.FaceGeomPath!));

        Assert.Equal(before["MaleHair"][0], after["MaleHair"][0]);
        Assert.Equal(before["MaleHair"][6], after["MaleHair"][6]);
        Assert.Contains(@"facetint", after["HeadAndHair"][6], StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The brief's specific attack: a CharGen path in slot 0, 1 and 7 at once must not produce a
    /// single hit in any of them.
    /// </summary>
    [Fact]
    public void The_locator_only_ever_returns_the_tint_slot()
    {
        var slots = new string[9];
        slots[0] = CharGenDiffuse;
        slots[1] = CharGenNormal;
        slots[7] = CharGenSpecular;
        var shapes = new List<FaceTintLocator.ShapeView>
        {
            new("MaleHeadBreton", 140, slots),
            new("MaleHair", 120, [HairDiffuse, "", "", "", "", "", @"hair\hair_flow.dds", "", ""]),
        };

        var hits = FaceTintLocator.Locate(shapes, null);

        Assert.All(hits, h => Assert.Equal(FaceTintLocator.FaceTintSlot, h.Slot));
        Assert.Single(hits);
        Assert.Equal(0, hits[0].ShapeIndex);
    }

    [Fact]
    public void A_shape_whose_tint_slot_holds_a_skin_map_is_left_alone()
    {
        var slots = new string[9];
        slots[6] = @"actors\character\female\skin.dds";
        var shapes = new List<FaceTintLocator.ShapeView> { new("MaleHeadBreton", 140, slots) };

        Assert.Empty(FaceTintLocator.Locate(shapes, null));
    }
}
