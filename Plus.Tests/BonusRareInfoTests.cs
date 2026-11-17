using Plus.Communication.Flash;
using Plus.Communication.Packets.Incoming.LandingView;
using Plus.Communication.Packets.Outgoing;
using Plus.Core.Settings;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users;
using Xunit;

namespace Plus.Tests;

public class BonusRareInfoTests
{
    private static readonly ItemDefinition BonusBag = new() { Id = 9001, SpriteId = 18432, ItemName = "bonusbag26_3" };

    [Fact]
    public async Task DefaultBonusRareIsTheBonusBagEveryHundredTwentyCredits()
    {
        var packet = await Request(new Dictionary<string, string>(), BonusBag);

        Assert.Equal("bonusbag26_3", packet.ReadString());
        Assert.Equal(18432, packet.ReadInt());
        Assert.Equal(120, packet.ReadInt());
        Assert.Equal(120, packet.ReadInt());
        Assert.False(packet.HasDataRemaining());
    }

    [Fact]
    public async Task SettingsChooseTheFurniAndTheCredits()
    {
        var chair = new ItemDefinition { Id = 7, SpriteId = 4321, ItemName = "rare_chair" };

        var packet = await Request(new() { ["landing.bonus_rare.item_name"] = "rare_chair", ["landing.bonus_rare.credits"] = "250" }, chair);

        Assert.Equal("rare_chair", packet.ReadString());
        Assert.Equal(4321, packet.ReadInt());
        Assert.Equal(250, packet.ReadInt());
        Assert.Equal(250, packet.ReadInt());
        Assert.False(packet.HasDataRemaining());
    }

    [Theory]
    [InlineData("", "120")]
    [InlineData("missing_furni", "120")]
    [InlineData("bonusbag26_3", "0")]
    [InlineData("bonusbag26_3", "lots")]
    public async Task UnusableSettingsHideTheBonusRare(string itemName, string credits)
    {
        var packet = await Request(new() { ["landing.bonus_rare.item_name"] = itemName, ["landing.bonus_rare.credits"] = credits }, BonusBag);

        Assert.Equal("", packet.ReadString());
        Assert.Equal(-1, packet.ReadInt());
        Assert.Equal(-1, packet.ReadInt());
        Assert.Equal(-1, packet.ReadInt());
        Assert.False(packet.HasDataRemaining());
    }

    private static async Task<FlashIncomingPacket> Request(Dictionary<string, string> settings, ItemDefinition furni)
    {
        var (client, sent) = HabbiconTestSupport.Client(new Habbo { Id = 5 });
        var items = CatalogSnapshotTestSupport.Proxy<IItemDataManager>((method, args) => method == "GetItemByName"
            ? (string)args[0]! == furni.ItemName ? furni : null
            : throw new NotSupportedException(method));

        await new GetBonusRareInfoEvent(new Settings(settings), items).Parse(client, HabbiconTestSupport.Incoming());

        var (header, payload) = Assert.Single(sent);
        Assert.Equal(ServerPacketHeader.BonusRareInfoComposer, header);

        return new FlashIncomingPacket { Buffer = payload };
    }

    private sealed class Settings(Dictionary<string, string> values) : ISettingsManager
    {
        public string TryGetValue(string value) => values.GetValueOrDefault(value, "0");
        public string? GetOptionalValue(string key) => values.GetValueOrDefault(key);
        public Task Reload() => Task.CompletedTask;
    }
}
