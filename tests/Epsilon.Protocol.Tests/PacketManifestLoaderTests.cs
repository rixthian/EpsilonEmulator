using Epsilon.Protocol;
using Microsoft.Extensions.Options;
using Xunit;

namespace Epsilon.Protocol.Tests;

public sealed class PacketManifestLoaderTests
{
    [Fact]
    public void Loader_reads_shipped_release63_packet_manifest()
    {
        PacketManifestOptions options = new()
        {
            Family = "RELEASE63",
            ManifestPath = Path.Combine("packet-manifests", "release63.json"),
            CommandManifestPath = Path.Combine("command-manifests", "release63.commands.json")
        };

        PacketManifestLoader loader = new(Options.Create(options));

        PacketManifest manifest = loader.Load();

        Assert.False(string.IsNullOrWhiteSpace(manifest.Family));
        Assert.NotNull(manifest.Packets);
    }

    [Fact]
    public void Registry_rejects_missing_manifest_path()
    {
        PacketManifestOptions options = new()
        {
            Family = "RELEASE63",
            ManifestPath = string.Empty,
            CommandManifestPath = string.Empty
        };

        PacketManifestLoader loader = new(Options.Create(options));

        Assert.Throws<InvalidOperationException>(() => loader.Load());
    }
}
