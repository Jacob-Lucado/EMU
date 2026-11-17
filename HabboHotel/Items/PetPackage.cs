using System.Globalization;

namespace Plus.HabboHotel.Items;

/// <summary>
/// What a pet package furni holds: the pet type, and the furni that takes the package's place when it is opened.
/// An egg or box leaves no furni and hatches its pet in a random breed.
/// </summary>
public sealed record PetPackage(int PetType, string? FurniName, PetBreeds? RandomBreeds = null)
{
    /// <summary>The egg or box with this item name; null for other furni.</summary>
    public static PetPackage? Hatchling(string itemName) => null;

    /// <summary>The figure the name dialog shows; null when the pet is only chosen when the package opens.</summary>
    public PetPackageFigure? FigureToShow(string state) => PetPackageFigure.Of(this, state);
}

/// <summary>The breeds, first to last, a hatching pet is picked from.</summary>
public sealed record PetBreeds(int First, int Last);

/// <summary>The pet a package holds, from the package's state: "type palette colour", such as "1 0 ABABAB".</summary>
public sealed record PetPackageFigure(int Type, int Palette, string Colour)
{
    /// <summary>A package without a valid state holds a pet of its own type in the first palette, white.</summary>
    public static PetPackageFigure Of(PetPackage package, string state)
    {
        var parts = state.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length >= 3
               && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var type)
               && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var palette)
               && parts[2].Length == 6 && parts[2].All(Uri.IsHexDigit)
            ? new(type, palette, parts[2])
            : new(package.PetType, 0, "FFFFFF");
    }
}
