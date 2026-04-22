using Epsilon.Persistence;
using Epsilon.Games;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class GroupServiceTests
{
    [Fact]
    public async Task List_ReturnsSeededGroupsForViewer()
    {
        ServiceProvider services = BuildServices();
        IGroupService groupService = services.GetRequiredService<IGroupService>();

        IReadOnlyList<HotelGroupSummary> groups =
            await groupService.ListAsync(new CharacterId(1), TestContext.Current.CancellationToken);

        Assert.NotEmpty(groups);
        Assert.Contains(groups, candidate => candidate.Group.Name == "Room Builders");
    }

    [Fact]
    public async Task Create_CreatesOwnerMembership()
    {
        ServiceProvider services = BuildServices();
        IGroupService groupService = services.GetRequiredService<IGroupService>();

        CreateGroupResult result = await groupService.CreateAsync(
            new CreateGroupRequest(
                new CharacterId(1),
                "Test Architects",
                "Internal build crew",
                "TESTGRP",
                new RoomId(1),
                GroupJoinMode.Open),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(GroupMemberRole.Owner, result.Snapshot!.ViewerRole);
        Assert.Equal(1, result.Snapshot.MemberCount);
    }

    [Fact]
    public async Task Join_AddsMemberToOpenGroup()
    {
        ServiceProvider services = BuildServices();
        IGroupService groupService = services.GetRequiredService<IGroupService>();

        HotelGroupSnapshot? snapshot = await groupService.JoinAsync(
            new GroupId(1),
            new CharacterId(3),
            TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.True(snapshot!.IsJoined);
        Assert.Contains(snapshot.Members, candidate => candidate.CharacterId == new CharacterId(3));
    }

    [Fact]
    public async Task Leave_RemovesNonOwnerMembership()
    {
        ServiceProvider services = BuildServices();
        IGroupService groupService = services.GetRequiredService<IGroupService>();

        bool left = await groupService.LeaveAsync(
            new GroupId(1),
            new CharacterId(7),
            TestContext.Current.CancellationToken);
        HotelGroupSnapshot? snapshot = await groupService.GetAsync(
            new GroupId(1),
            new CharacterId(1),
            TestContext.Current.CancellationToken);

        Assert.True(left);
        Assert.NotNull(snapshot);
        Assert.DoesNotContain(snapshot!.Members, candidate => candidate.CharacterId == new CharacterId(7));
    }

    [Fact]
    public async Task SessionSnapshot_IncludesGroups()
    {
        ServiceProvider services = BuildServices();
        IHotelSessionSnapshotService sessionSnapshotService =
            services.GetRequiredService<IHotelSessionSnapshotService>();

        HotelSessionSnapshot? snapshot = await sessionSnapshotService.BuildAsync(
            new CharacterId(1),
            TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.NotEmpty(snapshot!.Groups);
        Assert.Contains(snapshot.Groups, candidate => candidate.Group.Name == "Room Builders");
    }

    [Fact]
    public async Task SetLinkedRoom_RequiresOwnedPrivateRoom()
    {
        ServiceProvider services = BuildServices();
        IGroupService groupService = services.GetRequiredService<IGroupService>();

        HotelGroupSnapshot? snapshot = await groupService.SetLinkedRoomAsync(
            new GroupId(1),
            new CharacterId(1),
            new RoomId(20),
            TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.Equal(new RoomId(20), snapshot!.Group.RoomId);
    }

    [Fact]
    public async Task SetLinkedRoom_RejectsPublicRoom()
    {
        ServiceProvider services = BuildServices();
        IGroupService groupService = services.GetRequiredService<IGroupService>();

        HotelGroupSnapshot? snapshot = await groupService.SetLinkedRoomAsync(
            new GroupId(1),
            new CharacterId(1),
            new RoomId(1),
            TestContext.Current.CancellationToken);

        Assert.Null(snapshot);
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
