using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using HytaleServerManager.Models;

namespace HytaleServerManager.Services;

public sealed class ServerProcessService : IDisposable
{
    private const int SwHide = 0;
    private const int SwShow = 5;
    private Process? _serverProcess;
    private Process? _playitProcess;
    private bool _forceTunnelVisible;
    private TimeSpan _lastCpuTime;
    private DateTimeOffset _lastCpuSample = DateTimeOffset.UtcNow;

    public event Action<string>? LogReceived;

    public bool CanSendServerCommands
    {
        get
        {
            try { return _serverProcess is { HasExited: false }; }
            catch { return false; }
        }
    }

    public async Task StartAllAsync(AppSettings settings)
    {
        Validate(settings);
        _forceTunnelVisible = false;
        StartTunnelCore(settings, minimized: true);

        if (FindServerProcess(settings) is not null)
        {
            WriteLog("O servidor já está em execução.");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = settings.JavaPath,
            WorkingDirectory = settings.WorldPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add($"-Xms{settings.InitialMemoryMb}M");
        startInfo.ArgumentList.Add($"-Xmx{settings.MaximumMemoryMb}M");
        startInfo.ArgumentList.Add("-XX:+IgnoreUnrecognizedVMOptions");
        startInfo.ArgumentList.Add("-XX:+UseCompactObjectHeaders");
        startInfo.ArgumentList.Add("-XX:+UseShenandoahGC");
        startInfo.ArgumentList.Add("-XX:ShenandoahGCMode=generational");
        startInfo.ArgumentList.Add("-XX:+UnlockExperimentalVMOptions");
        startInfo.ArgumentList.Add("-jar");
        startInfo.ArgumentList.Add(settings.ServerJarPath);
        startInfo.ArgumentList.Add("--bind");
        startInfo.ArgumentList.Add($"0.0.0.0:{settings.ServerPort}");
        startInfo.ArgumentList.Add($"--assets={settings.AssetsPath}");
        startInfo.ArgumentList.Add($"--mods={settings.ModsPath}");
        startInfo.ArgumentList.Add($"--prefab-cache={settings.PrefabCachePath}");
        startInfo.ArgumentList.Add($"--early-plugins={settings.EarlyPluginsPath}");
        startInfo.ArgumentList.Add("--transport");
        startInfo.ArgumentList.Add("QUICHE");

        _serverProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _serverProcess.OutputDataReceived += (_, e) => { if (e.Data is not null) WriteLog(e.Data); };
        _serverProcess.ErrorDataReceived += (_, e) => { if (e.Data is not null) WriteLog(e.Data); };
        _serverProcess.Exited += (_, _) => WriteLog("O processo do servidor foi encerrado.");

        WriteLog("Iniciando o servidor Hytale...");
        if (!_serverProcess.Start()) throw new InvalidOperationException("O processo Java não pôde ser iniciado.");
        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();
        _lastCpuTime = TimeSpan.Zero;
        _lastCpuSample = DateTimeOffset.UtcNow;
        await Task.CompletedTask;
    }

    public async Task StopServerAsync()
    {
        var process = _serverProcess;
        if (process is null || process.HasExited)
        {
            WriteLog("O servidor não foi iniciado por esta interface ou já está desligado.");
            return;
        }

        WriteLog("Solicitando salvamento e encerramento seguro com /stop...");
        await process.StandardInput.WriteLineAsync("/stop");
        await process.StandardInput.FlushAsync();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            WriteLog("Servidor encerrado com segurança.");
        }
        catch (OperationCanceledException)
        {
            WriteLog("O servidor não encerrou em 30 segundos. Nenhum encerramento forçado foi executado.");
            throw new TimeoutException("O servidor ainda está executando. Confira o console antes de forçar o encerramento.");
        }
    }

    public async Task SendServerCommandAsync(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;
        if (_serverProcess is null || _serverProcess.HasExited)
            throw new InvalidOperationException("O console só aceita comandos para um servidor iniciado por esta interface.");

        await _serverProcess.StandardInput.WriteLineAsync(command.Trim());
        await _serverProcess.StandardInput.FlushAsync();
        WriteLog($"> {command.Trim()}");
    }

    public void StartTunnelSetup(AppSettings settings)
    {
        if (!File.Exists(settings.PlayitPath))
            throw new FileNotFoundException("Instale o agente oficial do Playit.gg antes de vinculá-lo.", settings.PlayitPath);

        var existing = _playitProcess ?? FindPlayitProcess();
        if (existing is { HasExited: false })
        {
            existing.Refresh();
            if (existing.MainWindowHandle == IntPtr.Zero)
            {
                existing.Kill(entireProcessTree: true);
                existing.WaitForExit(5000);
                _playitProcess = null;
            }
        }

        _forceTunnelVisible = true;
        StartTunnelCore(settings, minimized: false);
    }

    public void StartTunnel(AppSettings settings)
    {
        if (!File.Exists(settings.PlayitPath))
            throw new FileNotFoundException("Instale o agente oficial do Playit.gg antes de iniciar o túnel.", settings.PlayitPath);
        _forceTunnelVisible = false;
        StartTunnelCore(settings, minimized: true);
    }

    public void ApplyTunnelWindowPreference(AppSettings settings)
    {
        var process = _playitProcess ?? FindPlayitProcess();
        if (process is null || process.HasExited) return;

        process.Refresh();
        if (process.MainWindowHandle != IntPtr.Zero)
            ShowWindow(process.MainWindowHandle, _forceTunnelVisible || settings.ShowPlayitConsole ? SwShow : SwHide);
    }

    public static void OpenUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("O endereço recebido não é uma URL segura.");
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

    public void StopTunnel()
    {
        var process = _playitProcess ?? FindPlayitProcess();
        if (process is null || process.HasExited)
        {
            WriteLog("O túnel Playit.gg já está desligado.");
            return;
        }

        process.Kill(entireProcessTree: true);
        _forceTunnelVisible = false;
        WriteLog("Túnel Playit.gg encerrado.");
    }

    public ServerSnapshot GetSnapshot(AppSettings settings)
    {
        var server = FindServerProcess(settings);
        var tunnel = FindPlayitProcess();
        var serverRunning = server is { HasExited: false };
        var cpu = 0d;
        var memory = 0d;
        var uptime = TimeSpan.Zero;
        var pid = 0;

        if (serverRunning && server is not null)
        {
            try
            {
                server.Refresh();
                pid = server.Id;
                memory = server.WorkingSet64 / 1024d / 1024d;
                uptime = DateTime.Now - server.StartTime;
                var now = DateTimeOffset.UtcNow;
                var elapsed = now - _lastCpuSample;
                var cpuDelta = server.TotalProcessorTime - _lastCpuTime;
                if (elapsed.TotalMilliseconds > 0 && _lastCpuTime != TimeSpan.Zero)
                {
                    cpu = Math.Clamp(cpuDelta.TotalMilliseconds / (elapsed.TotalMilliseconds * Environment.ProcessorCount) * 100d, 0d, 100d);
                }
                _lastCpuTime = server.TotalProcessorTime;
                _lastCpuSample = now;
            }
            catch
            {
                // O processo pode encerrar entre a descoberta e a leitura das métricas.
            }
        }
        else
        {
            _lastCpuTime = TimeSpan.Zero;
            _lastCpuSample = DateTimeOffset.UtcNow;
        }

        var listening = IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveUdpListeners()
            .Any(endpoint => endpoint.Port == settings.ServerPort);

        return new ServerSnapshot(serverRunning, tunnel is { HasExited: false }, listening, cpu, memory, uptime, pid);
    }

    public void OpenWorldFolder(AppSettings settings) => OpenFolder(settings.WorldPath);

    public static void OpenFolder(string path)
    {
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }

    private void StartTunnelCore(AppSettings settings, bool minimized)
    {
        _playitProcess = FindPlayitProcess();
        if (_playitProcess is { HasExited: false })
        {
            ApplyTunnelWindowPreference(settings);
            WriteLog("O túnel Playit.gg já está ligado.");
            return;
        }

        if (!minimized || settings.ShowPlayitConsole)
        {
            WriteLog("Iniciando o túnel Playit.gg com console visível...");
            _playitProcess = Process.Start(new ProcessStartInfo
            {
                FileName = settings.PlayitPath,
                WorkingDirectory = Path.GetDirectoryName(settings.PlayitPath)!,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            });
            return;
        }

        WriteLog("Iniciando o túnel Playit.gg em segundo plano...");
        var startInfo = new ProcessStartInfo
        {
            FileName = settings.PlayitPath,
            WorkingDirectory = Path.GetDirectoryName(settings.PlayitPath)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        _playitProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _playitProcess.OutputDataReceived += (_, _) => { };
        _playitProcess.ErrorDataReceived += (_, _) => { };
        _playitProcess.Exited += (_, _) => WriteLog("O processo do Playit.gg foi encerrado.");
        if (!_playitProcess.Start()) throw new InvalidOperationException("O agente Playit.gg não pôde ser iniciado.");
        _playitProcess.BeginOutputReadLine();
        _playitProcess.BeginErrorReadLine();
    }

    private Process? FindServerProcess(AppSettings settings)
    {
        if (_serverProcess is { HasExited: false }) return _serverProcess;

        foreach (var process in Process.GetProcessesByName("java"))
        {
            try
            {
                if (string.Equals(process.MainModule?.FileName, settings.JavaPath, StringComparison.OrdinalIgnoreCase))
                {
                    return process;
                }
            }
            catch
            {
                process.Dispose();
            }
        }
        return null;
    }

    private static Process? FindPlayitProcess() =>
        Process.GetProcessesByName("playit-windows-x86_64-signed").FirstOrDefault();

    private static void Validate(AppSettings settings)
    {
        var missing = new List<string>();
        if (!File.Exists(Path.Combine(settings.WorldPath, "config.json"))) missing.Add($"Mundo: {settings.WorldPath}");
        if (!File.Exists(settings.JavaPath)) missing.Add($"Java: {settings.JavaPath}");
        if (!File.Exists(settings.ServerJarPath)) missing.Add($"Servidor: {settings.ServerJarPath}");
        if (!File.Exists(settings.AssetsPath)) missing.Add($"Assets: {settings.AssetsPath}");
        if (!File.Exists(settings.PlayitPath)) missing.Add($"Playit.gg: {settings.PlayitPath}");
        if (settings.InitialMemoryMb < 512 || settings.MaximumMemoryMb < settings.InitialMemoryMb)
            missing.Add("Memória: o máximo deve ser maior ou igual ao inicial, com mínimo de 512 MB.");

        if (missing.Count > 0)
            throw new InvalidOperationException("Verifique a configuração:\n\n" + string.Join("\n", missing));
    }

    private void WriteLog(string message) => LogReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");

    public void Dispose()
    {
        _serverProcess?.Dispose();
        _playitProcess?.Dispose();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr windowHandle, int command);
}
