using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Music;

namespace Plus.Communication.Packets.Outgoing.Rooms.Session;

// The web audio link the room plays. An empty link stops it.
public sealed class RoomStreamComposer(RoomStream? stream) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.RoomStreamComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteString(stream?.Kind ?? "");
        packet.WriteString(stream?.Url ?? "");
        packet.WriteString(stream?.SetBy ?? "");
    }
}
