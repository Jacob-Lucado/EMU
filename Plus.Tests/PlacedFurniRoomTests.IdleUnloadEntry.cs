using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Polls;
using Plus.HabboHotel.Users;
using Xunit;

namespace Plus.Tests;

// An empty room unloads after it has been idle for a while. A user who enters it at that moment must land in the room, never in a closed one.
public partial class PlacedFurniRoomTests
{
    [Fact]
    public void AnEmptyRoomAtItsIdleLimitStaysOpenForAUserWhoIsEnteringIt()
    {
        var unloads = 0;
        Set("_rooms", TestRoomOwners.Create(_ => unloads++));
        _room.IdleTime = 1000;
        var habbo = Entrant(new TickBeforeEntry(_room, Entry()));

        Assert.True(habbo.EnterRoom(_room));

        Assert.Equal(0, unloads);
        Assert.Same(_room, habbo.CurrentRoom);
        Assert.NotNull(_room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id));
    }

    [Fact]
    public void EnteringARoomThatAlreadyClosedLeavesTheUserOutsideIt()
    {
        var habbo = Entrant(Entry());
        // What an unload leaves behind.
        _room.MDisposed = true;
        Set("_roomUserManager", null!);

        Assert.False(habbo.EnterRoom(_room));

        Assert.Null(habbo.CurrentRoom);
        Assert.False(habbo.InRoom);
    }

    private Habbo Entrant(IRoomEntryService entry)
    {
        var habbo = _client.GetHabbo();
        habbo.CurrentRoom = null;
        habbo.Client = _client;
        habbo.SetHotelServices(TestGameClientManager.Empty, TestRoomOwners.Unused, roomEntry: entry);
        habbo.SetRoomVisitRecorder(Proxy<IRoomVisitRecorder>((_, _) => null), null!);

        return habbo;
    }

    private static RoomEntryService Entry() =>
        new(null!, TimeProvider.System, Proxy<IRoomPollService>((_, _) => null), EntryWordQuiz);

    // The room's own tick runs on another thread; here it lands between the start of the entry and the avatar joining the room.
    private sealed class TickBeforeEntry(Room room, IRoomEntryService entry) : IRoomEntryService
    {
        public void Enter(GameClient session)
        {
            room.ProcessRoom();
            entry.Enter(session);
        }
    }
}
