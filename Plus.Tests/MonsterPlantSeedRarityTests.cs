using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Rooms.AI.MonsterPlants;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

public sealed class MonsterPlantSeedRarityTests
{
    private static readonly ItemDefinition Seed = new()
    {
        Id = 4582,
        Type = ItemType.Floor,
        ItemName = "mnstr_seed",
        SpecialType = (int)FurniCategory.MonsterplantSeed
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASeedKeepsItsRarityInTheRoomAndInTheInventory(bool keepLegacy)
    {
        var data = FurniExtraData.Load(Seed, "rarity\t2", keepLegacy);

        Assert.Equal(FurniDataStructure.Map, data.StructureType);
        Assert.Equal(2, MonsterPlantSeedRarity.Of(data));
    }

    [Fact]
    public void TheInventoryShowsTheSeedRarityLikeHabbo()
    {
        var item = new InventoryItem { Id = 9, Definition = Seed, ExtraData = FurniExtraData.Load(Seed, "rarity\t2", keepLegacy: false) };
        var packet = new HabbiconTestSupport.RecordingPacket();

        new FurniListAddComposer(InventoryItemSnapshot.Capture(item)).Compose(packet);

        Assert.True(ContainsRun(packet.Writes, [1, 1, "rarity", "2"]), string.Join(",", packet.Writes));
    }

    [Fact]
    public void ASeedWithoutARarityKeepsItsOldData()
    {
        var data = FurniExtraData.Load(Seed, "", keepLegacy: true);

        Assert.NotEqual(FurniDataStructure.Map, data.StructureType);
        Assert.Null(MonsterPlantSeedRarity.Of(data));
    }

    [Fact]
    public void ARaritySeedGrowsOnlyIntoLooksOfThatRarity()
    {
        var names = new SortedSet<string>();

        for (var pick = 0; pick < 20; pick++) {
            var chosen = pick;
            var seedling = new MonsterPlantGenetics(max => Math.Min(chosen, max - 1)).Roll(Seed, 2);
            var color = MonsterPlantGenetics.Colors.Single(c => seedling.Name.StartsWith(c.Name + " ", StringComparison.Ordinal));
            var shape = MonsterPlantGenetics.Shapes.Single(s => seedling.Name.EndsWith(" " + s.Name, StringComparison.Ordinal));

            Assert.Equal(2, seedling.Rarity);
            Assert.Equal(2, color.Rarity + shape.Rarity);
            Assert.EndsWith($" 1 {shape.Part} {color.Palette}", seedling.Look.Parts);
            names.Add(seedling.Name);
        }

        Assert.Equal(["Aenueus Sunspike", "Aenueus Weggytum", "Fulvus Sunspike", "Fulvus Weggytum", "Incarnatus Squarg"], names);
    }

    [Theory]
    [InlineData(99, 11)]
    [InlineData(-3, 0)]
    public void ARarityOutsideTheTableGrowsTheNearestRarity(int stored, int grown)
    {
        var seedling = new MonsterPlantGenetics(_ => 0).Roll(Seed, stored);

        Assert.Equal(grown, seedling.Rarity);
    }

    private static bool ContainsRun(List<object> writes, object[] run) => Enumerable.Range(0, Math.Max(0, writes.Count - run.Length + 1))
        .Any(start => run.Select((value, offset) => Equals(writes[start + offset], value)).All(match => match));
}
