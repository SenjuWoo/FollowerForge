using FollowerForge.Domain;
using FollowerForge.FaceGen;

namespace FollowerForge.Tests;

/// <summary>
/// The tint lives on BSShaderTextureSet slot 6. Slot 7 is the specular map.
/// A real exported head (MaleHeadArgonian) kept its old facetint path on slot 6
/// while 3.7.2 rewrote slot 7, which is the grey-face bug next to the CK warning.
/// </summary>
public sealed class FaceTintLocatorTests
{
    private static readonly string[] EmptySlots = ["", "", "", "", "", "", "", "", ""];

    [Fact]
    public void CharGenPath_OnSlot6_IsRewrittenThere()
    {
        var head = Slots(EmptySlots);
        head[6] = @"textures\Data\SKSE\Plugins\CharGen\Monique2.dds";
        head[7] = @"textures\actors\character\female\femalehead_s.dds";

        var hits = FaceTintLocator.Locate([Shape("AAA_GirlHeads", 3832, head)], "Other.dds");

        var hit = Assert.Single(hits);
        Assert.Equal(FaceTintLocator.FaceTintSlot, hit.Slot);
        Assert.True(hit.MatchedByPath);
    }

    [Fact]
    public void ExistingFacetint_IsRewritten_NotTheSpecularSlot()
    {
        var head = Slots(EmptySlots);
        head[6] = @"textures\Actors\Character\FaceGenData\FaceTint\MASSEESL.esp\00000812.dds";
        head[7] = @"textures\actors\character\argonianmale\ArgonianMaleHead_s.dds";

        var hits = FaceTintLocator.Locate(
            [Shape("MaleHeadArgonian", 1219, head)],
            "ff-tint-probe.dds");

        var hit = Assert.Single(hits);
        Assert.Equal(6u, hit.Slot);
        Assert.True(hit.MatchedByPath);
        Assert.Contains("00000812.dds", hit.OldPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnlabeledHead_ClaimsEmptyTintSlot_IncludingUnderOneThousandVerts()
    {
        var head = Slots(EmptySlots);
        head[0] = @"textures\actors\character\female\femalehead.dds";
        head[7] = @"textures\actors\character\female\femalehead_s.dds";

        var hits = FaceTintLocator.Locate([Shape("FemaleHeadBrown", 800, head)], "Export.dds");

        var hit = Assert.Single(hits);
        Assert.Equal(6u, hit.Slot);
        Assert.False(hit.MatchedByPath);
    }

    [Fact]
    public void HeadNamedWithoutTheWordHead_IsFoundFromItsDiffuse()
    {
        var face = Slots(EmptySlots);
        face[0] = @"textures\actors\character\female\femalehead.dds";
        var hair = Slots(EmptySlots);
        hair[0] = @"textures\actors\character\hair\temptress.dds";

        var hits = FaceTintLocator.Locate(
            [
                Shape("AAA_Hair", 11673, hair),
                Shape("AAA_Custom", 500, face),
            ],
            "Export.dds");

        var hit = Assert.Single(hits);
        Assert.Equal(1, hit.ShapeIndex);
        Assert.Equal(6u, hit.Slot);
    }

    [Fact]
    public void HairAndEyesAlone_AreNotAHead()
    {
        var hair = Slots(EmptySlots);
        hair[0] = @"textures\actors\character\hair\temptress.dds";
        var eyes = Slots(EmptySlots);
        eyes[0] = @"textures\actors\character\eyes\eye.dds";

        var hits = FaceTintLocator.Locate(
            [
                Shape("AAA_Hair", 11673, hair),
                Shape("FemaleEyesHuman", 176, eyes),
            ],
            "Export.dds");

        Assert.Empty(hits);
    }

    [Fact]
    public void SpecularSittingInTheTintSlot_IsNotOverwritten()
    {
        var head = Slots(EmptySlots);
        head[0] = @"textures\actors\character\female\femalehead.dds";
        head[6] = @"textures\actors\character\female\femalehead_s.dds";

        var hits = FaceTintLocator.Locate([Shape("FemaleHead", 2000, head)], "Export.dds");

        Assert.Empty(hits);
    }

    [Fact]
    public void HandoffText_SaysWhatToDo_AndDoesNotAskThePlayerToGuess()
    {
        var text = FaceGenGuidance.Handoff(
            "FF_Aria.esp", "FF_Aria",
            "Could not locate the face-tint texture reference in the export; unsafe to guess.",
            @"meshes\actors\character\facegendata\facegeom\FF_Aria.esp\00000800.nif",
            @"textures\actors\character\facegendata\facetint\FF_Aria.esp\00000800.dds",
            @"D:\Mods\FF_Aria\ck-handoff-report.json");

        Assert.Contains("Ctrl+F4", text);
        Assert.Contains("FF_Aria.esp", text);
        Assert.Contains("ck-handoff-report.json", text);
        Assert.DoesNotContain("unsafe to guess", text);
        Assert.DoesNotContain("FACEGEN_MANUAL", text);
    }

    [Fact]
    public void SkippedExport_IsNotDescribedAsTheDefaultFace()
    {
        var findings = new[]
        {
            new ValidationFinding(ValidationSeverity.Warning, "FACEGEN_CK_HANDOFF", "steps"),
        };
        Assert.Equal(
            "not installed. The exported head was skipped; see the warning.",
            FaceGenPlayerText.Summary(false, findings));
        Assert.Equal("default", FaceGenPlayerText.Summary(false, []));
        Assert.Equal(
            "custom (from your RaceMenu export)",
            FaceGenPlayerText.Summary(true, findings));
    }

    private static string[] Slots(string[] source) => (string[])source.Clone();

    private static FaceTintLocator.ShapeView Shape(string name, int verts, string[] slots) =>
        new(name, verts, slots);
}
