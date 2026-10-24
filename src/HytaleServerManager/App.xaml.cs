using Microsoft.UI.Xaml;

namespace HytaleServerManager;

public partial class App : Application
{
    private Window? _window;
    private static readonly string CrashLogPath = Path.Combine(Path.GetTempPath(), "HytaleServerManager-crash.log");

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) => LogCrash(args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) => LogCrash(args.Exception);
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            throw;
        }
        UnhandledException += (_, args) =>
        {
            LogCrash(args.Exception);
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            throw;
        }
    }

    private static void LogCrash(Exception? exception)
    {
        if (exception is null) return;
        File.AppendAllText(CrashLogPath, $"[{DateTime.Now:O}]\n{exception}\n\n");
    }
}
