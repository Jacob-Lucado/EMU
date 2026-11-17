using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.Communication.Packets.Outgoing.Rooms.Furni;

public class GnomeBoxComposer : IServerPacket
{
    private readonly uint _itemId;
    private readonly PetPackageFigure? _figure;
    public uint MessageId => ServerPacketHeader.GnomeBoxComposer;

    public GnomeBoxComposer(uint itemId, PetPackageFigure? figure = null)
    {
        _itemId = itemId;
        _figure = figure;
    }

    public void Compose(IOutgoingPacket packet) => packet.WriteUInteger(_itemId);
}
