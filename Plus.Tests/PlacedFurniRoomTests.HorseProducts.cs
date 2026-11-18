using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Fact]
    public void HorseProductWorksStraightFromTheInventory()
    {
        var horse = PlaceHorse(ownerId: 7);
        Inventory(new InventoryItem { Id = 40, OwnerId = 7, Definition = HairDye() });
        var store = new ConsumedProducts();

        Service(store).ApplyEffect(_room, _client, 40, horse.PetId);

        Assert.Equal(81, horse.HairDye);
        Assert.Equal((horse.PetId, 40u, 0u), Assert.Single(store.Consumed));
        Assert.Null(Assert.IsType<InventoryComponent>(_client.GetHabbo().Inventory).Furniture.GetItem(40));
        Assert.Contains(ServerPacketHeader.FurniListRemoveComposer, _client.Sent);
    }

    [Fact]
    public void HorseProductFromTheInventoryNeedsTheOwnersHorse()
    {
        var horse = PlaceHorse(ownerId: 8);
        Inventory(new InventoryItem { Id = 40, OwnerId = 7, Definition = HairDye() });
        var store = new ConsumedProducts();

        Service(store).ApplyEffect(_room, _client, 40, horse.PetId);

        Assert.Equal(0, horse.HairDye);
        Assert.Empty(store.Consumed);
        Assert.NotNull(Assert.IsType<InventoryComponent>(_client.GetHabbo().Inventory).Furniture.GetItem(40));
    }

    private static ItemDefinition HairDye() => new()
    {
        Id = 4973,
        Type = ItemType.Floor,
        ItemName = "horse_hairdye_13",
        InteractionType = InteractionType.HorseHairDye,
        CustomParams = "15 2,3 81,81",
    };

    private HorseCustomizationService Service(IHorseCustomizationStore store) =>
        new(null!, null!, null!, store, new PetInformationService(TimeProvider.System));

    private Pet PlaceHorse(int ownerId)
    {
        var pet = (Pet)RuntimeHelpers.GetUninitializedObject(typeof(Pet));
        pet.PetId = 12;
        pet.Type = 15;
        pet.Race = "7";
        pet.Color = "ffffff";
        pet.Name = "horse";
        pet.OwnerName = "owner";
        pet.OwnerId = ownerId;
        pet.PetHair = -1;
        pet.ExperienceLevels = [100];
        var user = new RoomUser(0, RoomId, 3, _room, null, TestChatEmotions.Unused, TestRewardProgress.Unused) { PetData = pet };
        var pets = (ConcurrentDictionary<int, RoomUser>)typeof(RoomUserManager)
            .GetField("_pets", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_room.GetRoomUserManager())!;
        pets[pet.PetId] = user;

        return pet;
    }

    private sealed class ConsumedProducts : IHorseCustomizationStore
    {
        public List<(int PetId, uint ItemId, uint RoomId)> Consumed { get; } = [];
        public void UpdatePet(int petId, string column, object value) { }
        public Item RemoveSaddle(int petId, int saddle, SaddleRefund refund) => throw new NotSupportedException();
        public Item? ConsumeItem(int petId, HorseStyle style, uint itemId, uint roomId, int ownerId, int previousSaddle, SaddleRefund? refund)
        {
            Consumed.Add((petId, itemId, roomId));

            return null;
        }
    }
}
