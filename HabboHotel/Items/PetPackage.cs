using System.Globalization;

namespace Plus.HabboHotel.Items;

/// <summary>
/// What a pet package furni holds: the pet type, and the furni that takes the package's place when it is opened.
/// An egg or box leaves no furni and hatches its pet in a random breed.
/// </summary>
public sealed record PetPackage(int PetType, string? FurniName, PetBreeds? RandomBreeds = null)
{
    // Habbo's eggs and boxes have no customparams. The breed ranges are the breeds Habbo names for each pet;
    // a Cow Box holds "one of 20 cow pet breeds" and a Gold Cow Box the gold cow, breed 0.
    private static readonly Dictionary<string, PetPackage> Hatchlings = new()
    {
        ["velociraptor_egg"] = new(34, null, new(0, 19)),
        ["pterosaur_egg"] = new(33, null, new(0, 20)),
        ["cowbox"] = new(35, null, new(1, 20)),
        ["cowbox_gold"] = new(35, null, new(0, 0))
    };

    /// <summary>The egg or box with this item name; null for other furni.</summary>
    public static PetPackage? Hatchling(string itemName) => Hatchlings.GetValueOrDefault(itemName);

    /// <summary>The figure the name dialog shows; null when the pet is only chosen when the package opens.</summary>
    public PetPackageFigure? FigureToShow(string state) => RandomBreeds == null ? PetPackageFigure.Of(this, state) : null;

    /// <summary>The pet that comes out: an egg or box picks a random breed, a package shows the figure in its state.</summary>
    public PetPackageFigure Hatch(string state) => RandomBreeds is { } breeds
        ? new(PetType, Random.Shared.Next(breeds.First, breeds.Last + 1), "ffffff")
        : PetPackageFigure.Of(this, state);
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
