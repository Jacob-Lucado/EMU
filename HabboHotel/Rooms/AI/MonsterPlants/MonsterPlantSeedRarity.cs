using System.Globalization;
using Plus.HabboHotel.Items.DataFormat;

namespace Plus.HabboHotel.Rooms.AI.MonsterPlants;

/// <summary>A seed keeps the rarity of the plant it grows into as the <c>rarity</c> value of its map data.</summary>
public static class MonsterPlantSeedRarity
{
    public const string Key = "rarity";

    public static int? Of(IFurniObjectData? data) =>
        data is MapDataFormat map && map.Data.TryGetValue(Key, out var value)
        && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rarity)
            ? rarity
            : null;

    public static string Data(int rarity) =>
        new MapDataFormat(new() { [Key] = rarity.ToString(CultureInfo.InvariantCulture) }).Serialize();

    /// <summary>The stored seed data, or null when it holds no rarity.</summary>
    public static MapDataFormat? Load(string stored)
    {
        if (stored.Length == 0) {
            return null;
        }

        var map = new MapDataFormat();
        map.Store(stored);

        return Of(map) is null ? null : map;
    }
}
