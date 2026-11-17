using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms.AI.MonsterPlants;

/// <summary>
/// A plant's look is figure layer 0 (a base palette) and layer 1 (shape part and colour palette). Its name is the colour
/// word and the shape word, and its rarity is the shape rarity plus the colour rarity.
/// </summary>
public sealed class MonsterPlantGenetics(Func<int, int> roll) : IMonsterPlantGenetics
{
    private const int BasePalettes = 11;

    public static IReadOnlyList<(int Palette, string Name, int Rarity)> Colors { get; } =
    [
        (0, "Aenueus", 0), (9, "Fulvus", 0), (3, "Viridulus", 1), (5, "Incarnatus", 2),
        (8, "Amethyst", 3), (6, "Azureus", 4), (4, "Cyaneus", 5)
    ];

    public static IReadOnlyList<(int Part, string Name, int Rarity)> Shapes { get; } =
    [
        (5, "Squarg", 0), (9, "Weggytum", 2), (4, "Sunspike", 2), (6, "Shroomer", 3),
        (11, "Hairbullis", 4), (10, "Wystique", 4), (8, "Abysswirl", 5), (12, "Snozzle", 6)
    ];

    public MonsterPlantGenetics() : this(Random.Shared.Next) { }

    public MonsterPlantSeedling Roll(ItemDefinition seed) => throw new NotImplementedException();
}
