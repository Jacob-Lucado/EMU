using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Catalog;

public class CheckGnomeNameComposer : IServerPacket
{
    private readonly uint _itemId;
    private readonly string _petName;
    private readonly PetPackageNameError _errorId;
    public uint MessageId => ServerPacketHeader.CheckGnomeNameComposer;

    public CheckGnomeNameComposer(uint itemId, string petName, PetPackageNameError errorId)
    {
        _itemId = itemId;
        _petName = petName;
        _errorId = errorId;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteUInteger(_itemId);
        packet.WriteInteger((int)_errorId);
        packet.WriteString(_errorId == PetPackageNameError.None ? "" : _petName);
    }
}
