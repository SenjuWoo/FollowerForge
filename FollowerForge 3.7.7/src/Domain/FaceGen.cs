namespace FollowerForge.Domain;

/// <summary>
/// How far the FaceGen step actually got. These four states exist because the old pair of booleans
/// could both be true at once: a mesh written from a broken export reported Success *and*
/// NeedsCreationKit, so every consumer had to guess which one to believe.
/// </summary>
public enum FaceGenStatus
{
    /// <summary>No export was selected, so nothing was attempted.</summary>
    NotAttempted,
    /// <summary>Mesh written, tint copied, tint path persisted and the saved NIF reopened clean.</summary>
    Complete,
    /// <summary>Something usable was written, but the face is not finished — e.g. the mesh exists
    /// with the tint path left exactly as exported.</summary>
    Partial,
    /// <summary>Nothing usable was written; the Creation Kit handoff report carries the steps.</summary>
    NeedsManualRepair,
    /// <summary>The conversion ran and failed outright.</summary>
    Failed,
}

/// <summary>Outcome of the FaceGen dirty-swap for one follower.</summary>
public sealed record FaceGenResult
{
    public required FaceGenStatus Status { get; init; }
    /// <summary>Only a complete conversion counts. Partial results are deliberately not "successful".</summary>
    public bool Success => Status == FaceGenStatus.Complete;
    /// <summary>True when the user has to finish or repair the face by hand.</summary>
    public bool NeedsCreationKit =>
        Status is FaceGenStatus.Partial or FaceGenStatus.NeedsManualRepair;
    public string? FaceGeomPath { get; init; }
    public string? FaceTintPath { get; init; }
    public string? CkHandoffPath { get; init; }
    public IReadOnlyList<string> ShapeNames { get; init; } = [];
    /// <summary>Texture paths referenced by the head NIF and whether each resolves in the asset index.</summary>
    public IReadOnlyList<TextureResolution> Textures { get; init; } = [];
    public IReadOnlyList<ValidationFinding> Findings { get; init; } = [];

    /// <summary>Stable word for the manifest, the report and the console line.</summary>
    public static string Describe(FaceGenStatus status) => status switch
    {
        FaceGenStatus.Complete => "complete",
        FaceGenStatus.Partial => "partial",
        FaceGenStatus.NeedsManualRepair => "needs manual repair",
        FaceGenStatus.Failed => "failed",
        _ => "not attempted",
    };
}

public sealed record TextureResolution(string Path, bool Resolved, string? Container);

/// <summary>One face line for the build log, the command line, and the HTML report.</summary>
public static class FaceGenPlayerText
{
    /// <summary>
    /// Says what the build actually produced. The old signature took a bool, which could not tell a
    /// finished face from one whose tint path was left as exported.
    /// </summary>
    public static string Summary(FaceGenStatus? status) => status switch
    {
        FaceGenStatus.Complete => "custom (from your RaceMenu export)",
        FaceGenStatus.Partial => "copied. The head mesh is in the follower; the tint path was left as "
            + "exported. See the warning.",
        FaceGenStatus.NeedsManualRepair => "not installed. The exported head was skipped; see the warning.",
        FaceGenStatus.Failed => "failed. See the warning.",
        _ => "default",
    };

    /// <summary>Back-compat shim for callers that only have the old bool plus findings.</summary>
    public static string Summary(bool installed, IEnumerable<ValidationFinding> findings)
    {
        var handoff = findings.Any(f => f.Code == "FACEGEN_CK_HANDOFF");
        // A copied mesh with a handoff is a partial face, not a failed one — the bool alone cannot
        // tell them apart, so prefer the finding that says the mesh made it in.
        if (installed) return Summary(handoff ? FaceGenStatus.Partial : FaceGenStatus.Complete);
        return Summary(handoff ? FaceGenStatus.NeedsManualRepair : FaceGenStatus.NotAttempted);
    }

    /// <summary>Same text, read from the manifest's stored word.</summary>
    public static string Summary(string? status) => Summary(status switch
    {
        "complete" => FaceGenStatus.Complete,
        "partial" => FaceGenStatus.Partial,
        "needs manual repair" => FaceGenStatus.NeedsManualRepair,
        "failed" => FaceGenStatus.Failed,
        _ => FaceGenStatus.NotAttempted,
    });
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
