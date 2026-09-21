namespace FollowerForge.FaceGen;

/// <summary>Player-facing FaceGen text. The build log is where this is read.</summary>
public static class FaceGenGuidance
{
    public static string Handoff(
        string plugin, string editorId, string reason,
        string faceGeom, string faceTint, string reportPath)
    {
        return Classify(reason)
            + $" The plugin {plugin} was still built. To bake a face from the sliders and colors already stored on {editorId}, open that plugin in the Creation Kit, select {editorId}, and press Ctrl+F4."
            + $" That creates {faceGeom} and {faceTint}."
            + " Ctrl+F4 uses the actor record, not a RaceMenu sculpt, so a sculpted head only appears when the exported mesh itself can be installed."
            + $" The same steps are saved at {reportPath}.";
    }

    public static string TintFallback(string shapeName) =>
        $"The exported head did not name its tint file. The face-tint texture on '{shapeName}' was pointed at the tint mask shipped with the follower. The specular map was left as it was.";

    private static string Classify(string reason)
    {
        if (reason.Contains("face-tint", StringComparison.OrdinalIgnoreCase))
            return "The exported head was not copied. None of its textures point at a face tint, and the head shape could not be picked out without guessing. A wrong guess is what leaves a grey or blotchy face.";
        if (reason.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return "The exported head file is missing, so no custom face was installed. " + reason;
        if (reason.Contains("no shapes", StringComparison.OrdinalIgnoreCase))
            return "That file is not a head mesh, so no custom face was installed.";
        return "The exported head could not be converted, so no custom face was installed. " + reason;
    }
}
