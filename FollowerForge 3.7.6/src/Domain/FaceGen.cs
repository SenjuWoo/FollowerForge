namespace FollowerForge.Domain;

/// <summary>Outcome of the FaceGen dirty-swap for one follower.</summary>
public sealed record FaceGenResult
{
    public required bool Success { get; init; }
    /// <summary>True when conversion was declined and a CK handoff report was written instead.</summary>
    public bool NeedsCreationKit { get; init; }
    public string? FaceGeomPath { get; init; }
    public string? FaceTintPath { get; init; }
    public string? CkHandoffPath { get; init; }
    public IReadOnlyList<string> ShapeNames { get; init; } = [];
    /// <summary>Texture paths referenced by the head NIF and whether each resolves in the asset index.</summary>
    public IReadOnlyList<TextureResolution> Textures { get; init; } = [];
    public IReadOnlyList<ValidationFinding> Findings { get; init; } = [];
}

public sealed record TextureResolution(string Path, bool Resolved, string? Container);

/// <summary>One face line for the build log, the command line, and the HTML report.</summary>
public static class FaceGenPlayerText
{
    public static string Summary(bool installed, IEnumerable<ValidationFinding> findings)
    {
        var handoff = findings.Any(f => f.Code == "FACEGEN_CK_HANDOFF");
        // A saved mesh with no claimable tint is installed, and it is not a finished face.
        // The one-line summary has to say the tint was left alone, or the warning is the only place that does.
        if (installed && handoff)
            return "copied. The head mesh is in the follower; the tint path was left as exported. See the warning.";
        if (installed) return "custom (from your RaceMenu export)";
        if (handoff)
            return "not installed. The exported head was skipped; see the warning.";
        return "default";
    }
}

/// <summary>Data for the Creation Kit handoff when automatic FaceGen conversion is unsafe.</summary>
public sealed record CkHandoff(
    string PluginName,
    string ActorEditorId,
    string NpcFormKey,
    string ExpectedFaceGeomPath,
    string ExpectedFaceTintPath,
    string Reason,
    IReadOnlyList<string> Steps);
