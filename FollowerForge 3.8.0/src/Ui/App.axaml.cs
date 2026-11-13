using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Serilog;

namespace FollowerForge.Ui;

public sealed class App : Application
{
    private static readonly ILogger Log = UiLog.Logger;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // A crash used to leave nothing but a Windows event entry. Both hooks only record.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception; the app is closing");
        TaskScheduler.UnobservedTaskException += (_, e) =>
            Log.Error(e.Exception, "A background task failed and nothing observed it");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var preferences = UiPreferencesStore.Load(warning: message => Log.Warning("{Warning}", message));
            ThemeResources.Apply(this, preferences.Theme);
            desktop.MainWindow = new WizardWindow(preferences);
        }
        base.OnFrameworkInitializationCompleted();
    }
}
