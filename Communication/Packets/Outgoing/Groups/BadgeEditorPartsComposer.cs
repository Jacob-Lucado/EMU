using System.Collections.Immutable;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Outgoing.Groups;

public class BadgeEditorPartsComposer(BadgeEditorPresentation presentation) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.BadgeEditorPartsComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(presentation.Bases.Length);

        foreach (var part in presentation.Bases) {
            packet.WriteInteger(part.EditorId);
            packet.WriteString(part.AssetOne);
            packet.WriteString(part.AssetTwo);
        }

        packet.WriteInteger(presentation.Symbols.Length);

        foreach (var part in presentation.Symbols) {
            packet.WriteInteger(part.EditorId);
            packet.WriteString(part.AssetOne);
            packet.WriteString(part.AssetTwo);
        }

        packet.WriteInteger(presentation.BaseColours.Length);

        foreach (var color in presentation.BaseColours) {
            packet.WriteInteger(color.Id);
            packet.WriteString(color.Colour);
        }

        packet.WriteInteger(presentation.SymbolColours.Length);

        foreach (var color in presentation.SymbolColours) {
            packet.WriteInteger(color.Id);
            packet.WriteString(color.Colour);
        }

        packet.WriteInteger(presentation.BackgroundColours.Length);

        foreach (var color in presentation.BackgroundColours) {
            packet.WriteInteger(color.Id);
            packet.WriteString(color.Colour);
        }

        // Badge code ids follow the fields the official client reads, so clients that draw badge codes themselves can map each part.
        WriteCodeIds(packet, presentation.Bases);
        WriteCodeIds(packet, presentation.Symbols);
    }

    private static void WriteCodeIds(IOutgoingPacket packet, ImmutableArray<BadgePartPresentation> parts)
    {
        packet.WriteInteger(parts.Length);

        foreach (var part in parts) {
            packet.WriteInteger(part.CodeId);
        }
    }
}
