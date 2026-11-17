using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.AI.MonsterPlants;

/// <summary>
/// A monster plant never walks, talks or greets. It shows its growth as a room posture and tells the room when it
/// reaches a new level or when its breeding or revival state changes.
/// </summary>
public sealed class MonsterPlantBot(TimeProvider clock) : BotAi
{
    private int _level;
    private (bool CanBreed, bool CanRevive)? _state;

    public override void OnSelfEnterRoom() => Refresh(false);
    public override void OnSelfLeaveRoom(bool kicked) { }
    public override void OnUserEnterRoom(RoomUser user) { }
    public override void OnUserLeaveRoom(GameClient client) { }
    public override void OnUserSay(RoomUser user, string message) { }
    public override void OnUserShout(RoomUser user, string message) { }
    public override void OnTimerTick() => Refresh(true);

    private void Refresh(bool announce) => throw new NotImplementedException();
}
