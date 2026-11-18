using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    private static readonly ItemDefinition SwapSaddle = new() { Id = 7100, ItemName = "horse_saddle1", InteractionType = InteractionType.HorseSaddle1, CustomParams = "15 4 9 0 77" };
    private static readonly ItemDefinition SwapSaddleStand = new() { Id = 4450, ItemName = "horse_saddle2", InteractionType = InteractionType.HorseSaddle2, CustomParams = "15 4 10 0 103" };

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, false)]
    [InlineData(9, true)]
    public void SaddleReplacementKeepsBothItems(int previous, bool failReturn)
    {
        var horse = (Pet)RuntimeHelpers.GetUninitializedObject(typeof(Pet));
        horse.PetId = 12;
        horse.Type = 15;
        horse.Race = "2";
        horse.Color = "ffffff";
        horse.Name = "horse";
        horse.OwnerName = "owner";
        horse.OwnerId = 7;
        horse.PetHair = -1;
        horse.HairDye = 1;
        horse.Saddle = previous;
        horse.ExperienceLevels = [100];
        var pets = (ConcurrentDictionary<int, RoomUser>)typeof(RoomUserManager)
            .GetField("_pets", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_room.GetRoomUserManager())!;
        pets[horse.PetId] = new RoomUser(0, RoomId, 3, _room, null, TestChatEmotions.Unused, TestRewardProgress.Unused) { PetData = horse };
        var stand = Furni(40, InteractionType.HorseSaddle2, WiredBoxType.None);
        stand.Definition = SwapSaddleStand;
        stand.UserId = 7;
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null!, stand, 2, 2, 0, true, false, false));
        var inventory = new FurnitureInventoryComponent([], []);
        _client.GetHabbo().Inventory = new InventoryComponent { Furniture = inventory };
        var items = Proxy<IItemDataManager>((method, _) => method == "get_Items"
            ? new Dictionary<uint, ItemDefinition> { [7100] = SwapSaddle, [4450] = SwapSaddleStand }
            : throw new InvalidOperationException(method));
        var factory = Proxy<IItemFactory>((method, args) => failReturn ? throw new InvalidOperationException("Could not return saddle") : method == "CreateSingleItemNullable" && args[2] is ItemDefinition definition
            ? new Item { Id = 77, OwnerId = 7, UserId = 7, Definition = definition }
            : throw new InvalidOperationException(method));
        var service = new HorseCustomizationService(null!, items, factory, new SwapStore(), new PetInformationService(TimeProvider.System));

        if (failReturn) {
            Assert.Throws<InvalidOperationException>(() => service.ApplyEffect(_room, _client, 40, horse.PetId));
            Assert.Equal(previous, horse.Saddle);
            Assert.Same(stand, _room.GetRoomItemHandler().GetItem(40));
            return;
        }

        service.ApplyEffect(_room, _client, 40, horse.PetId);

        Assert.Equal(10, horse.Saddle);
        Assert.Equal(previous == 9 ? SwapSaddle : SwapSaddleStand, Assert.Single(inventory.GetItems).Definition);
        Assert.Contains(ServerPacketHeader.FurniListAddComposer, _client.Sent);
    }

    private sealed class SwapStore : IHorseCustomizationStore
    {
        public void UpdatePet(int petId, string column, object value) { }
        public Item? ConsumeItem(int petId, HorseStyle style, uint itemId, uint roomId, int ownerId, int previousSaddle, SaddleRefund? refund) => refund?.Create(null!, null!);
        public Item RemoveSaddle(int petId, int saddle, SaddleRefund refund) => refund.Create(null!, null!);
    }
}
