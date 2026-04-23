using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class HabbowoodServiceTests
{
    [Fact]
    public async Task Snapshot_ReturnsSeededEventAndPackages()
    {
        ServiceProvider services = BuildServices();
        IHabbowoodService habbowoodService = services.GetRequiredService<IHabbowoodService>();

        HabbowoodEventSnapshot? snapshot = await habbowoodService.GetSnapshotAsync(
            new CharacterId(1),
            TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.True(snapshot!.Definition.IsActive);
        Assert.NotEmpty(snapshot.AssetPackages);
        Assert.Contains(snapshot.AssetPackages, candidate => candidate.PackageKey == "habbowood-actors-classic");
    }

    [Fact]
    public async Task Submit_CreatesPendingSubmission()
    {
        ServiceProvider services = BuildServices();
        IHabbowoodService habbowoodService = services.GetRequiredService<IHabbowoodService>();

        SubmitHabbowoodMovieResult result = await habbowoodService.SubmitAsync(
            new SubmitHabbowoodMovieRequest(
                new CharacterId(3),
                "Neon Rooftop",
                "A rooftop chase scene shot at sunset.",
                "<movie><scene set=\"rooftop\" /></movie>",
                "@nova",
                "en"),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(HabbowoodSubmissionStatus.Pending, result.Snapshot!.Submission.Status);
        Assert.Equal("neon-rooftop", result.Snapshot.Submission.Slug);
    }

    [Fact]
    public async Task Publish_MakesSubmissionVisibleOnLeaderboard()
    {
        ServiceProvider services = BuildServices();
        IHabbowoodService habbowoodService = services.GetRequiredService<IHabbowoodService>();

        SubmitHabbowoodMovieResult created = await habbowoodService.SubmitAsync(
            new SubmitHabbowoodMovieRequest(
                new CharacterId(3),
                "Moonlight Signal",
                "A mystery short filmed in the public studio.",
                "<movie><scene set=\"studio\" /></movie>",
                "@nova",
                "en"),
            TestContext.Current.CancellationToken);

        HabbowoodSubmissionSnapshot? published = await habbowoodService.PublishAsync(
            created.Snapshot!.Submission.Slug,
            new CharacterId(5),
            "Approved for broadcast.",
            TestContext.Current.CancellationToken);
        IReadOnlyList<HabbowoodLeaderboardEntry> leaderboard =
            await habbowoodService.GetLeaderboardAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(published);
        Assert.Equal(HabbowoodSubmissionStatus.Published, published!.Submission.Status);
        Assert.Contains(leaderboard, candidate => candidate.Slug == "moonlight-signal");
    }

    [Fact]
    public async Task Vote_EnforcesHourlyLimit()
    {
        ServiceProvider services = BuildServices();
        IHabbowoodService habbowoodService = services.GetRequiredService<IHabbowoodService>();

        VoteHabbowoodMovieResult first = await habbowoodService.VoteAsync(
            new VoteHabbowoodMovieRequest(new CharacterId(3), "pixels-of-justice", 1, "127.0.0.1"),
            TestContext.Current.CancellationToken);
        VoteHabbowoodMovieResult second = await habbowoodService.VoteAsync(
            new VoteHabbowoodMovieRequest(new CharacterId(3), "pixels-of-justice", 1, "127.0.0.1"),
            TestContext.Current.CancellationToken);
        VoteHabbowoodMovieResult third = await habbowoodService.VoteAsync(
            new VoteHabbowoodMovieRequest(new CharacterId(3), "ducks-on-the-boulevard", 1, "127.0.0.1"),
            TestContext.Current.CancellationToken);
        VoteHabbowoodMovieResult fourth = await habbowoodService.VoteAsync(
            new VoteHabbowoodMovieRequest(new CharacterId(3), "pixels-of-justice", 1, "127.0.0.1"),
            TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.True(third.Succeeded);
        Assert.False(fourth.Succeeded);
        Assert.Equal("vote_limit_reached", fourth.FailureCode);
    }

    [Fact]
    public async Task Activation_DisablesSubmissionsAndVotes()
    {
        ServiceProvider services = BuildServices();
        IHabbowoodService habbowoodService = services.GetRequiredService<IHabbowoodService>();

        HabbowoodEventDefinition? definition = await habbowoodService.SetActivationAsync(
            false,
            new CharacterId(6),
            TestContext.Current.CancellationToken);

        SubmitHabbowoodMovieResult submit = await habbowoodService.SubmitAsync(
            new SubmitHabbowoodMovieRequest(
                new CharacterId(3),
                "Disabled Event Test",
                "Should not enter queue while inactive.",
                "<movie />",
                null,
                "en"),
            TestContext.Current.CancellationToken);
        VoteHabbowoodMovieResult vote = await habbowoodService.VoteAsync(
            new VoteHabbowoodMovieRequest(new CharacterId(3), "pixels-of-justice", 1, "127.0.0.1"),
            TestContext.Current.CancellationToken);

        Assert.NotNull(definition);
        Assert.False(definition!.IsActive);
        Assert.False(submit.Succeeded);
        Assert.Equal("event_inactive", submit.FailureCode);
        Assert.False(vote.Succeeded);
        Assert.Equal("event_inactive", vote.FailureCode);
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
