using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Rooms.Settings;

public sealed class RoomSettingsSaveErrorComposer(uint roomId, int code, string message) : IServerPacket
{
    // A custom setting error; the message names the field the client marks as invalid.
    public const int CustomSettingError = 16;

    public uint MessageId => ServerPacketHeader.RoomSettingsSaveErrorComposer;

    public void Compose(IOutgoingPacket packet) => throw new NotImplementedException();
}
