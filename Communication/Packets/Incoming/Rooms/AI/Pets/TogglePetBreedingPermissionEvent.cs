using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets;

internal class TogglePetBreedingPermissionEvent(IPetBreedingPermissionService breeding) : RoomPacketEvent
{
    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        breeding.Toggle(room, session, packet.ReadInt());

        return Task.CompletedTask;
    }
}
