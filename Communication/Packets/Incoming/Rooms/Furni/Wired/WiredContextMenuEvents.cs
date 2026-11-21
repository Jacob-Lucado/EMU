using Plus.Communication.Packets.Outgoing.Rooms.Furni.Wired;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Items.Wired.Modern.Addons;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni.Wired;

/// <summary>GetWiredContextMenu: int target type, int target id. Always answered, with no items when nothing applies.</summary>
internal sealed class GetWiredContextMenuEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var targetType = packet.ReadInt();
        var targetId = packet.ReadInt();
        IReadOnlyList<WiredContextMenuEntry> items = [];

        if (targetType == (int)WiredContextMenuTarget.RoomUnit && WiredContextMenuUnits.Find(session, targetId) is var (room, viewer, unit)) {
            items = room.GetWired().ContextMenu(viewer, unit);
        }

        session.Send(new WiredContextMenuComposer(targetType, targetId, items));

        return Task.CompletedTask;
    }
}

/// <summary>WiredContextMenuClick: int target type, int target id, string button id.</summary>
internal sealed class WiredContextMenuClickEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var targetType = packet.ReadInt();
        var targetId = packet.ReadInt();
        var buttonId = packet.ReadString();

        if (targetType == (int)WiredContextMenuTarget.RoomUnit && WiredContextMenuUnits.Find(session, targetId) is var (room, clicker, unit)) {
            room.GetWired().ClickContextMenu(clicker, unit, buttonId);
        }

        return Task.CompletedTask;
    }
}

internal static class WiredContextMenuUnits
{
    /// <summary>The session's room, its own avatar and the room unit with that virtual id, or null.</summary>
    public static (Room Room, RoomUser Viewer, RoomUser Unit)? Find(GameClient session, int virtualId)
    {
        if (session.GetHabbo() is not { CurrentRoom: { } room } habbo || room.GetRoomUserManager() is not { } users
            || users.GetRoomUserByHabbo(habbo.Id) is not { } viewer || users.GetRoomUserByVirtualId(virtualId) is not { } unit) {
            return null;
        }

        return (room, viewer, unit);
    }
}
