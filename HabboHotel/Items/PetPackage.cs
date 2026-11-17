namespace Plus.HabboHotel.Items;

/// <summary>What a pet package furni holds: the pet type, and the furni that takes the package's place when it is opened.</summary>
public sealed record PetPackage(int PetType, string FurniName);

/// <summary>The pet a package holds, from the package's state: "type palette colour", such as "1 0 ABABAB".</summary>
public sealed record PetPackageFigure(int Type, int Palette, string Colour)
{
    public static PetPackageFigure Of(PetPackage package, string state) => throw new NotImplementedException();
}
