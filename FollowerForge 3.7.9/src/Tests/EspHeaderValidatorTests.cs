using FollowerForge.Domain;
using FollowerForge.Validation;

namespace FollowerForge.Tests;

/// <summary>
/// Malformed-binary coverage for the ship gate. These build a plugin byte by byte so the exact
/// damage is known, which a Mutagen-written file cannot express: a truncated record, a FormID
/// naming a master that is not declared, and bytes after the last group.
/// </summary>
public sealed class EspHeaderValidatorTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ff_esp_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private const int Tes4Header = 24;
    private const int GroupHeader = 24;

    /// <summary>
    /// A minimal but real-shaped plugin: TES4 with HEDR(1.71) + one master, then one top-level group
    /// holding one NPC_ record. Every knob the tests need is a parameter.
    /// </summary>
    private static byte[] Build(
        uint recordFormId = 0x01000800,
        uint? groupSizeOverride = null,
        uint? hedrCountOverride = null,
        byte[]? trailing = null,
        ushort recordFormVersion = 44,
        byte[]? junkInsideGroup = null,
        bool nested = false)
    {
        // One record; one group, or two when nested. HEDR counts records plus GRUPs.
        var groups = nested ? 2u : 1u;
        byte[] hedrPayload =
        [
            .. BitConverter.GetBytes(1.71f),
            .. BitConverter.GetBytes(hedrCountOverride ?? (1u + groups)),
            .. BitConverter.GetBytes(0x800u),
        ];
        var hedr = Subrecord("HEDR", hedrPayload);
        var mast = Subrecord("MAST", "Skyrim.esm\0"u8.ToArray());

        var tes4Data = hedr.Concat(mast).ToArray();
        var tes4 = MainRecord("TES4", 0x00000200, 0, tes4Data, formVersion: 44);

        var body = new byte[8];                                    // record payload, contents irrelevant
        var child = MainRecord("NPC_", 0, recordFormId, body, recordFormVersion);
        var junk = junkInsideGroup ?? [];

        byte[] content;
        if (nested)
        {
            // Inner group holds the record plus any junk; the outer group holds the inner one.
            var innerSize = (uint)(GroupHeader + child.Length + junk.Length);
            content = [.. Group(innerSize, "NPC_"), .. child, .. junk];
        }
        else
        {
            content = [.. child, .. junk];
        }

        var groupSize = groupSizeOverride ?? (uint)(GroupHeader + content.Length);
        var group = Group(groupSize, "NPC_").Concat(content).ToArray();

        var bytes = tes4.Concat(group).ToList();
        if (trailing is not null) bytes.AddRange(trailing);
        return bytes.ToArray();
    }

    private static byte[] Group(uint size, string label)
    {
        var head = new List<byte>();
        head.AddRange("GRUP"u8.ToArray());
        head.AddRange(BitConverter.GetBytes(size));
        head.AddRange(System.Text.Encoding.ASCII.GetBytes(label.PadRight(4).Substring(0, 4)));
        head.AddRange(BitConverter.GetBytes(0));                   // group type
        head.AddRange(new byte[2]);                                // stamp
        head.AddRange(new byte[2]);                                // version control
        head.AddRange(BitConverter.GetBytes(0));                   // unknown
        return head.ToArray();
    }

    private static byte[] Subrecord(string sig, byte[] payload)
    {
        var outp = new List<byte>();
        outp.AddRange(System.Text.Encoding.ASCII.GetBytes(sig));
        outp.AddRange(BitConverter.GetBytes((ushort)payload.Length));
        outp.AddRange(payload);
        return outp.ToArray();
    }

    private static byte[] MainRecord(string sig, uint flags, uint formId, byte[] data, ushort formVersion)
    {
        var outp = new List<byte>();
        outp.AddRange(System.Text.Encoding.ASCII.GetBytes(sig));
        outp.AddRange(BitConverter.GetBytes((uint)data.Length));
        outp.AddRange(BitConverter.GetBytes(flags));
        outp.AddRange(BitConverter.GetBytes(formId));
        // Mutagen (and this validator) put the record's formVersion at header offset 20.
        outp.AddRange(new byte[4]);                                // version control + padding
        outp.AddRange(BitConverter.GetBytes(formVersion));
        outp.AddRange(new byte[2]);                                // padding to 24
        outp.AddRange(data);
        return outp.ToArray();
    }

    private ValidationReport Run(byte[] bytes, bool requireEsl = true)
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "FF_Test.esp");
        File.WriteAllBytes(path, bytes);
        var report = new ValidationReport();
        EspHeaderValidator.Validate(path, report, requireEsl);
        return report;
    }

    private static string[] Codes(ValidationReport r) => r.Findings.Select(f => f.Code).ToArray();

    [Fact]
    public void A_well_formed_plugin_passes_the_gate()
    {
        var report = Run(Build());

        Assert.DoesNotContain("ESP_MASTER_INDEX", Codes(report));
        Assert.DoesNotContain("ESP_TRUNCATED", Codes(report));
        Assert.DoesNotContain("ESP_TRAILING_DATA", Codes(report));
        Assert.Contains("SHIP_GATE_PASS", Codes(report));
    }

    [Fact]
    public void A_form_id_naming_an_undeclared_master_is_rejected()
    {
        // One master declared, so the plugin's own slot is 1; index 3 names a master that is not there.
        var report = Run(Build(recordFormId: 0x03000800));

        Assert.Contains("ESP_MASTER_INDEX", Codes(report));
        Assert.Contains(report.Findings, f => f.Code == "ESP_MASTER_INDEX" && f.Message.Contains("03000800"));
    }

    [Fact]
    public void A_record_that_runs_past_its_group_is_rejected()
    {
        // Rewrite the child record's declared size so it claims more bytes than the group holds.
        var bytes = Build();
        var childOffset = Tes4Header + Tes4DataLength(bytes) + GroupHeader;
        BitConverter.GetBytes(4096u).CopyTo(bytes, childOffset + 4);

        var damaged = Run(bytes);

        Assert.Contains("ESP_TRUNCATED", Codes(damaged));
        Assert.DoesNotContain("SHIP_GATE_PASS", Codes(damaged));
    }

    [Fact]
    public void Bytes_after_the_last_group_are_rejected()
    {
        var report = Run(Build(trailing: "GARBAGE!"u8.ToArray()));

        Assert.Contains("ESP_TRAILING_DATA", Codes(report));
        Assert.DoesNotContain("SHIP_GATE_PASS", Codes(report));
    }

    [Fact]
    public void A_corrupt_group_size_is_rejected()
    {
        var report = Run(Build(groupSizeOverride: 8));

        Assert.Contains("ESP_GRUP", Codes(report));
    }

    [Fact]
    public void The_hedr_count_must_include_groups()
    {
        // HEDR convention is records + GRUPs. Claiming only the record breaks it.
        var report = Run(Build(hedrCountOverride: 1));

        Assert.Contains("ESP_HEDR_COUNT", Codes(report));
    }

    [Fact]
    public void A_record_with_the_wrong_form_version_is_rejected()
    {
        var report = Run(Build(recordFormVersion: 43));

        Assert.Contains("ESP_FV_44", Codes(report));
    }

    [Fact]
    public void A_missing_esl_flag_is_rejected_when_required()
    {
        var bytes = Build();
        BitConverter.GetBytes(0u).CopyTo(bytes, 8);                // clear the TES4 flags

        Assert.Contains("ESP_NO_ESL", Codes(Run(bytes)));
        Assert.DoesNotContain("ESP_NO_ESL", Codes(Run(bytes, requireEsl: false)));
    }

    /// <summary>The TES4 data length is the dword at offset 4; the group starts right after it.</summary>
    private static int Tes4DataLength(byte[] bytes) => (int)BitConverter.ToUInt32(bytes, 4);

    // ---- a group must consume exactly the bytes it declares ------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(23)]
    public void Junk_inside_a_group_is_rejected(int junkBytes)
    {
        var report = Run(Build(junkInsideGroup: new byte[junkBytes]));

        Assert.Contains("ESP_GRUP_LEFTOVER", Codes(report));
        Assert.DoesNotContain("SHIP_GATE_PASS", Codes(report));
    }

    [Fact]
    public void Junk_inside_a_nested_group_is_rejected()
    {
        var report = Run(Build(junkInsideGroup: new byte[9], nested: true));

        Assert.Contains("ESP_GRUP_LEFTOVER", Codes(report));
        Assert.DoesNotContain("SHIP_GATE_PASS", Codes(report));
    }

    [Fact]
    public void A_clean_nested_group_still_passes()
    {
        var report = Run(Build(nested: true));

        Assert.DoesNotContain("ESP_GRUP_LEFTOVER", Codes(report));
        Assert.Contains("SHIP_GATE_PASS", Codes(report));
    }

    [Fact]
    public void A_clean_group_without_junk_passes()
    {
        var report = Run(Build());

        Assert.DoesNotContain("ESP_GRUP_LEFTOVER", Codes(report));
        Assert.Contains("SHIP_GATE_PASS", Codes(report));
    }

    /// <summary>A truncated TES4 or a short HEDR must be a finding, never an exception.</summary>
    [Fact]
    public void Truncated_headers_produce_findings_rather_than_throwing()
    {
        var tooShort = Run([0x54, 0x45, 0x53, 0x34]);          // "TES4" and nothing else
        Assert.Contains("ESP_NOT_TES4", Codes(tooShort));

        var noHedr = Build();
        BitConverter.GetBytes(600u).CopyTo(noHedr, 4);         // TES4 claims more than it holds
        var report = Run(noHedr);
        Assert.NotEmpty(report.Findings);
    }
}
