using Plus.Communication.Flash;
using Plus.Communication.Packets.Incoming.Rooms.Engine;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Fact]
    public async Task NobodyCanUseAGroupGate()
    {
        AdmitOwner();
        var definition = Furni(30, InteractionType.GuildGate, WiredBoxType.None).Definition;
        definition.Modes = 4;
        Inventory(new InventoryItem { Id = 30, Definition = definition, ExtraData = new LegacyDataFormat { Data = "0" } });
        await PlaceObject().Parse(_room, _client, ClientPacket("30 1 1 0"));

        var gate = _room.GetRoomItemHandler().GetItem(30);
        var added = Assert.Single(_client.Packets, packet => packet.Header == ServerPacketHeader.ObjectAddComposer);
        var body = new FlashIncomingPacket { Buffer = added.Body };
        Assert.Equal(gate.Id, body.ReadUInt());
        body.ReadInt();
        body.ReadInt();
        body.ReadInt();
        body.ReadInt();
        body.ReadString();
        body.ReadString();
        body.ReadInt();
        Assert.Equal(0, body.ReadInt());
        Assert.Equal("0", body.ReadString());
        body.ReadInt();
        Assert.Equal(0, body.ReadInt());
        _client.Packets.Clear();

        await new UseFurnitureEvent(new FurnitureUseService(new FurnitureUseStore(_database), Proxy<IQuestManager>((_, _) => null)))
            .Parse(_room, _client, ClientPacket(30, 0));

        Assert.Equal("0", gate.LegacyDataString);
        Assert.DoesNotContain(_client.Packets, packet => packet.Header is ServerPacketHeader.ObjectDataUpdateComposer or ServerPacketHeader.ObjectsDataUpdateComposer);
    }
}
