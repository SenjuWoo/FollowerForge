using FollowerForge.SkyrimRecords;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace FollowerForge.Tests;

/// <summary>
/// One damaged plugin in a load order of thousands must cost that plugin, not the whole index.
///
/// Reported from a real setup: the app stopped at "Could not read your setup: One or more errors
/// occurred (Exception has been thrown by the target of invocation)" with every list blank, and
/// the user had to bisect their load order by hand to find the one .esp responsible.
/// </summary>
public sealed class UnreadablePluginTests : IDisposable
{
    private static readonly ILogger Log = new LoggerConfiguration().CreateLogger();
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ff_bad_" + Guid.NewGuid().ToString("N"));

    public UnreadablePluginTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private string WritePlugin(string fileName, string npcEditorId)
    {
        var mod = new SkyrimMod(ModKey.FromFileName(fileName), SkyrimRelease.SkyrimSE);
        mod.Npcs.AddNew(npcEditorId);
        var path = Path.Combine(_dir, fileName);
        mod.WriteToBinary(path);
        return path;
    }

    /// <summary>
    /// Marks the NPC record as compressed without compressing it. The file still opens — the header
    /// and group table are intact — and fails only when the record is actually read.
    /// </summary>
    private static void CorruptFirstNpcRecord(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var group = bytes.AsSpan().IndexOf("GRUP"u8);
        var record = group + 24;
        Assert.True(bytes.AsSpan(record, 4).SequenceEqual("NPC_"u8));
        var flags = BitConverter.ToUInt32(bytes, record + 8) | 0x00040000u;
        BitConverter.TryWriteBytes(bytes.AsSpan(record + 8, 4), flags);
        File.WriteAllBytes(path, bytes);
    }

    private static void Truncate(string path)
    {
        var bytes = File.ReadAllBytes(path);
        File.WriteAllBytes(path, bytes[..(bytes.Length - 20)]);
    }

    [Fact]
    public void ARecordThatCannotBeRead_SkipsThatPlugin_AndKeepsTheRest()
    {
        WritePlugin("Good.esp", "GoodNpc");
        CorruptFirstNpcRecord(WritePlugin("Bad.esp", "BadNpc"));

        var order = LoadOrderBuilder.ImportTolerant(_dir, ["Good.esp", "Bad.esp"], Log, out var unopenable);
        var built = new LoadOrderBuilder.BuiltLoadOrder(order, [], [], unopenable);

        var indexer = new RecordIndexer(Log);
        var records = indexer.EnumerateWinningRecords(built).ToList();

        Assert.Contains(records, r => r.EditorId == "GoodNpc");
        var skipped = unopenable.Concat(indexer.Unreadable).ToList();
        Assert.Contains(skipped, u => u.Plugin.Equals("Bad.esp", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(skipped, u => u.Plugin.Equals("Good.esp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ATruncatedPlugin_IsNamed_AndDoesNotStopTheOthersLoading()
    {
        WritePlugin("Good.esp", "GoodNpc");
        Truncate(WritePlugin("Cut.esp", "CutNpc"));

        var order = LoadOrderBuilder.ImportTolerant(_dir, ["Good.esp", "Cut.esp"], Log, out var unopenable);
        var built = new LoadOrderBuilder.BuiltLoadOrder(order, [], [], unopenable);

        var indexer = new RecordIndexer(Log);
        var records = indexer.EnumerateWinningRecords(built).ToList();

        Assert.Contains(records, r => r.EditorId == "GoodNpc");
        var skipped = unopenable.Concat(indexer.Unreadable).ToList();
        Assert.Contains(skipped, u => u.Plugin.Equals("Cut.esp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AHealthyLoadOrder_ReportsNothingUnreadable()
    {
        WritePlugin("Good.esp", "GoodNpc");
        WritePlugin("AlsoGood.esp", "OtherNpc");

        var order = LoadOrderBuilder.ImportTolerant(_dir, ["Good.esp", "AlsoGood.esp"], Log, out var unopenable);
        var indexer = new RecordIndexer(Log);
        var records = indexer.EnumerateWinningRecords(
            new LoadOrderBuilder.BuiltLoadOrder(order, [], [], unopenable)).ToList();

        Assert.Empty(unopenable);
        Assert.Empty(indexer.Unreadable);
        Assert.Equal(2, records.Count(r => r.Type == Domain.IndexedRecordType.Npc));
    }

    [Fact]
    public void TheReasonIsTheRealError_NotTheReflectionWrapper()
    {
        var wrapped = new System.Reflection.TargetInvocationException(
            new AggregateException(new InvalidDataException("record 0x800 is damaged")));

        Assert.Equal("record 0x800 is damaged", UnreadablePlugin.RootMessage(wrapped));
    }
}
