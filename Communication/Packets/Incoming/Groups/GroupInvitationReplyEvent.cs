using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class GroupInvitationReplyEvent(IGroupInvitationService invitations) : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var groupId = packet.ReadInt();
        var answer = packet.ReadInt();

        return invitations.Reply(session, groupId, answer);
    }
}
