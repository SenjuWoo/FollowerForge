using Serilog;

namespace FollowerForge.Ui;

/// <summary>
/// The window's log. Through 3.7.11 every logger in the app was created with no sink at all, so
/// each error the window caught and "logged" was thrown away: there was nothing to send when a
/// build or a scan failed, and a crash left only a Windows event entry. The command line always
/// wrote to this folder; the window now writes beside it.
/// </summary>
public static class UiLog
{
    /// <summary>%LOCALAPPDATA%\FollowerForge\logs, the same folder the command line uses.</summary>
    public static string Directory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FollowerForge", "logs");

    public static ILogger Logger { get; } = Create();

    private static ILogger Create()
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            return new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    Path.Combine(Directory, "ui-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 8_000_000,
                    shared: true)
                .CreateLogger();
        }
        catch (Exception)
        {
            // A read-only profile must not stop the app from starting; it only loses the log.
            return new LoggerConfiguration().CreateLogger();
        }
    }
}
