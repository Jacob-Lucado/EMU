using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Music;

namespace Plus.Communication.Packets.Outgoing.Rooms.Session;

// The web audio link a room plays. An empty link stops it. The room id lets a client ignore a link meant for another room.
public sealed class RoomStreamComposer(uint roomId, RoomStream? stream) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.RoomStreamComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger((int)roomId);
        packet.WriteString(stream?.Kind ?? "");
        packet.WriteString(stream?.Url ?? "");
        packet.WriteString(stream?.SetBy ?? "");
    }
}
