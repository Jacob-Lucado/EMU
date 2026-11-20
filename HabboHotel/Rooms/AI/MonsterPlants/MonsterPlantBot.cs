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

    /// <summary>Tells the room at once when the plant's level or breeding or revival state changed.</summary>
    public void Announce() { }

    private void Refresh(bool announce)
    {
        if (GetRoomUser() is not { PetData: { Plant: { } plant } pet } user) {
            return;
        }

        var now = clock.GetUtcNow();
        var posture = plant.Posture(now);

        if (!user.Statusses.ContainsKey(posture)) {
            user.Statusses.Clear();
            user.Statusses[posture] = "";
            user.UpdateNeeded = true;
        }

        var level = plant.Level(now);
        var state = (plant.CanBreedNow(now), plant.IsDead(now));
        var room = GetRoom();

        if (announce && room != null) {
            if (level != _level) {
                room.SendPacket(new PetLevelUpdateComposer(user.VirtualId, pet.PetId, level));
            }

            if (_state != state) {
                room.SendPacket(new PetStatusUpdateComposer(user.VirtualId, pet.PetId, state.Item1, false, state.Item2, plant.BreedingPermission));
            }
        }

        _level = level;
        _state = state;
    }
}
