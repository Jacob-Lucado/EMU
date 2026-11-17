using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.Types;
using Plus.HabboHotel.Rooms.Chat.Pets.Locale;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Plus.HabboHotel.Users.Inventory.Pets;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Fact]
    public void APlacedPetGreetsItsOwnerByName()
    {
        Set("_roomUserManager", new RoomUserManager(_room, TestRoomUserStore.Instance, TimeProvider.System, new TestRewardProgress(), TestChatEmotions.Unused,
            new TestBotAiFactory((_, virtualId) => new PetBot(virtualId, new OwnerLineLocale(), new RecordingCommands(3))),
            TestGameClientManager.Empty, TestItemRuntime.Travel, TestItemRuntime.Settings));
        Viewer(0, 0);
        var pet = new Pet(81, 7, 0, "pixel", 29, "0", "ffffff", 0, 100, 100, 0,
            DateTimeOffset.FromUnixTimeMilliseconds(1609459200123), 0, 0, 0, 0, 0, 0, 0, "", "owner");
        _client.GetHabbo().Inventory = new InventoryComponent
        {
            Furniture = new FurnitureInventoryComponent([], []),
            Pets = new PetsInventoryComponent([pet])
        };
        var settings = Proxy<Plus.Core.Settings.ISettingsManager>((method, args) =>
            method == "TryGetValue" && (string)args[0]! == "room.pets.placement_limit" ? "5" : null);

        new PetPlacementService(new AcceptingPetStore(), TestGameClientManager.Empty, settings).Place(_room, _client, pet.PetId, 2, 2);

        Assert.Equal(["Knock, knock. Who's there? owner's here!"], ChatTexts(_client, ServerPacketHeader.ChatComposer));
    }

    private sealed class OwnerLineLocale : IPetLocale
    {
        public void Init() { }
        public string[] GetValue(string key) => key == "welcome.speech.pet29" ? ["Knock, knock. Who's there? %owner%'s here!"] : [];
    }
}
