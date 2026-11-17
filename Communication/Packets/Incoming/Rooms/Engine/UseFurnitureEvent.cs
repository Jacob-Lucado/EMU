using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI.MonsterPlants;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal sealed class UseFurnitureEvent(IFurnitureUseService furniture, IMonsterPlantSeedService? seeds = null) : RoomPacketEvent
{
    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        furniture.Use(room, session, new(BuildersClubItemIds.FromWire(packet.ReadUInt()), packet.ReadInt()));

        return Task.CompletedTask;
    }
}
