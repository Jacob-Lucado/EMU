using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Outgoing.Groups;

public class GroupCreationWindowComposer : IServerPacket
{
    private readonly GroupCreationPresentation _presentation;
    public uint MessageId => ServerPacketHeader.GroupCreationWindowComposer;

    public GroupCreationWindowComposer(GroupCreationPresentation presentation)
    {
        _presentation = presentation;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_presentation.Price);
        packet.WriteInteger(_presentation.Rooms.Length); //Room count that the user has.

        foreach (var room in _presentation.Rooms) {
            packet.WriteUInteger(room.Id); //Room Id
            packet.WriteString(room.Name); //Room Name
            packet.WriteBoolean(false); // Whether the room has controllers.
        }

        packet.WriteInteger(_presentation.BadgePieces.Length);

        foreach (var piece in _presentation.BadgePieces) {
            packet.WriteInteger(piece.Symbol);
            packet.WriteInteger(piece.Colour);
            packet.WriteInteger(piece.Position);
        }
    }
}
