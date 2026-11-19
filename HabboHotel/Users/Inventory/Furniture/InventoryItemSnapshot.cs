using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Recycler;

namespace Plus.HabboHotel.Users.Inventory.Furniture;

public sealed record InventoryItemSnapshot(uint Id, string Type, int SpriteId, FurniCategory Category,
    FurnitureDataSnapshot Data, uint UniqueNumber, uint UniqueSeries, bool Recyclable, bool Tradeable,
    bool Stackable, bool StackableWhenAdded, bool MarketplaceSellable, bool IsWallItem)
{
    public static InventoryItemSnapshot Capture(InventoryItem item) => Capture(item, null);

    public static InventoryItemSnapshot Capture(InventoryItem item, GuildFurnitureAppearance? guild)
    {
        var definition = item.Definition;
        var isGuildFurni = GuildFurnitureAppearance.IsGuildFurni(definition);

        // The client applies wallpaper, floor and landscape items to the room instead of placing them.
        var category = definition.InteractionType switch
        {
            InteractionType.Wallpaper => FurniCategory.WallPaper,
            InteractionType.Floor => FurniCategory.Floor,
            InteractionType.Landscape => FurniCategory.Landscape,
            InteractionType.HorseBodyDye => FurniCategory.PetShampoo,
            InteractionType.HorseHairstyle => FurniCategory.PetCustomPart,
            InteractionType.HorseHairDye => FurniCategory.PetCustomPartShampoo,
            InteractionType.HorseSaddle1 or InteractionType.HorseSaddle2 => FurniCategory.PetSaddle,
            _ when isGuildFurni => FurniCategory.GuildFurni,
            _ => RecyclerBox.IsDefinition(definition)
                ? FurniCategory.EcotronBox
                : Plus.HabboHotel.Rooms.Music.RoomMusicDefinition.IsDisc(definition) ? FurniCategory.TraxSong : definition.Category
        };

        // A Furni-Matic box keeps its date for the room, but the inventory lists it without data.
        var data = category == FurniCategory.EcotronBox ? new FurnitureDataSnapshot.Legacy("")
            : isGuildFurni && guild != null ? guild.Data(GuildState(item.ExtraData))
            : FurnitureDataSnapshot.Capture(item.ExtraData);

        return new(item.Id, definition.Type.ToCharCode(), definition.SpriteId, category,
            data, item.UniqueNumber, item.UniqueSeries,
            definition.AllowEcotronRecycle, definition.AllowTrade, item.ShouldStackInInventory(),
            item.UniqueNumber == 0 && definition.AllowInventoryStack, definition.AllowMarketplaceSell, item.IsWallItem);
    }

    // The inventory stores most states as empty data; a group furni then shows state 0.
    private static string GuildState(IFurniObjectData data) => data is LegacyDataFormat { Data.Length: > 0 } legacy ? legacy.Data : "0";
}
