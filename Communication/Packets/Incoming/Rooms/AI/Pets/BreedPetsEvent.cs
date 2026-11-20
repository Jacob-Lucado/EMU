using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI.MonsterPlants;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Pets;

internal class BreedPetsEvent(IMonsterPlantBreedingService breeding) : RoomPacketEvent
{
    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        breeding.Breed(room, session, packet.ReadInt(), packet.ReadInt(), packet.ReadInt());

        return Task.CompletedTask;
    }
}
