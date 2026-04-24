using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class StudioServiceTests
{
    [Fact]
    public async Task Snapshot_ReturnsSeededEventAndPackages()
    {
        ServiceProvider services = BuildServices();
        IStudioService studioService = services.GetRequiredService<IStudioService>();

        StudioEventSnapshot? snapshot = await studioService.GetSnapshotAsync(
            new CharacterId(1),
            TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.True(snapshot!.Definition.IsActive);
        Assert.NotEmpty(snapshot.AssetPackages);
        Assert.Contains(snapshot.AssetPackages, candidate => candidate.PackageKey == "studio-actors-classic");
    }

    [Fact]
    public async Task Submit_CreatesPendingSubmission()
    {
        ServiceProvider services = BuildServices();
        IStudioService studioService = services.GetRequiredService<IStudioService>();

        SubmitStudioMovieResult result = await studioService.SubmitAsync(
            new SubmitStudioMovieRequest(
                new CharacterId(3),
                "Neon Rooftop",
                "A rooftop chase scene shot at sunset.",
                "<movie><scene set=\"rooftop\" /></movie>",
                "@nova",
                "en"),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(StudioSubmissionStatus.Pending, result.Snapshot!.Submission.Status);
        Assert.Equal("neon-rooftop", result.Snapshot.Submission.Slug);
    }

    [Fact]
    public async Task Publish_MakesSubmissionVisibleOnLeaderboard()
    {
        ServiceProvider services = BuildServices();
        IStudioService studioService = services.GetRequiredService<IStudioService>();

        SubmitStudioMovieResult created = await studioService.SubmitAsync(
            new SubmitStudioMovieRequest(
                new CharacterId(3),
                "Moonlight Signal",
                "A mystery short filmed in the public studio.",
                "<movie><scene set=\"studio\" /></movie>",
                "@nova",
                "en"),
            TestContext.Current.CancellationToken);

        StudioSubmissionSnapshot? published = await studioService.PublishAsync(
            created.Snapshot!.Submission.Slug,
            new CharacterId(5),
            "Approved for broadcast.",
            TestContext.Current.CancellationToken);
        IReadOnlyList<StudioLeaderboardEntry> leaderboard =
            await studioService.GetLeaderboardAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(published);
        Assert.Equal(StudioSubmissionStatus.Published, published!.Submission.Status);
        Assert.Contains(leaderboard, candidate => candidate.Slug == "moonlight-signal");
    }

    [Fact]
    public async Task Vote_EnforcesHourlyLimit()
    {
        ServiceProvider services = BuildServices();
        IStudioService studioService = services.GetRequiredService<IStudioService>();

        VoteStudioMovieResult first = await studioService.VoteAsync(
            new VoteStudioMovieRequest(new CharacterId(3), "pixels-of-justice", 1, "127.0.0.1"),
            TestContext.Current.CancellationToken);
        VoteStudioMovieResult second = await studioService.VoteAsync(
            new VoteStudioMovieRequest(new CharacterId(3), "pixels-of-justice", 1, "127.0.0.1"),
            TestContext.Current.CancellationToken);
        VoteStudioMovieResult third = await studioService.VoteAsync(
            new VoteStudioMovieRequest(new CharacterId(3), "ducks-on-the-boulevard", 1, "127.0.0.1"),
            TestContext.Current.CancellationToken);
        VoteStudioMovieResult fourth = await studioService.VoteAsync(
            new VoteStudioMovieRequest(new CharacterId(3), "pixels-of-justice", 1, "127.0.0.1"),
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
        IStudioService studioService = services.GetRequiredService<IStudioService>();

        StudioEventDefinition? definition = await studioService.SetActivationAsync(
            false,
            new CharacterId(6),
            TestContext.Current.CancellationToken);

        SubmitStudioMovieResult submit = await studioService.SubmitAsync(
            new SubmitStudioMovieRequest(
                new CharacterId(3),
                "Disabled Event Test",
                "Should not enter queue while inactive.",
                "<movie />",
                null,
                "en"),
            TestContext.Current.CancellationToken);
        VoteStudioMovieResult vote = await studioService.VoteAsync(
            new VoteStudioMovieRequest(new CharacterId(3), "pixels-of-justice", 1, "127.0.0.1"),
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
