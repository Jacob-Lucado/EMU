using System.Globalization;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms.AI;

/// <summary>The parts of a horse look that products change. Hair covers the mane and tail, which products always set together.</summary>
public readonly record struct HorseStyle(int Palette, int Hair, int HairPalette, int Saddle);

public static class HorseProducts
{
    public const int HorseType = 15;
    private const int DefaultHairPalette = 1;

    // The first body palette of each colour tag in the horse asset. Breeds 1 to 4 follow it in order.
    private static readonly Dictionary<string, int> BodyColours = new(StringComparer.Ordinal)
    {
        ["Beige"] = 2,
        ["Brown"] = 6,
        ["Dark-Brown"] = 10,
        ["Grey"] = 14,
        ["Red"] = 18,
        ["Purple"] = 22,
        ["Light-Blue"] = 26,
        ["Gold"] = 30,
        ["Black"] = 34,
        ["White"] = 38,
        ["Green"] = 42,
        ["Orange"] = 46,
        ["Dark-Blue"] = 61,
        ["Pink"] = 65,
        ["Yellow"] = 69,
        ["Cyan"] = 73,
        ["Lime"] = 77,
        ["Blue"] = 86,
    };

    /// <summary>The look after using a product with these furnidata customparams, or null when it does not apply to this horse.</summary>
    public static HorseStyle? Apply(HorseStyle style, InteractionType product, string customParams)
    {
        var values = customParams.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (values.Length < 2 || values[0] != HorseType.ToString(CultureInfo.InvariantCulture)) {
            return null;
        }

        return product switch
        {
            InteractionType.HorseBodyDye => Dye(style, values[1]),
            InteractionType.HorseHairDye when values.Length >= 3 && First(values[2]) is int dye => style with { HairPalette = dye },
            // Hair that already has a colour keeps it; otherwise the product's colour is used.
            InteractionType.HorseHairstyle when values.Length >= 4 && First(values[2]) is int hair && First(values[3]) is int defaultColor => style with
            {
                Hair = hair,
                HairPalette = style.HairPalette > 0 ? style.HairPalette : defaultColor,
            },
            InteractionType.HorseSaddle1 or InteractionType.HorseSaddle2 when values.Length >= 4 && First(values[2]) is int saddle => style with { Saddle = saddle },
            _ => null,
        };
    }

    /// <summary>The breed (1 to 4) of a horse body palette, or 0 for a palette without one.</summary>
    public static int Breed(int palette) => BodyColours.Values
        .Where(first => palette >= first && palette < first + 4).Select(first => palette - first + 1).FirstOrDefault();

    /// <summary>The hair palette a horse shows: hair without a colour of its own wears the default palette.</summary>
    public static int HairPalette(int stored) => stored > 0 ? stored : DefaultHairPalette;

    /// <summary>The furniture a horse gets back when its saddle comes off: the saddle product that puts this saddle part on.</summary>
    public static ItemDefinition? SaddleProduct(IEnumerable<ItemDefinition> definitions, int saddle) => definitions
        .Where(definition => Apply(default, definition.InteractionType, definition.CustomParams) is { Saddle: var part } && part == saddle && saddle > 0)
        .MinBy(definition => definition.Id);

    private static HorseStyle? Dye(HorseStyle style, string tag)
    {
        var breed = BodyColours.Values.Where(first => style.Palette >= first && style.Palette < first + 4).Select(first => style.Palette - first).FirstOrDefault(-1);

        return breed >= 0 && BodyColours.TryGetValue(tag, out var colour) ? style with { Palette = colour + breed } : null;
    }

    private static int? First(string list) => int.TryParse(list.Split(',')[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
}
