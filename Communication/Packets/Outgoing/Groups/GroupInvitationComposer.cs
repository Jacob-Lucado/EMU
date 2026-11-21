using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Groups;

public class GroupInvitationComposer(int groupId, string groupName, string badgeCode, uint roomId) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.GroupInvitationComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(groupId);
        packet.WriteString(groupName);
        packet.WriteString(badgeCode);
        packet.WriteInteger((int)roomId);
    }
}
