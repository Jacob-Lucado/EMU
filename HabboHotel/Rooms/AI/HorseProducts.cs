using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms.AI;

/// <summary>The parts of a horse look that products change. Hair covers the mane and tail, which products always set together.</summary>
public readonly record struct HorseStyle(int Palette, int Hair, int HairPalette, int Saddle);

public static class HorseProducts
{
    /// <summary>The look after using a product with these furnidata customparams, or null when it does not apply to this horse.</summary>
    public static HorseStyle? Apply(HorseStyle style, InteractionType product, string customParams) => throw new NotImplementedException();
}
