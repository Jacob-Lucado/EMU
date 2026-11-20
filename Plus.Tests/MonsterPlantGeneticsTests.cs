using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.AI.MonsterPlants;
using Xunit;

namespace Plus.Tests;

public sealed class MonsterPlantGeneticsTests
{
    [Theory]
    [InlineData(0, 5, 6, "Aenueus Squarg", "2 0 -1 6 1 5 0", 0)]
    [InlineData(9, 9, 1, "Fulvus Weggytum", "2 0 -1 1 1 9 9", 2)]
    public void ASeedGrowsIntoItsColourAndShapeLikeHabbo(int palette, int part, int basePalette, string name, string parts, int rarity)
    {
        var picks = new Queue<int>([
            MonsterPlantGenetics.Colors.Select(c => c.Palette).ToList().IndexOf(palette),
            MonsterPlantGenetics.Shapes.Select(s => s.Part).ToList().IndexOf(part),
            basePalette
        ]);

        var seedling = new MonsterPlantGenetics(_ => picks.Dequeue()).Roll(new ItemDefinition { ItemName = "mnstr_seed" }, null);

        Assert.Equal((name, 0, "ffffff", parts, rarity), (seedling.Name, seedling.Look.Race, seedling.Look.Color, seedling.Look.Parts, seedling.Rarity));
    }
}
