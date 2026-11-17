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

public interface IHorseCustomizationStore
{
    void UpdatePet(int petId, string column, object value);
    void ConsumeItem(int petId, HorseStyle style, uint itemId, uint roomId, int ownerId);
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

    public void ConsumeItem(int petId, HorseStyle style, uint itemId, uint roomId, int ownerId)
    {
        using var connection = database.Connection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var look = new { race = style.Palette.ToString(CultureInfo.InvariantCulture), pethair = style.Hair, hairdye = style.HairPalette, saddle = style.Saddle, petId };

        if (connection.Execute("UPDATE bots_petdata SET race=@race, pethair=@pethair, hairdye=@hairdye, have_saddle=@saddle WHERE id=@petId LIMIT 1", look, transaction) != 1 ||
            connection.Execute("DELETE FROM items WHERE id=@itemId AND room_id=@roomId AND user_id=@ownerId LIMIT 1",
                new { itemId, roomId, ownerId }, transaction) != 1) {
            throw new InvalidOperationException("The horse customization was not persisted.");
        }

        transaction.Commit();
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

        store.UpdatePet(petUser.PetData.PetId, "have_saddle", 0);
        petUser.PetData.Saddle = 0;

        GiveBack(session, inventory.Furniture, itemData);
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

        var oldSaddle = style.Saddle != current.Saddle ? HorseProducts.SaddleProduct(itemDataManager.Items.Values, current.Saddle) : null;
        store.ConsumeItem(pet.PetId, style, itemId, placed ? room.Id : 0, session.GetHabbo().Id);
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

        if (oldSaddle != null && session.GetHabbo().Inventory is { } owned) {
            GiveBack(session, owned.Furniture, oldSaddle);
        }

        room.SendUser(petUser);
        room.SendPacket(new PetHorseFigureInformationComposer(PetAppearanceSnapshots.Horse(petUser)));
    }

    private void GiveBack(GameClient session, FurnitureInventoryComponent inventory, ItemDefinition saddle)
    {
        var item = itemFactory.CreateSingleItemNullable(saddle, session.GetHabbo(), "", "")?.ToInventoryItem();

        if (item != null) {
            inventory.AddItem(item);
            session.Send(new FurniListAddComposer(InventoryItemSnapshot.Capture(item)));
        }
    }
}
