using System.Globalization;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Recycler;

internal sealed record RecyclerBoxKind(string ClassName, int SpriteId, string DateFormat);

internal static class RecyclerBox
{
    // The Furni-Matic gives Habbo's Mystery Box. Ecotron boxes from older exchanges can still be opened.
    public static readonly RecyclerBoxKind Mystery = new("matic_box", 4692, "yyyy-MM-dd");
    public static readonly RecyclerBoxKind Ecotron = new("ecotron_box", 3095, "d-M-yyyy");

    public static RecyclerBoxKind? KindOf(ItemDefinition definition) =>
        new[] { Mystery, Ecotron }.FirstOrDefault(kind => definition.ItemName == kind.ClassName && definition.SpriteId == kind.SpriteId);
    public static bool IsIdentity(ItemDefinition definition) => KindOf(definition) is not null;
    public static bool IsDefinition(ItemDefinition definition) => IsIdentity(definition)
        && definition.Type == ItemType.Floor && definition.ProductType == "s" && definition.InteractionType == InteractionType.None;
    public static ItemDefinition? Find(IEnumerable<ItemDefinition> definitions, RecyclerBoxKind kind)
    {
        var boxes = definitions.Where(definition => KindOf(definition) == kind).Take(2).ToArray();

        return boxes.Length == 1 && IsDefinition(boxes[0]) ? boxes[0] : null;
    }
    public static string Date(DateTimeOffset now) => now.UtcDateTime.ToString(Mystery.DateFormat, CultureInfo.InvariantCulture);
    public static bool HasDate(RecyclerBoxKind kind, string data) =>
        DateOnly.TryParseExact(data, kind.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
