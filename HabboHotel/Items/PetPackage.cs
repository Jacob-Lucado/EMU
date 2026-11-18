namespace Plus.HabboHotel.Items;

/// <summary>What a pet package furni holds: the pet type, and the furni that takes the package's place when it is opened.</summary>
public sealed record PetPackage(int PetType, string FurniName);
