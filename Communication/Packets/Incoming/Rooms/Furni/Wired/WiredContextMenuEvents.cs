using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni.Wired;

/// <summary>GetWiredContextMenu: int target type, int target id. Always answered, with no items when nothing applies.</summary>
internal sealed class GetWiredContextMenuEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet) => throw new NotImplementedException();
}

/// <summary>WiredContextMenuClick: int target type, int target id, string button id.</summary>
internal sealed class WiredContextMenuClickEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet) => throw new NotImplementedException();
}
