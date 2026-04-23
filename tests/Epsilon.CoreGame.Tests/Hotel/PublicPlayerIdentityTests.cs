using Epsilon.Persistence;
using Epsilon.Games;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class PublicPlayerIdentityTests
{
    [Fact]
    public async Task Create_AssignsStablePublicId()
    {
        ServiceProvider services = BuildServices();
        ICharacterProfileRepository repository = services.GetRequiredService<ICharacterProfileRepository>();

        CharacterProfile profile = await repository.CreateAsync(
            new AccountId(999),
            "mobilepilot",
            new RoomId(1),
            TestContext.Current.CancellationToken);

        Assert.StartsWith("usr_", profile.PublicId);
        Assert.NotEmpty(profile.PublicId);
    }

    [Fact]
    public async Task SeededProfile_IsResolvableByPublicId()
    {
        ServiceProvider services = BuildServices();
        ICharacterProfileRepository repository = services.GetRequiredService<ICharacterProfileRepository>();

        CharacterProfile? profile = await repository.GetByPublicIdAsync(
            "usr_epsilon",
            TestContext.Current.CancellationToken);

        Assert.NotNull(profile);
        Assert.Equal("epsilon", profile!.Username);
    }

    private static ServiceProvider BuildServices()
    {
        ConfigurationManager configuration = new();
        configuration["Infrastructure:Provider"] = "InMemory";

        ServiceCollection services = new();
        services.AddPersistenceRuntime(configuration);
        services.AddGameRuntime();
        services.AddCoreGameRuntime();
        return services.BuildServiceProvider();
    }
}
