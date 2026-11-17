using Plus.HabboHotel.Badges.Rarity;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Badges;

/// <summary>Type, rarity, page, page size, total entries, the entries, then whether the viewer's own row follows.</summary>
public sealed class BadgeLeaderboardComposer(BadgeLeaderboardPage page) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.BadgeLeaderboardComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(page.Request.Type);
        packet.WriteInteger(page.Request.Rarity);
        packet.WriteInteger(page.Request.Page);
        packet.WriteInteger(page.Request.Size);
        packet.WriteInteger(page.TotalEntries);
        packet.WriteInteger(page.Entries.Length);

        foreach (var entry in page.Entries) {
            Write(packet, entry);
        }

        packet.WriteBoolean(true);
        Write(packet, page.Own);
    }

    private static void Write(IOutgoingPacket packet, BadgeLeaderboardEntry entry)
    {
        packet.WriteInteger(entry.UserId);
        packet.WriteString(entry.Username);
        packet.WriteString(entry.Figure);
        packet.WriteInteger(entry.Rank);
        packet.WriteInteger(entry.Score);
    }
}
