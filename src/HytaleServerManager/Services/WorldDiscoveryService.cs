using HytaleServerManager.Models;

namespace HytaleServerManager.Services;

public sealed class WorldDiscoveryService
{
    public IReadOnlyList<WorldOption> Discover(string savesRootPath)
    {
        if (!Directory.Exists(savesRootPath)) return [];

        return Directory.EnumerateDirectories(savesRootPath)
            .Where(path => File.Exists(System.IO.Path.Combine(path, "config.json")))
            .Select(path => new WorldOption(System.IO.Path.GetFileName(path), path))
            .OrderBy(world => world.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}
