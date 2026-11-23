using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Music;

namespace Plus.Communication.Packets.Incoming.Rooms.Session;

// A room's player says the current song has ended. Only people with rights are listened to, so a listener cannot skip songs for the room.
public sealed class RoomStreamEndedEvent(IRoomStreamLog log) : RoomPacketEvent
{
    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var roomId = packet.ReadInt();
        var token = packet.ReadInt();

        if (roomId != room.RoomId || !room.CheckRights(session) || !room.Playlist.Ended(token, DateTimeOffset.UtcNow)) {
            return Task.CompletedTask;
        }

        if (room.Playlist.Current is { } current) {
            log.Record(room.RoomId, current);
        }

        RoomMusic.Broadcast(room);

        return Task.CompletedTask;
    }
}
