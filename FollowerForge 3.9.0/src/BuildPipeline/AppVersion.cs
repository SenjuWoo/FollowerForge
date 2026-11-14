using System.Reflection;

namespace FollowerForge.BuildPipeline;

/// <summary>The FollowerForge version, as the build stamped it ("3.9.0"), without the commit suffix.</summary>
public static class AppVersion
{
    public static string Current { get; } = Read();

    private static string Read()
    {
        var informational = typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(informational))
            return typeof(AppVersion).Assembly.GetName().Version?.ToString(3) ?? "dev";
        var plus = informational.IndexOf('+');
        return plus > 0 ? informational[..plus] : informational;
    }
}
