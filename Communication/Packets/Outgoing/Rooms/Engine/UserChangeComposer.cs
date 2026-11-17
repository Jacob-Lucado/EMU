using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Outgoing.Rooms.Engine;

/// <summary>
/// Id, figure, sex, motto, achievement score, an unread
/// string, n x (int, int, int), badges rank.
/// </summary>
public sealed class UserChangeComposer(AvatarChangeSnapshot avatar) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.UserChangeComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(avatar.VirtualId);
        packet.WriteString(avatar.Look);
        packet.WriteString(avatar.Gender);
        packet.WriteString(avatar.Motto);
        packet.WriteInteger(avatar.AchievementPoints);
        packet.WriteString("");
        packet.WriteInteger(0);
        packet.WriteInteger(avatar.BadgesRank);
    }
}
