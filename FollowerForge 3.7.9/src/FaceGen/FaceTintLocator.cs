namespace FollowerForge.FaceGen;

/// <summary>
/// Picks the shader slot that holds a face tint.
/// BSShaderTextureSet is 0-based. Index 6 is the 7th line in NifSkope (the tint).
/// Index 7 is the specular map. Writing the tint there leaves the real tint
/// pointing at the old mask and replaces the specular map.
///
/// Only slot 6 is ever written. An earlier version scanned every slot and accepted any path
/// containing "\CharGen\" or "\facetint\", so an exported head whose diffuse or normal map also
/// lived under CharGen had that texture replaced by a tint path — a broken face produced by the
/// step that exists to fix one.
/// </summary>
public static class FaceTintLocator
{
    public const uint FaceTintSlot = 6;
    public const int MinHeadVertices = 101;

    public readonly record struct ShapeView(string Name, int Vertices, IReadOnlyList<string> Slots);

    public readonly record struct TintHit(int ShapeIndex, uint Slot, string OldPath, bool MatchedByPath);

    /// <summary>
    /// The shapes to repoint, always at <see cref="FaceTintSlot"/>. The head shape is claimed when
    /// its tint slot is empty or already a tint; another shape is claimed only when its own tint
    /// slot already reads as a tint, which is the export saying it uses one.
    /// </summary>
    public static IReadOnlyList<TintHit> Locate(IReadOnlyList<ShapeView> shapes, string? sourceDdsName)
    {
        var hits = new List<TintHit>();
        var headIndex = FindHead(shapes);

        for (var i = 0; i < shapes.Count; i++)
        {
            var current = SlotAt(shapes[i], FaceTintSlot);
            var isTint = IsTintReference(current, sourceDdsName);

            // Not the head, and its own slot 6 is not a tint: leave the shape untouched.
            if (i != headIndex && !isTint) continue;
            // A skin, hair or normal map sitting in slot 6 is not a tint either.
            if (!string.IsNullOrWhiteSpace(current) && !isTint) continue;

            hits.Add(new TintHit(i, FaceTintSlot, current, MatchedByPath: isTint));
        }

        return hits;
    }

    /// <summary>
    /// A path is a tint reference when it is the CharGen export, an existing facetint
    /// mask, or the tint file that was found beside the mesh. Matching the filename
    /// covers a path with no directory.
    /// </summary>
    public static bool IsTintReference(string? path, string? sourceDdsName)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var normalized = path.Replace('/', '\\');
        if (normalized.Contains(@"\chargen\", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith(@"chargen\", StringComparison.OrdinalIgnoreCase))
            return true;
        if (normalized.Contains(@"\facetint\", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.IsNullOrWhiteSpace(sourceDdsName)) return false;
        return Path.GetFileName(normalized).Equals(sourceDdsName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Empty, or already a tint. A skin or specular path is not claimed.</summary>
    public static bool SafeToClaim(string? path, string? sourceDdsName) =>
        string.IsNullOrWhiteSpace(path) || IsTintReference(path, sourceDdsName);

    public static int FindHead(IReadOnlyList<ShapeView> shapes)
    {
        var named = Best(shapes, static s => s.Vertices > MinHeadVertices && NameIsHead(s.Name));
        if (named >= 0) return named;
        return Best(shapes, static s => s.Vertices > MinHeadVertices && DiffuseIsHead(SlotAt(s, 0)));
    }

    private static int Best(IReadOnlyList<ShapeView> shapes, Func<ShapeView, bool> match)
    {
        var best = -1;
        var bestVerts = -1;
        for (var i = 0; i < shapes.Count; i++)
        {
            if (!match(shapes[i]) || shapes[i].Vertices <= bestVerts) continue;
            best = i;
            bestVerts = shapes[i].Vertices;
        }
        return best;
    }

    private static bool NameIsHead(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return name.Contains("head", StringComparison.OrdinalIgnoreCase)
            && !name.Contains("hair", StringComparison.OrdinalIgnoreCase);
    }

    private static bool DiffuseIsHead(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        return Path.GetFileName(path.Replace('/', '\\'))
            .Contains("head", StringComparison.OrdinalIgnoreCase);
    }

    private static string SlotAt(ShapeView shape, uint slot) =>
        slot < shape.Slots.Count ? shape.Slots[(int)slot] ?? "" : "";
}
