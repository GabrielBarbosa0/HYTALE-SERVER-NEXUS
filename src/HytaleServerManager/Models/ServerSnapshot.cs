namespace HytaleServerManager.Models;

public sealed record ServerSnapshot(
    bool ServerRunning,
    bool TunnelRunning,
    bool PortListening,
    double CpuPercent,
    double MemoryMb,
    TimeSpan Uptime,
    int ServerProcessId);
