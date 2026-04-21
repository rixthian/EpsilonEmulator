using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Epsilon.Protocol;

public sealed class PacketManifestLoader
{
    private readonly PacketManifestOptions _options;

    public PacketManifestLoader(IOptions<PacketManifestOptions> options)
    {
        _options = options.Value;
    }

    public PacketManifest Load()
    {
        if (string.IsNullOrWhiteSpace(_options.ManifestPath))
        {
            throw new InvalidOperationException("Protocol packet manifest path is not configured.");
        }

        string resolvedPath = ResolvePath(_options.ManifestPath);

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException("Configured protocol packet manifest was not found.", resolvedPath);
        }

        string json = File.ReadAllText(resolvedPath);
        PacketManifest? manifest = JsonSerializer.Deserialize<PacketManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (manifest is null)
        {
            throw new InvalidOperationException("Protocol packet manifest could not be deserialized.");
        }

        return manifest;
    }

    internal static string ResolvePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
    }
}
