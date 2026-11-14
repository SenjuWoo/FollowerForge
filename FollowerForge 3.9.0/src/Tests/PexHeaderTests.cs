using System.Buffers.Binary;
using System.Text;
using FollowerForge.BuildPipeline;

namespace FollowerForge.Tests;

/// <summary>
/// A Papyrus compiler writes the Windows account name and the computer name of whoever compiled a
/// script into its header. The bundled scripts are copied into every follower package the app
/// builds, so through 3.7.11 those two strings went out with every follower anyone made. The game
/// reads neither; tools\Scrub-PexHeader.ps1 replaces both, and this keeps them replaced.
/// </summary>
public sealed class PexHeaderTests
{
    private const string Token = "FollowerForge";

    public static IEnumerable<object[]> BundledScripts() =>
        typeof(FollowerBuilder).Assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".pex", StringComparison.OrdinalIgnoreCase))
            .Select(name => new object[] { name });

    private static (string Source, string User, string Machine) ReadHeader(string resource)
    {
        using var stream = typeof(FollowerBuilder).Assembly.GetManifestResourceStream(resource)!;
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);

        Assert.Equal(0xFA57C0DEu, BinaryPrimitives.ReadUInt32BigEndian(bytes));
        var offset = 16;
        string Next()
        {
            var length = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset));
            var text = Encoding.ASCII.GetString(bytes, offset + 2, length);
            offset += 2 + length;
            return text;
        }
        return (Next(), Next(), Next());
    }

    [Fact]
    public void FourScriptsAreBundled()
    {
        Assert.Equal(4, BundledScripts().Count());
    }

    [Theory]
    [MemberData(nameof(BundledScripts))]
    public void BundledScript_CarriesNoAccountOrMachineName(string resource)
    {
        var (source, user, machine) = ReadHeader(resource);

        Assert.Equal(Token, user);
        Assert.Equal(Token, machine);
        // The source field is a bare file name, never a path into someone's folders.
        Assert.Equal(Path.GetFileName(source), source);
        Assert.EndsWith(".psc", source, StringComparison.OrdinalIgnoreCase);
    }
}
