namespace HytaleServerManager.Models;

public sealed class AppSettings
{
    private static string AppData => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public string SavesRootPath { get; set; } = Path.Combine(AppData, "Hytale", "UserData", "Saves");
    public string WorldPath { get; set; } = string.Empty;
    public string JavaPath { get; set; } = Path.Combine(AppData, "Hytale", "install", "release", "package", "jre", "latest", "bin", "java.exe");
    public string ServerJarPath { get; set; } = Path.Combine(AppData, "Hytale", "install", "release", "package", "game", "latest", "Server", "HytaleServer.jar");
    public string AssetsPath { get; set; } = Path.Combine(AppData, "Hytale", "install", "release", "package", "game", "latest", "Assets.zip");
    public string ModsPath { get; set; } = Path.Combine(AppData, "Hytale", "UserData", "Mods");
    public string PrefabCachePath { get; set; } = Path.Combine(AppData, "Hytale", "UserData", "PrefabCache");
    public string EarlyPluginsPath { get; set; } = Path.Combine(AppData, "Hytale", "UserData", "EarlyPlugins");
    public string PlayitPath { get; set; } = Path.Combine(LocalAppData, "PlayitGG", "playit-windows-x86_64-signed.exe");
    public string PublicAddress { get; set; } = "cone-dq.tun.ply.gg:2172";
    public int ServerPort { get; set; } = 5520;
    public int InitialMemoryMb { get; set; } = 768;
    public int MaximumMemoryMb { get; set; } = 4096;
    public bool ShowPlayitConsole { get; set; }
}
