using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class ChatCommandRepositoryTests
{
    [Fact]
    public async Task RegularUser_GetsOnlyPlayerCommands()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IChatCommandRepository repository = services.GetRequiredService<IChatCommandRepository>();

        IReadOnlyList<ChatCommandDefinition> commands =
            await repository.GetAvailableByCharacterIdAsync(new CharacterId(7), cancellationToken);

        Assert.Contains(commands, command => command.CommandKey == "help");
        Assert.Contains(commands, command => command.CommandKey == "lang");
        Assert.Contains(commands, command => command.CommandKey == "wave");
        Assert.DoesNotContain(commands, command => command.CommandKey == "roommute");
        Assert.DoesNotContain(commands, command => command.CommandKey == "ha");
        Assert.DoesNotContain(commands, command => command.CommandKey == "rareweek");
    }

    [Fact]
    public async Task ModeratorRank_GetsHotelModeratorCommandsButNotAdminCommands()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IChatCommandRepository repository = services.GetRequiredService<IChatCommandRepository>();

        IReadOnlyList<ChatCommandDefinition> commands =
            await repository.GetAvailableByCharacterIdAsync(new CharacterId(4), cancellationToken);

        Assert.Contains(commands, command => command.CommandKey == "roommute");
        Assert.Contains(commands, command => command.CommandKey == "roomalert");
        Assert.Contains(commands, command => command.CommandKey == "ha");
        Assert.DoesNotContain(commands, command => command.CommandKey == "rareweek");
        Assert.DoesNotContain(commands, command => command.CommandKey == "bbprepare");
    }

    [Fact]
    public async Task AdministratorRank_GetsAdministrativeGameAndCatalogCommands()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IChatCommandRepository repository = services.GetRequiredService<IChatCommandRepository>();

        IReadOnlyList<ChatCommandDefinition> commands =
            await repository.GetAvailableByCharacterIdAsync(new CharacterId(5), cancellationToken);

        Assert.Contains(commands, command => command.CommandKey == "rareweek");
        Assert.Contains(commands, command => command.CommandKey == "gamesessions");
        Assert.Contains(commands, command => command.CommandKey == "bbprepare");
        Assert.Contains(commands, command => command.CommandKey == "bbfinish");
    }

    private static ServiceProvider BuildServices()
    {
        ConfigurationManager configuration = new();
        configuration["Infrastructure:Provider"] = "InMemory";
        configuration["Infrastructure:RedisConnectionString"] = "localhost:6379";

        ServiceCollection services = new();
        services.AddPersistenceRuntime(configuration);
        services.AddCoreGameRuntime();
        return services.BuildServiceProvider();
    }
}
