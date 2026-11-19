using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Incoming.Inventory.Furni;

internal sealed class RequestFurniInventoryEvent(IGroupManager groups) : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        session.GetHabbo().Inventory?.Furniture.SendInventory(session);

        return Task.CompletedTask;
    }
}
