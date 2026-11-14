namespace FollowerForge.Tests;

/// <summary>
/// The tests that compile and publish a real plugin share more than a temp folder: the ship gate,
/// the file-system publisher and the manager guard all touch process-wide resources. Running them
/// in parallel produced one random failure every few runs — a different test each time — which made
/// the publish script's test step unreliable.
///
/// Serialising them costs about a second and makes the gate deterministic.
/// </summary>
[CollectionDefinition("builds", DisableParallelization = true)]
public sealed class BuildCollection
{
}
