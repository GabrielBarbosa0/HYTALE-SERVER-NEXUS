using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Input;
using HytaleServerManager.Infrastructure;
using HytaleServerManager.Models;
using HytaleServerManager.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace HytaleServerManager.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private static readonly Regex JoinPattern = new("Player '([^']+)' joined", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex LeavePattern = new("Player '([^']+)' (left|disconnected)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AuthCodePattern = new(@"(?:Enter code|Code):\s*([A-Z0-9-]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AuthUrlPattern = new(@"https://accounts\.hytale\.com/device(?:\?user_code=[A-Z0-9-]+)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly string PlayitConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "playit_gg", "playit.toml");
    private readonly SettingsService _settingsService;
    private readonly ServerProcessService _processService;
    private readonly WorldDiscoveryService _worldDiscoveryService;
    private readonly DispatcherQueue _dispatcher;
    private readonly HashSet<string> _players = new(StringComparer.OrdinalIgnoreCase);
    private bool _serverRunning;
    private bool _tunnelRunning;
    private bool _portListening;
    private double _cpuPercent;
    private double _memoryMb;
    private string _uptime = "00:00:00";
    private string _serverStatus = "Desligado";
    private string _tunnelStatus = "Desligado";
    private string _notice = "Pronto para iniciar";
    private string _initialMemoryText = "768";
    private string _maximumMemoryText = "4096";
    private WorldOption? _selectedWorld;
    private bool _initialized;
    private bool _hytaleAuthConfirmed;
    private bool _authPageOpened;
    private string _consoleCommandText = "";
    private string _hytaleAuthCode = "Aguardando geração";
    private string _hytaleAuthUrl = "https://accounts.hytale.com/device";
    private string _activeWorldName = "Nenhum";
    private string _worldSelectionHint = "Escolha o mundo que será iniciado.";

    public MainViewModel(SettingsService settingsService, ServerProcessService processService, WorldDiscoveryService worldDiscoveryService, DispatcherQueue dispatcher)
    {
        _settingsService = settingsService;
        _processService = processService;
        _worldDiscoveryService = worldDiscoveryService;
        _dispatcher = dispatcher;
        _processService.LogReceived += OnLogReceived;

        StartCommand = new AsyncRelayCommand(StartAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        StopTunnelCommand = new RelayCommand(StopTunnel);
        CopyAddressCommand = new RelayCommand(CopyAddress);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        OpenWorldCommand = new RelayCommand(() => ExecuteSafely(() => _processService.OpenWorldFolder(Settings)));
        ClearLogsCommand = new RelayCommand(() => Logs.Clear());
        SendConsoleCommand = new AsyncRelayCommand(SendConsoleCommandAsync);
        BeginHytaleAuthCommand = new AsyncRelayCommand(BeginHytaleAuthAsync);
        CheckAuthStatusCommand = new AsyncRelayCommand(() => SendQuickCommandAsync("/auth status", "Consultando autenticação..."));
        CheckAuthPersistenceCommand = new AsyncRelayCommand(() => SendQuickCommandAsync("/auth persistence", "Consultando persistência..."));
        EnableEncryptedAuthCommand = new AsyncRelayCommand(() => SendQuickCommandAsync("/auth persistence Encrypted", "Ativando persistência criptografada..."));
        OpenHytaleAuthPageCommand = new RelayCommand(() => ExecuteSafely(() => ServerProcessService.OpenUrl(HytaleAuthUrl)));
        CopyAuthCodeCommand = new RelayCommand(CopyAuthCode);
        OpenPlayitDownloadCommand = new RelayCommand(() => ExecuteSafely(() => ServerProcessService.OpenUrl("https://playit.gg/download/windows")));
        OpenPlayitDashboardCommand = new RelayCommand(() => ExecuteSafely(() => ServerProcessService.OpenUrl("https://playit.gg/account/agents")));
        StartPlayitSetupCommand = new RelayCommand(StartPlayitSetup);
        ReloadWorldsCommand = new RelayCommand(LoadWorlds);
    }

    public AppSettings Settings { get; private set; } = new();
    public ObservableCollection<string> Logs { get; } = [];
    public ObservableCollection<string> Players { get; } = [];
    public ObservableCollection<WorldOption> Worlds { get; } = [];

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand StopTunnelCommand { get; }
    public ICommand CopyAddressCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand OpenWorldCommand { get; }
    public ICommand ClearLogsCommand { get; }
    public ICommand SendConsoleCommand { get; }
    public ICommand BeginHytaleAuthCommand { get; }
    public ICommand CheckAuthStatusCommand { get; }
    public ICommand CheckAuthPersistenceCommand { get; }
    public ICommand EnableEncryptedAuthCommand { get; }
    public ICommand OpenHytaleAuthPageCommand { get; }
    public ICommand CopyAuthCodeCommand { get; }
    public ICommand OpenPlayitDownloadCommand { get; }
    public ICommand OpenPlayitDashboardCommand { get; }
    public ICommand StartPlayitSetupCommand { get; }
    public ICommand ReloadWorldsCommand { get; }

    public bool ServerRunning { get => _serverRunning; private set => SetProperty(ref _serverRunning, value); }
    public bool TunnelRunning { get => _tunnelRunning; private set => SetProperty(ref _tunnelRunning, value); }
    public bool PortListening { get => _portListening; private set => SetProperty(ref _portListening, value); }
    public double CpuPercent { get => _cpuPercent; private set => SetProperty(ref _cpuPercent, value); }
    public double MemoryMb { get => _memoryMb; private set => SetProperty(ref _memoryMb, value); }
    public string Uptime { get => _uptime; private set => SetProperty(ref _uptime, value); }
    public string ServerStatus { get => _serverStatus; private set => SetProperty(ref _serverStatus, value); }
    public string TunnelStatus { get => _tunnelStatus; private set => SetProperty(ref _tunnelStatus, value); }
    public string Notice { get => _notice; private set => SetProperty(ref _notice, value); }
    public string InitialMemoryText { get => _initialMemoryText; set => SetProperty(ref _initialMemoryText, value); }
    public string MaximumMemoryText { get => _maximumMemoryText; set => SetProperty(ref _maximumMemoryText, value); }
    public string ConsoleCommandText { get => _consoleCommandText; set => SetProperty(ref _consoleCommandText, value); }
    public string HytaleAuthCode { get => _hytaleAuthCode; private set => SetProperty(ref _hytaleAuthCode, value); }
    public string HytaleAuthUrl { get => _hytaleAuthUrl; private set => SetProperty(ref _hytaleAuthUrl, value); }
    public string ActiveWorldName
    {
        get => _activeWorldName;
        private set
        {
            if (SetProperty(ref _activeWorldName, value)) OnPropertyChanged(nameof(ActiveWorldText));
        }
    }
    public string ActiveWorldText => $"Mundo: {ActiveWorldName}";
    public string WorldSelectionHint { get => _worldSelectionHint; private set => SetProperty(ref _worldSelectionHint, value); }
    public WorldOption? SelectedWorld
    {
        get => _selectedWorld;
        set
        {
            if (!SetProperty(ref _selectedWorld, value) || value is null) return;
            Settings.WorldPath = value.Path;
            OnPropertyChanged(nameof(SelectedWorldName));
            OnPropertyChanged(nameof(SelectedWorldPath));
            RefreshSetupStatus();
            Notice = $"Mundo selecionado: {value.Name}";
            if (_initialized) _ = PersistWorldSelectionAsync();
        }
    }
    public string SelectedWorldName => SelectedWorld?.Name ?? "Selecione um mundo";
    public string SelectedWorldPath => SelectedWorld?.Path ?? "Nenhum mundo válido foi encontrado";
    public string WorldCountText => Worlds.Count == 1 ? "1 mundo encontrado" : $"{Worlds.Count} mundos encontrados";
    public bool CanSelectWorld => !ServerRunning;
    public bool CanUseConsole => _processService.CanSendServerCommands;
    public bool CanStartServer => !ServerRunning;
    public string ConsoleAvailabilityText => CanUseConsole
        ? "Console conectado ao servidor atual."
        : ServerRunning
            ? "Servidor iniciado por outra janela. Encerre-o no console original para recuperar os comandos."
            : "Inicie o servidor por este painel para habilitar os comandos.";
    public bool IsHytaleInstalled => File.Exists(Settings.JavaPath) && File.Exists(Settings.ServerJarPath) && File.Exists(Settings.AssetsPath);
    public bool IsPlayitInstalled => File.Exists(Settings.PlayitPath);
    public bool IsPlayitLinked => File.Exists(PlayitConfigPath);
    public bool IsHytaleAuthenticated => _hytaleAuthConfirmed || (SelectedWorld is not null && File.Exists(Path.Combine(SelectedWorld.Path, "auth.enc")));
    public string HytaleInstallStatus => IsHytaleInstalled ? "Hytale pronto" : "Instalação do Hytale não encontrada";
    public string PlayitInstallStatus => IsPlayitInstalled ? "Agente Playit instalado" : "Agente Playit não instalado";
    public string PlayitLinkStatus => IsPlayitLinked ? "Agente vinculado neste computador" : "Vinculação do Playit pendente";
    public string HytaleAuthStatus => IsHytaleAuthenticated ? "Servidor Hytale autorizado" : "Autorização Hytale pendente";
    public Brush HytaleSetupBrush => new SolidColorBrush(IsHytaleInstalled && IsHytaleAuthenticated ? Color.FromArgb(255, 80, 227, 164) : Color.FromArgb(255, 255, 200, 87));
    public Brush PlayitSetupBrush => new SolidColorBrush(IsPlayitInstalled && IsPlayitLinked ? Color.FromArgb(255, 80, 227, 164) : Color.FromArgb(255, 255, 200, 87));
    public string PlayerCountText => _players.Count == 1 ? "1 jogador" : $"{_players.Count} jogadores";
    public string MemoryText => $"{MemoryMb:N0} MB";
    public string CpuText => $"{CpuPercent:N1}%";
    public string PortStatus => PortListening ? $"UDP {Settings.ServerPort} ativa" : $"UDP {Settings.ServerPort} fechada";
    public Brush ServerStatusBrush => new SolidColorBrush(ServerRunning ? Color.FromArgb(255, 80, 227, 164) : Color.FromArgb(255, 255, 107, 122));
    public Brush TunnelStatusBrush => new SolidColorBrush(TunnelRunning ? Color.FromArgb(255, 80, 227, 164) : Color.FromArgb(255, 255, 107, 122));

    public async Task InitializeAsync()
    {
        Settings = await _settingsService.LoadAsync();
        InitialMemoryText = Settings.InitialMemoryMb.ToString();
        MaximumMemoryText = Settings.MaximumMemoryMb.ToString();
        OnPropertyChanged(nameof(Settings));
        LoadWorlds();
        _initialized = true;
        AddLog("Interface iniciada. O .bat continua disponível como fallback.");
        await RefreshAsync();
    }

    public Task RefreshAsync()
    {
        _processService.ApplyTunnelWindowPreference(Settings);
        var snapshot = _processService.GetSnapshot(Settings);
        ServerRunning = snapshot.ServerRunning;
        TunnelRunning = snapshot.TunnelRunning;
        PortListening = snapshot.PortListening;
        CpuPercent = snapshot.CpuPercent;
        MemoryMb = snapshot.MemoryMb;
        Uptime = snapshot.Uptime.Days > 0
            ? snapshot.Uptime.ToString(@"d\.hh\:mm\:ss")
            : snapshot.Uptime.ToString(@"hh\:mm\:ss");
        ServerStatus = ServerRunning ? "Online" : "Desligado";
        TunnelStatus = TunnelRunning ? "Conectado" : "Desligado";
        ActiveWorldName = ServerRunning ? DetectActiveWorldName() : "Nenhum";
        WorldSelectionHint = ServerRunning
            ? $"Em execução: {ActiveWorldName}. Encerre o servidor antes de trocar de mundo."
            : "Escolha o mundo que será iniciado.";
        OnPropertyChanged(nameof(MemoryText));
        OnPropertyChanged(nameof(CpuText));
        OnPropertyChanged(nameof(PortStatus));
        OnPropertyChanged(nameof(ServerStatusBrush));
        OnPropertyChanged(nameof(TunnelStatusBrush));
        OnPropertyChanged(nameof(CanSelectWorld));
        OnPropertyChanged(nameof(CanUseConsole));
        OnPropertyChanged(nameof(CanStartServer));
        OnPropertyChanged(nameof(ConsoleAvailabilityText));
        RefreshSetupStatus();
        return Task.CompletedTask;
    }

    private async Task StartAsync()
    {
        try
        {
            if (SelectedWorld is null)
                throw new InvalidOperationException("Selecione um mundo válido antes de iniciar.");

            await RefreshAsync();
            if (ServerRunning)
            {
                Notice = $"O mundo {ActiveWorldName} já está em execução";
                AddLog($"O servidor já está ativo com o mundo {ActiveWorldName}. Encerre-o antes de iniciar outro.");
                return;
            }

            Notice = "Iniciando serviços...";
            ApplyMemorySettings();
            if (!ServerRunning)
            {
                _players.Clear();
                Players.Clear();
                OnPropertyChanged(nameof(PlayerCountText));
            }
            AddLog($"Iniciando mundo: {SelectedWorld.Name}");
            await _processService.StartAllAsync(Settings);
            Notice = "Inicialização solicitada";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Notice = "Falha ao iniciar";
            AddLog("ERRO: " + ex.Message);
        }
    }

    private async Task StopAsync()
    {
        try
        {
            Notice = "Salvando e encerrando...";
            await _processService.StopServerAsync();
            Notice = "Servidor encerrado";
        }
        catch (Exception ex)
        {
            Notice = "Atenção necessária";
            AddLog("ERRO: " + ex.Message);
        }
        await RefreshAsync();
    }

    private void StopTunnel()
    {
        ExecuteSafely(_processService.StopTunnel);
        _ = RefreshAsync();
    }

    private void CopyAddress()
    {
        var package = new DataPackage();
        package.SetText(Settings.PublicAddress);
        Clipboard.SetContent(package);
        Notice = "Endereço copiado";
        AddLog($"Endereço copiado: {Settings.PublicAddress}");
    }

    private async Task SendConsoleCommandAsync()
    {
        try
        {
            var command = ConsoleCommandText.Trim();
            if (command.Length == 0) return;
            await _processService.SendServerCommandAsync(command);
            ConsoleCommandText = "";
        }
        catch (Exception ex)
        {
            Notice = "Não foi possível enviar o comando";
            AddLog("ERRO: " + ex.Message);
        }
    }

    private async Task BeginHytaleAuthAsync()
    {
        try
        {
            HytaleAuthCode = "Gerando código...";
            HytaleAuthUrl = "https://accounts.hytale.com/device";
            _authPageOpened = false;
            await _processService.SendServerCommandAsync("/auth login device");
            Notice = "Aguardando código de autorização do Hytale";
        }
        catch (Exception ex)
        {
            HytaleAuthCode = "Inicie o servidor primeiro";
            AddLog("ERRO: " + ex.Message);
        }
    }

    private async Task SendQuickCommandAsync(string command, string notice)
    {
        try
        {
            Notice = notice;
            await _processService.SendServerCommandAsync(command);
        }
        catch (Exception ex)
        {
            Notice = "Comando indisponível para este processo";
            AddLog("ERRO: " + ex.Message);
        }
    }

    private void CopyAuthCode()
    {
        if (HytaleAuthCode is "Aguardando geração" or "Gerando código..." or "Inicie o servidor primeiro")
        {
            Notice = "Gere o código antes de copiá-lo";
            return;
        }

        var package = new DataPackage();
        package.SetText(HytaleAuthCode);
        Clipboard.SetContent(package);
        Notice = "Código de autorização copiado";
    }

    private void StartPlayitSetup()
    {
        try
        {
            _processService.StartTunnelSetup(Settings);
            ServerProcessService.OpenUrl("https://playit.gg/account/agents");
            Notice = "Playit aberto para vinculação";
        }
        catch (Exception ex)
        {
            AddLog("ERRO: " + ex.Message);
            ServerProcessService.OpenUrl("https://playit.gg/download/windows");
            Notice = "Baixe o agente oficial do Playit.gg";
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            ApplyMemorySettings();
            await _settingsService.SaveAsync(Settings);
            Notice = "Configurações salvas";
            AddLog("Configurações salvas no perfil local do Windows.");
            OnPropertyChanged(nameof(PortStatus));
        }
        catch (Exception ex)
        {
            AddLog("ERRO: " + ex.Message);
        }
    }

    private void ApplyMemorySettings()
    {
        if (!int.TryParse(InitialMemoryText, out var initial) || !int.TryParse(MaximumMemoryText, out var maximum))
            throw new InvalidOperationException("Use números inteiros para a memória em MB.");
        Settings.InitialMemoryMb = initial;
        Settings.MaximumMemoryMb = maximum;
    }

    private void LoadWorlds()
    {
        Worlds.Clear();
        foreach (var world in _worldDiscoveryService.Discover(Settings.SavesRootPath)) Worlds.Add(world);

        SelectedWorld = Worlds.FirstOrDefault(world =>
            string.Equals(world.Path, Settings.WorldPath, StringComparison.OrdinalIgnoreCase))
            ?? Worlds.FirstOrDefault(world =>
                string.Equals(world.Name, "CAVALEIROS CLT", StringComparison.OrdinalIgnoreCase))
            ?? Worlds.FirstOrDefault();

        OnPropertyChanged(nameof(WorldCountText));
        if (Worlds.Count == 0) AddLog($"Nenhum mundo válido encontrado em {Settings.SavesRootPath}");
        else AddLog($"{WorldCountText}: {string.Join(", ", Worlds.Select(world => world.Name))}.");
        RefreshSetupStatus();
    }

    private string DetectActiveWorldName()
    {
        var lockedWorlds = Worlds
            .Where(world => File.Exists(Path.Combine(world.Path, ".world.lock")))
            .Select(world => world.Name)
            .ToArray();

        return lockedWorlds.Length switch
        {
            0 => SelectedWorldName,
            1 => lockedWorlds[0],
            _ => string.Join(", ", lockedWorlds)
        };
    }

    private async Task PersistWorldSelectionAsync()
    {
        try
        {
            await _settingsService.SaveAsync(Settings);
            AddLog($"Mundo selecionado: {SelectedWorldName}");
        }
        catch (Exception ex)
        {
            AddLog("ERRO ao salvar o mundo selecionado: " + ex.Message);
        }
    }

    private void OnLogReceived(string line) => _dispatcher.TryEnqueue(() => AddLog(line));

    private void AddLog(string line)
    {
        Logs.Add(line);
        while (Logs.Count > 600) Logs.RemoveAt(0);

        var join = JoinPattern.Match(line);
        if (join.Success && _players.Add(join.Groups[1].Value)) Players.Add(join.Groups[1].Value);

        var leave = LeavePattern.Match(line);
        if (leave.Success && _players.Remove(leave.Groups[1].Value)) Players.Remove(leave.Groups[1].Value);

        var authCode = AuthCodePattern.Match(line);
        if (authCode.Success)
        {
            HytaleAuthCode = authCode.Groups[1].Value.ToUpperInvariant();
            HytaleAuthUrl = $"https://accounts.hytale.com/device?user_code={Uri.EscapeDataString(HytaleAuthCode)}";
            OpenAuthPageOnce();
        }

        var authUrl = AuthUrlPattern.Match(line);
        if (authUrl.Success)
        {
            HytaleAuthUrl = authUrl.Value;
            Notice = "Código Hytale pronto para confirmação";
            if (HytaleAuthUrl.Contains("user_code=", StringComparison.OrdinalIgnoreCase)) OpenAuthPageOnce();
        }

        if (line.Contains("Authentication successful", StringComparison.OrdinalIgnoreCase))
        {
            _hytaleAuthConfirmed = true;
            Notice = "Servidor Hytale autorizado";
            RefreshSetupStatus();
        }
        OnPropertyChanged(nameof(PlayerCountText));
    }

    private void OpenAuthPageOnce()
    {
        if (_authPageOpened) return;
        try
        {
            ServerProcessService.OpenUrl(HytaleAuthUrl);
            _authPageOpened = true;
        }
        catch (Exception ex)
        {
            Notice = "Copie o código e abra a confirmação manualmente";
            Logs.Add($"[{DateTime.Now:HH:mm:ss}] ERRO ao abrir a confirmação: {ex.Message}");
        }
    }

    private void RefreshSetupStatus()
    {
        OnPropertyChanged(nameof(IsHytaleInstalled));
        OnPropertyChanged(nameof(IsPlayitInstalled));
        OnPropertyChanged(nameof(IsPlayitLinked));
        OnPropertyChanged(nameof(IsHytaleAuthenticated));
        OnPropertyChanged(nameof(HytaleInstallStatus));
        OnPropertyChanged(nameof(PlayitInstallStatus));
        OnPropertyChanged(nameof(PlayitLinkStatus));
        OnPropertyChanged(nameof(HytaleAuthStatus));
        OnPropertyChanged(nameof(HytaleSetupBrush));
        OnPropertyChanged(nameof(PlayitSetupBrush));
    }

    private void ExecuteSafely(Action action)
    {
        try { action(); }
        catch (Exception ex) { AddLog("ERRO: " + ex.Message); }
    }

    public void Dispose()
    {
        _processService.LogReceived -= OnLogReceived;
        _processService.Dispose();
    }
}
