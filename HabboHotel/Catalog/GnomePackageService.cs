using Plus.Communication.Packets;
using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.HabboHotel.Catalog.Utilities;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI.Speech;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Catalog;

[Singleton]
public interface IGnomePackageService
{
    void Open(Room room, GameClient session, uint itemId, string petName);
}

public sealed class GnomePackageService(
    IGnomePackageStore store,
    IItemDataManager definitions,
    IItemFactory items,
    TimeProvider clock) : IGnomePackageService
{
    public void Open(Room room, GameClient session, uint itemId, string petName)
    {
        var habbo = session.GetHabbo();

        if (!ReferenceEquals(habbo.CurrentRoom, room)) {
            return;
        }

        var item = room.GetRoomItemHandler().GetItem(itemId);

        if (item == null) {
            return;
        }

        lock (item) {
            if (!ReferenceEquals(room.GetRoomItemHandler().GetItem(itemId), item)
                || item.IsTemporary || item.RoomId != room.RoomId
                || item.OwnerId != habbo.Id
                || habbo.Inventory is not { } inventory) {
                return;
            }

            var furni = PetPackageFurni(item.Definition);

            if (item.Definition?.InteractionType != InteractionType.GnomeBox && furni == null) {
                return;
            }

            if (!PetUtility.CheckPetName(petName)) {
                session.Send(new CheckGnomeNameComposer(itemId, petName, PetPackageNameError.InvalidName));

                return;
            }

            if (furni != null) {
                OpenPetPackage(room, session, item, furni, petName);

                return;
            }

            var pet = store.Open(new(item.Id, item.Definition.Id, habbo.Id, habbo.Username, room.RoomId,
                item.GetX, item.GetY, item.GetZ, petName, RandomClothing(), clock.GetUtcNow()));

            if (pet == null) {
                session.SendNotification("Oops, an error occoured. Please report this!");

                return;
            }

            room.GetRoomItemHandler().RemoveFurniture(session, item.Id);
            session.Send(new CheckGnomeNameComposer(itemId, petName, PetPackageNameError.None));
            var speeches = new List<RandomSpeech>();
            room.GetRoomUserManager().DeployBot(new(pet.PetId, pet.RoomId, "pet", "freeroam", pet.Name, "", pet.Look,
                pet.X, pet.Y, pet.Z, 0, 0, 0, 0, 0, ref speeches, "", 0, pet.OwnerId, false, 0, false, 0), pet);

            if (definitions.Items.TryGetValue(320, out var foodDefinition)) {
                var foodItem = items.CreateSingleItemNullable(foodDefinition, habbo, "", "");

                if (foodItem != null) {
                    var food = foodItem.ToInventoryItem();
                    inventory.Furniture.AddItem(food);
                    session.Send(new FurniListNotificationComposer(food.Id, 1));
                }
            }
        }
    }

    /// <summary>The furni a pet package leaves behind; null when the furni is no pet package or its furni is unknown.</summary>
    private ItemDefinition? PetPackageFurni(ItemDefinition? definition) =>
        definition is { InteractionType: InteractionType.None, PetPackage: { } package }
        && definitions.GetItemByName(package.FurniName) is { Type: ItemType.Floor } furni
            ? furni
            : null;

    // Habbo replaces the package with its furni facing direction 4 and puts the named pet on top of it.
    private void OpenPetPackage(Room room, GameClient session, Item item, ItemDefinition furni, string petName)
    {
        var habbo = session.GetHabbo();
        var map = room.GetGameMap();
        var tiles = Gamemap.GetAffectedTiles(furni.Length, furni.Width, item.GetX, item.GetY, PetPackageFurniRotation);

        if (!map.ValidTile(item.GetX, item.GetY) || tiles.Values.Any(tile => !map.ValidTile(tile.X, tile.Y))) {
            return;
        }

        var figure = PetPackageFigure.Of(item.Definition.PetPackage!, item.LegacyDataString);
        var opened = store.OpenPetPackage(new(item.Id, item.Definition.Id, habbo.Id, habbo.Username, room.RoomId, item.GetX, item.GetY, item.GetZ,
            petName, figure, item.GetZ + furni.Height, furni.Id, PetPackageFurniRotation, clock.GetUtcNow()));

        if (opened == null) {
            session.SendNotification("Oops, an error occoured. Please report this!");

            return;
        }

        var placed = new Item
        {
            Id = opened.FurniId,
            BaseItem = checked((int)furni.Id),
            OwnerId = item.OwnerId,
            RoomId = room.RoomId,
            Definition = furni,
            ExtraData = FurniExtraData.Load(furni, "", keepLegacy: true),
            GetX = item.GetX,
            GetY = item.GetY,
            GetZ = item.GetZ,
            Rotation = PetPackageFurniRotation
        };

        var handler = room.GetRoomItemHandler();
        handler.PublishStoredFloorItem(placed);
        handler.RemoveFurniture(session, item);

        var pet = opened.Pet;
        var speeches = new List<RandomSpeech>();
        var actor = room.GetRoomUserManager().DeployBot(new(pet.PetId, pet.RoomId, "pet", "freeroam", pet.Name, "", pet.Look,
            pet.X, pet.Y, pet.Z, 0, 0, 0, 0, 0, ref speeches, "", 0, pet.OwnerId, false, 0, false, 0), pet);
        session.Send(new CheckGnomeNameComposer(item.Id, petName, PetPackageNameError.None));
        (actor.BotAi as Plus.HabboHotel.Rooms.AI.Types.PetBot)?.Greet();
    }

    private const int PetPackageFurniRotation = 4;

    private static string RandomClothing() => Random.Shared.Next(1, 7) switch
    {
        2 => "5 0 -1 0 1 102 13 3 301 4 4 401 5 2 201 3",
        3 => "5 1 102 8 2 201 16 4 401 9 3 303 4 0 -1 6",
        4 => "5 0 -1 0 3 303 4 4 401 5 1 101 2 2 201 3",
        5 => "5 3 302 4 2 201 11 1 102 12 0 -1 28 4 401 24",
        6 => "5 4 402 5 3 302 21 0 -1 7 1 101 12 2 201 17",
        _ => "5 0 -1 0 4 402 5 3 301 4 1 101 2 2 201 3"
    };
}
