using Plus.Communication.Packets.Incoming.Rooms.Engine;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Quests;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Fact]
    public async Task NobodyCanUseAGroupGate()
    {
        AdmitOwner();
        var gate = Add(30, 1, 1, type: InteractionType.GuildGate);
        gate.Definition.Modes = 4;
        gate.ExtraData = new LegacyDataFormat { Data = "0" };
        _client.Packets.Clear();

        await new UseFurnitureEvent(new FurnitureUseService(new FurnitureUseStore(_database), Proxy<IQuestManager>((_, _) => null)))
            .Parse(_room, _client, ClientPacket(30, 0));

        Assert.Equal("0", gate.LegacyDataString);
        Assert.DoesNotContain(_client.Packets, packet => packet.Header is ServerPacketHeader.ObjectDataUpdateComposer or ServerPacketHeader.ObjectsDataUpdateComposer);
        Assert.Equal(0, RoomItemSnapshot.Capture(gate).UseButton);
    }
}
