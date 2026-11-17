using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Permissions;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Xunit;

namespace Plus.Tests;

// A group member who may decorate is controller level 2. Kick, ban, mute, taking other users' furni and Builders Club
// furni stay with rights holders and group admins (level 1 and level 3 or more), so a plain member never gets them.
public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void PlainGroupMemberCannotKickInTheGroupRoom(int adminsOnly)
    {
        var target = ModerationTarget();
        var achievements = new List<string>();
        var service = ModerationService(target, achievements.Add);
        PlainMemberOfTheRoomGroup(adminsOnly);

        service.Kick(_client, 8);

        Assert.Same(_room, target.GetHabbo().CurrentRoom);
        Assert.NotNull(_room.GetRoomUserManager().GetRoomUserByHabbo(8));
        Assert.Empty(achievements);
    }

    [Fact]
    public void PlainGroupMemberCannotBanInTheGroupRoom()
    {
        var target = ModerationTarget();
        var achievements = new List<string>();
        var service = ModerationService(target, achievements.Add);
        PlainMemberOfTheRoomGroup(0);
        _room.SetBans(new BansComponent(_room, new RecordingModerationBans(), TimeProvider.System, []));

        service.Ban(_client, new RoomBanRequest(8, (int)RoomId, "hour"));

        Assert.Equal(0, _room.GetBans().Count);
        Assert.Empty(achievements);
    }

    [Fact]
    public void GroupAdminStillKicksInTheGroupRoom()
    {
        var target = ModerationTarget();
        var achievements = new List<string>();
        var service = ModerationService(target, achievements.Add);
        PlainMemberOfTheRoomGroup(0);
        _room.Group!.MakeAdmin(7);

        service.Kick(_client, 8);

        Assert.Null(_room.GetRoomUserManager().GetRoomUserByHabbo(8));
        Assert.Equal(["ACH_SelfModKickSeen"], achievements);
    }

    [Fact]
    public void GroupAdminStillBansInTheGroupRoom()
    {
        var target = ModerationTarget();
        var achievements = new List<string>();
        var service = ModerationService(target, achievements.Add);
        PlainMemberOfTheRoomGroup(0);
        _room.Group!.MakeAdmin(7);
        _room.SetBans(new BansComponent(_room, new RecordingModerationBans(), TimeProvider.System, []));

        service.Ban(_client, new RoomBanRequest(8, (int)RoomId, "hour"));

        Assert.Equal(1, _room.GetBans().Count);
        Assert.Equal(["ACH_SelfModBanSeen"], achievements);
    }

    [Fact]
    public async Task PlainGroupMemberCannotTakeAnotherUsersFurniInTheGroupRoom()
    {
        Viewer();
        var item = Add(30, 1, 1);
        item.OwnerId = 99;
        item.UserId = 99;
        PlainMemberOfTheRoomGroup(0);
        var store = new PickupStore(_ => true);

        await new RoomItemPickupService(Proxy<IGameClientManager>((_, _) => null), Proxy<IQuestManager>((_, _) => null), store)
            .PickUp(_client, item.Id);

        Assert.Same(item, _room.GetRoomItemHandler().GetItem(item.Id));
        Assert.Empty(store.Calls);
    }

    [Fact]
    public void PlainGroupMemberCannotPlaceBuildersClubFurniInTheGroupRoom()
    {
        Viewer();
        PlainMemberOfTheRoomGroup(0);
        var (service, store, _) = BuildersClub();

        service.PlaceFloorItem(_room, _client, new(BuildersClubPageId, BuildersClubOfferId, ""), 1, 1, 0, confirmed: true);

        Assert.Empty(store.Created);
        Assert.Empty(_room.GetRoomItemHandler().GetWallAndFloor);
    }

    [Fact]
    public void RightsHolderCannotPlaceBuildersClubFurniInTheGroupRoom()
    {
        Viewer();
        PlainMemberOfTheRoomGroup(1);
        _room.Group!.DeleteMember(7);
        _room.UsersWithRights.Add(7);
        var (service, store, _) = BuildersClub();

        service.PlaceFloorItem(_room, _client, new(BuildersClubPageId, BuildersClubOfferId, ""), 1, 1, 0, confirmed: true);

        Assert.Empty(store.Created);
    }

    [Fact]
    public void GroupAdminPlacesBuildersClubFurniInTheGroupRoom()
    {
        Viewer();
        PlainMemberOfTheRoomGroup(0);
        _room.Group!.MakeAdmin(7);
        var (service, store, _) = BuildersClub();

        service.PlaceFloorItem(_room, _client, new(BuildersClubPageId, BuildersClubOfferId, ""), 1, 1, 0, confirmed: true);

        Assert.Single(store.Created);
    }

    [Fact]
    public void PlainGroupMemberCannotTakeAnotherUsersBuildersClubFurniInTheGroupRoom()
    {
        Viewer();
        var (service, _, _) = BuildersClub();
        var item = new Item { Id = 31, IsBuildersClub = true, OwnerId = 99, UserId = 99, RoomId = RoomId, Definition = Furni(31, InteractionType.None, WiredBoxType.None).Definition };
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null!, item, 1, 1, 0, true, false, false));
        PlainMemberOfTheRoomGroup(0);

        Assert.False(service.TryPickUp(_client, _room, item));
        Assert.Same(item, _room.GetRoomItemHandler().GetItem(item.Id));
    }

    [Fact]
    public async Task PlainGroupMemberCannotSwitchDiagonalWalkingInTheGroupRoom()
    {
        EnterAsOwner();
        _room.UsersWithRights = [];
        var commands = DisableDiagonalCommands([]);
        _client.GetHabbo().Access = UserAccess.Create([], [new(PermissionKeys.CommandDisablediagonal, false)]);
        PlainMemberOfTheRoomGroup(0);

        Assert.True(await commands.Parse(_client, ":disablediagonal"));

        Assert.False(_room.DiagonalSqueeze);
        Assert.Equal("Oops, you need rights in this room to run this command!", LastWhisper());
    }

    // The test client (id 7) stops owning the room and joins its group as a plain member.
    private void PlainMemberOfTheRoomGroup(int adminsOnly)
    {
        _room.OwnerId = 99;
        _room.OwnerName = "somebody else";
        _room.UsersWithRights ??= [];
        var group = new Group(20, "group", "", "", RoomId, 99, null, 0, 1, 1, 0, false, new GroupMembershipSnapshot([], [], []));
        group.PublishJoin(7);
        group.AdminOnlyDeco = adminsOnly;
        _room.Group = group;
    }
}
