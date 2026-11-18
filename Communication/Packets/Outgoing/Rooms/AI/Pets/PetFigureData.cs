using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;

/// <summary>Writes a pet look (<c>type palette color count part id color ...</c>) as the client's pet figure block.</summary>
public static class PetFigureData
{
    public static void Write(IOutgoingPacket packet, string look)
    {
        var bits = look.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var palette = Number(bits, 1);
        packet.WriteInteger(Number(bits, 0));
        packet.WriteInteger(palette);
        packet.WriteString(bits.Length > 2 ? bits[2] : "ffffff");
        packet.WriteInteger(palette);
        var parts = Math.Max(0, Math.Min(Number(bits, 3), (bits.Length - 4) / 3));
        packet.WriteInteger(parts);

        for (var part = 0; part < parts; part++) {
            packet.WriteInteger(Number(bits, 4 + part * 3));
            packet.WriteInteger(Number(bits, 5 + part * 3));
            packet.WriteInteger(Number(bits, 6 + part * 3));
        }
    }

    private static int Number(string[] bits, int index) => index < bits.Length && int.TryParse(bits[index], out var value) ? value : 0;
}
