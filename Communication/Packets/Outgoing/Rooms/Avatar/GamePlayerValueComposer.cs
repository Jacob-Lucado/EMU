using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Rooms.Avatar;

public class GamePlayerValueComposer : IServerPacket
{
    private readonly int _userId;
    private readonly int _value;
    public uint MessageId => ServerPacketHeader.GamePlayerValueComposer;

    public GamePlayerValueComposer(int userId, int value)
    {
        _userId = userId;
        _value = value;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_userId);
        packet.WriteInteger(_value);
    }
}
