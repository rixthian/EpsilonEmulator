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
        if (string.IsNullOrWhiteSpace(_options.Path))
        {
            throw new InvalidOperationException("Protocol packet manifest path is not configured.");
        }

        if (!File.Exists(_options.Path))
        {
            throw new FileNotFoundException("Configured protocol packet manifest was not found.", _options.Path);
        }

        string json = File.ReadAllText(_options.Path);
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
}

