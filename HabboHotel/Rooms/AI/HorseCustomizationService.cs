using System.Data;
using System.Globalization;
using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Rooms.AI;

public interface IHorseCustomizationService
{
    void RemoveSaddle(GameClient session, int petId);
    void ToggleRiding(Room room, GameClient session, int petId);
    void ApplyEffect(Room room, GameClient session, uint itemId, int petId);
}

public sealed record SaddleRefund(IItemFactory Factory, ItemDefinition Definition, Plus.HabboHotel.Users.Habbo Owner)
{
    public Item Create(IDbConnection connection, IDbTransaction transaction) =>
        Factory.CreateSingleItemNullable(connection, transaction, Definition, Owner, "");
}

public interface IHorseCustomizationStore
{
    void UpdatePet(int petId, string column, object value);
    Item? ConsumeItem(int petId, HorseStyle style, uint itemId, uint roomId, int ownerId, int previousSaddle, SaddleRefund? refund);
    Item RemoveSaddle(int petId, int saddle, SaddleRefund refund);
}

public sealed class HorseCustomizationStore(IDatabase database) : IHorseCustomizationStore
{
    private static readonly HashSet<string> Columns = ["have_saddle", "anyone_ride"];

    public void UpdatePet(int petId, string column, object value)
    {
        if (!Columns.Contains(column)) {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        using var connection = database.Connection();

        if (connection.Execute($"UPDATE bots_petdata SET `{column}`=@value WHERE id=@petId LIMIT 1", new { value, petId }) != 1) {
            throw new InvalidOperationException("The horse customization was not persisted.");
        }
    }

    public Item RemoveSaddle(int petId, int saddle, SaddleRefund refund)
    {
        using var connection = database.Connection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        if (connection.Execute("UPDATE bots_petdata SET have_saddle=0 WHERE id=@petId AND have_saddle=@saddle LIMIT 1", new { petId, saddle }, transaction) != 1) {
            throw new InvalidOperationException("The saddle has already changed.");
        }

        var item = refund.Create(connection, transaction);
        transaction.Commit();

        return item;
    }

    public Item? ConsumeItem(int petId, HorseStyle style, uint itemId, uint roomId, int ownerId, int previousSaddle, SaddleRefund? refund)
    {
        using var connection = database.Connection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var look = new { race = style.Palette.ToString(CultureInfo.InvariantCulture), pethair = style.Hair, hairdye = style.HairPalette, saddle = style.Saddle, previousSaddle, petId };

        if (connection.Execute("UPDATE bots_petdata SET race=@race, pethair=@pethair, hairdye=@hairdye, have_saddle=@saddle WHERE id=@petId AND have_saddle=@previousSaddle LIMIT 1", look, transaction) != 1 ||
            connection.Execute("DELETE FROM items WHERE id=@itemId AND room_id=@roomId AND user_id=@ownerId LIMIT 1",
                new { itemId, roomId, ownerId }, transaction) != 1) {
            throw new InvalidOperationException("The horse customization was not persisted.");
        }

        var item = refund?.Create(connection, transaction);
        transaction.Commit();

        return item;
    }
}

public sealed class HorseCustomizationService(
    IRoomManager roomManager,
    IItemDataManager itemDataManager,
    IItemFactory itemFactory,
    IHorseCustomizationStore store,
    IPetInformationService petInformation) : IHorseCustomizationService
{
    public void RemoveSaddle(GameClient session, int petId)
    {
        var habbo = session.GetHabbo();

        // The saddle goes back into the loaded inventory, so it is not removed without one.
        if (!habbo.InRoom || habbo.CurrentRoom == null || habbo.Inventory is not { } inventory || !roomManager.TryGetRoom(habbo.CurrentRoom.Id, out var room)) {
            return;
        }

        if (!room.GetRoomUserManager().TryGetPet(petId, out var petUser) || petUser.PetData?.OwnerId != habbo.Id) {
            return;
        }

        if (HorseProducts.SaddleProduct(itemDataManager.Items.Values, petUser.PetData.Saddle) is not { } itemData) {
            return;
        }

        var returned = store.RemoveSaddle(petUser.PetData.PetId, petUser.PetData.Saddle, new(itemFactory, itemData, habbo));
        petUser.PetData.Saddle = 0;

        GiveBack(session, inventory.Furniture, returned);
        room.SendUser(petUser);
        room.SendPacket(new PetHorseFigureInformationComposer(PetAppearanceSnapshots.Horse(petUser)));
    }

    public void ToggleRiding(Room room, GameClient session, int petId)
    {
        if (!room.GetRoomUserManager().TryGetPet(petId, out var pet) || pet.PetData?.OwnerId != session.GetHabbo().Id) {
            return;
        }

        var anyoneCanRide = pet.PetData.AnyoneCanRide == 1 ? 0 : 1;
        store.UpdatePet(pet.PetData.PetId, "anyone_ride", anyoneCanRide);
        pet.PetData.AnyoneCanRide = anyoneCanRide;
        room.SendPacket(new PetInformationComposer(petInformation.Capture(pet.PetData)));
    }

    public void ApplyEffect(Room room, GameClient session, uint itemId, int petId)
    {
        var habbo = session.GetHabbo();
        var placed = room.GetRoomItemHandler().GetItem(itemId);
        var inventory = placed == null ? habbo.Inventory?.Furniture : null;
        var stored = inventory?.GetItem(itemId);
        var definition = placed?.Definition ?? stored?.Definition;

        if (definition == null || placed is { } item && (item.IsTemporary || item.UserId != habbo.Id)) {
            return;
        }

        if (!room.GetRoomUserManager().TryGetPet(petId, out var petUser) || petUser.PetData?.OwnerId != habbo.Id) {
            return;
        }

        if (stored != null && !stored.TryReserve()) {
            return;
        }

        try {
            Customize(room, session, petUser, definition, itemId, placed != null, inventory);
        }
        finally {
            stored?.ReleaseReservation();
        }
    }

    private void Customize(Room room, GameClient session, RoomUser petUser, ItemDefinition definition, uint itemId, bool placed, FurnitureInventoryComponent? inventory)
    {
        var pet = petUser.PetData;

        if (pet.Type != HorseProducts.HorseType || !int.TryParse(pet.Race, NumberStyles.Integer, CultureInfo.InvariantCulture, out var palette)) {
            return;
        }

        var current = new HorseStyle(palette, pet.PetHair, pet.HairDye, pet.Saddle);

        if (HorseProducts.Apply(current, definition.InteractionType, definition.CustomParams) is not { } style) {
            return;
        }

        SaddleRefund? refund = null;
        if (current.Saddle > 0 && definition.InteractionType is InteractionType.HorseSaddle1 or InteractionType.HorseSaddle2) {
            if (session.GetHabbo().Inventory == null || HorseProducts.SaddleProduct(itemDataManager.Items.Values, current.Saddle) is not { } saddle) {
                return;
            }

            refund = new(itemFactory, saddle, session.GetHabbo());
        }

        var returned = store.ConsumeItem(pet.PetId, style, itemId, placed ? room.Id : 0, session.GetHabbo().Id, current.Saddle, refund);
        pet.Race = style.Palette.ToString(CultureInfo.InvariantCulture);
        pet.PetHair = style.Hair;
        pet.HairDye = style.HairPalette;
        pet.Saddle = style.Saddle;

        if (placed) {
            room.GetRoomItemHandler().RemoveFurniture(session, itemId);
        }
        else if (inventory != null) {
            inventory.RemoveItem(itemId);
            session.Send(new FurniListRemoveComposer(itemId));
        }

        if (returned != null && session.GetHabbo().Inventory is { } owned) {
            GiveBack(session, owned.Furniture, returned);
        }

        room.SendUser(petUser);
        room.SendPacket(new PetHorseFigureInformationComposer(PetAppearanceSnapshots.Horse(petUser)));
    }

    private static void GiveBack(GameClient session, FurnitureInventoryComponent inventory, Item saddle)
    {
        var item = saddle.ToInventoryItem();

        if (item != null) {
            inventory.AddItem(item);
            session.Send(new FurniListAddComposer(InventoryItemSnapshot.Capture(item)));
        }
    }
}
