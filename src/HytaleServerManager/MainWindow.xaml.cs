using System.Collections.Specialized;
using HytaleServerManager.Services;
using HytaleServerManager.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

namespace HytaleServerManager;

public sealed partial class MainWindow : Window
{
    private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel(new SettingsService(), new ServerProcessService(), new WorldDiscoveryService(), DispatcherQueue);
        Root.DataContext = ViewModel;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };

        var windowId = Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(this));
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "HytaleServerManager.ico");
        if (File.Exists(iconPath)) appWindow.SetIcon(iconPath);
        appWindow.Resize(new SizeInt32(1240, 820));

        _refreshTimer.Tick += async (_, _) => await ViewModel.RefreshAsync();
        Activated += MainWindow_Activated;
        Closed += (_, _) =>
        {
            _refreshTimer.Stop();
            ViewModel.Dispose();
        };
    }

    private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= MainWindow_Activated;
        await ViewModel.InitializeAsync();
        _refreshTimer.Start();
    }

    private void LogList_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Logs is INotifyCollectionChanged changed)
        {
            changed.CollectionChanged += (_, _) =>
            {
                if (ViewModel.Logs.Count > 0)
                {
                    LogList.ScrollIntoView(ViewModel.Logs[^1]);
                }
            };
        }
    }
}
