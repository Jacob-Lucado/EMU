using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Music;

public static class RoomMusic
{
    public static RoomStreamComposer Packet(Room room) => new(room.RoomId, room.Playlist.Current, room.Playlist.Token, room.Playlist.Queued);

    public static void Broadcast(Room room) => room.SendPacket(Packet(room));

    public static void SendTo(GameClient session, Room room)
    {
        if (room.Playlist.Current != null) {
            session.Send(Packet(room));
        }
    }
}
