namespace FollowerForge.FaceGen;

/// <summary>
/// Picks the shader slot that holds a face tint.
/// BSShaderTextureSet is 0-based. Index 6 is the 7th line in NifSkope (the tint).
/// Index 7 is the specular map. Writing the tint there leaves the real tint
/// pointing at the old mask and replaces the specular map.
/// </summary>
public static class FaceTintLocator
{
    public const uint FaceTintSlot = 6;
    public const int MinHeadVertices = 101;

    public readonly record struct ShapeView(string Name, int Vertices, IReadOnlyList<string> Slots);

    public readonly record struct TintHit(int ShapeIndex, uint Slot, string OldPath, bool MatchedByPath);

    public static IReadOnlyList<TintHit> Locate(IReadOnlyList<ShapeView> shapes, string? sourceDdsName)
    {
        var hits = new List<TintHit>();
        for (var i = 0; i < shapes.Count; i++)
        {
            var slots = shapes[i].Slots;
            for (var slot = 0; slot < slots.Count; slot++)
            {
                var path = slots[slot] ?? "";
                if (IsTintReference(path, sourceDdsName))
                    hits.Add(new TintHit(i, (uint)slot, path, true));
            }
        }
        if (hits.Count > 0) return hits;

        var headIndex = FindHead(shapes);
        if (headIndex < 0) return hits;

        var slot6 = SlotAt(shapes[headIndex], FaceTintSlot);
        if (!SafeToClaim(slot6, sourceDdsName)) return hits;

        hits.Add(new TintHit(headIndex, FaceTintSlot, slot6, false));
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
