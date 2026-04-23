using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Epsilon.Protocol;

/// <summary>
/// Loads protocol command manifests from configured JSON files.
/// </summary>
public sealed class ProtocolCommandManifestLoader
{
    private readonly PacketManifestOptions _options;

    /// <summary>
    /// Creates a protocol command manifest loader.
    /// </summary>
    public ProtocolCommandManifestLoader(IOptions<PacketManifestOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Loads the configured protocol command manifest.
    /// </summary>
    public ProtocolCommandManifest Load()
    {
        if (string.IsNullOrWhiteSpace(_options.CommandManifestPath))
        {
            throw new InvalidOperationException("Protocol command manifest path is not configured.");
        }

        string resolvedPath = PacketManifestLoader.ResolvePath(_options.CommandManifestPath);

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException("Configured protocol command manifest was not found.", resolvedPath);
        }

        string json = File.ReadAllText(resolvedPath);
        ProtocolCommandManifest? manifest = JsonSerializer.Deserialize<ProtocolCommandManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (manifest is null)
        {
            throw new InvalidOperationException("Protocol command manifest could not be deserialized.");
        }

        return manifest;
    }
}
