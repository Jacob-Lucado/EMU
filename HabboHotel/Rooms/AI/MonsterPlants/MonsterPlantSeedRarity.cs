using Plus.HabboHotel.Items.DataFormat;

namespace Plus.HabboHotel.Rooms.AI.MonsterPlants;

/// <summary>A seed keeps the rarity of the plant it grows into as the <c>rarity</c> value of its map data.</summary>
public static class MonsterPlantSeedRarity
{
    public const string Key = "rarity";

    public static int? Of(IFurniObjectData? data) => null;
}
