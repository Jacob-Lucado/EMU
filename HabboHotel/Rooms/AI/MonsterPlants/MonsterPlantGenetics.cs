using Plus.HabboHotel.Items;
using PlantColor = (int Palette, string Name, int Rarity);
using PlantShape = (int Part, string Name, int Rarity);

namespace Plus.HabboHotel.Rooms.AI.MonsterPlants;

/// <summary>
/// A plant's look is figure layer 0 (a base palette) and layer 1 (shape part and colour palette). Its name is the colour
/// word and the shape word, and its rarity is the shape rarity plus the colour rarity.
/// </summary>
public sealed class MonsterPlantGenetics(Func<int, int> roll) : IMonsterPlantGenetics
{
    private const int BasePalettes = 11;

    public static IReadOnlyList<PlantColor> Colors { get; } =
    [
        (0, "Aenueus", 0), (9, "Fulvus", 0), (3, "Viridulus", 1), (5, "Incarnatus", 2),
        (8, "Amethyst", 3), (6, "Azureus", 4), (4, "Cyaneus", 5)
    ];

    public static IReadOnlyList<PlantShape> Shapes { get; } =
    [
        (5, "Squarg", 0), (9, "Weggytum", 2), (4, "Sunspike", 2), (6, "Shroomer", 3),
        (11, "Hairbullis", 4), (10, "Wystique", 4), (8, "Abysswirl", 5), (12, "Snozzle", 6)
    ];

    public MonsterPlantGenetics() : this(Random.Shared.Next) { }

    private static readonly IReadOnlyList<(PlantColor Color, PlantShape Shape)> Looks =
        Colors.SelectMany(color => Shapes.Select(shape => (color, shape))).ToList();

    private static readonly int MaxRarity = Colors.Max(color => color.Rarity) + Shapes.Max(shape => shape.Rarity);

    public MonsterPlantSeedling Roll(ItemDefinition seed, int? rarity)
    {
        var (color, shape) = rarity is { } wanted ? LookOfRarity(Math.Clamp(wanted, 0, MaxRarity)) : (Colors[roll(Colors.Count)], Shapes[roll(Shapes.Count)]);
        var basePalette = roll(BasePalettes);

        return new($"{color.Name} {shape.Name}", new(0, "ffffff", $"2 0 -1 {basePalette} 1 {shape.Part} {color.Palette}"),
            shape.Rarity + color.Rarity);
    }

    public int BreedRarity(int first, int second) => throw new NotImplementedException();

    private (PlantColor Color, PlantShape Shape) LookOfRarity(int rarity)
    {
        var looks = Looks.Where(look => look.Color.Rarity + look.Shape.Rarity == rarity).ToList();

        return looks[roll(looks.Count)];
    }
}
