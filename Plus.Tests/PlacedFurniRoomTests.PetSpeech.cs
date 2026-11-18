using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.Types;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Plus.HabboHotel.Users.Inventory.Pets;
using Xunit;

namespace Plus.Tests;

// A pet says one welcome line when it is placed and otherwise talks only with gestures.
public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void APlacedPetSaysOneWelcomeLineUnlessPetsAreMuted(bool muted, int lines)
    {
        Set("_roomUserManager", new RoomUserManager(_room, TestRoomUserStore.Instance, TimeProvider.System, new TestRewardProgress(), TestChatEmotions.Unused,
            new TestBotAiFactory((_, virtualId) => new PetBot(virtualId, new RecordingLocale(), new RecordingCommands(3))),
            TestGameClientManager.Empty, TestItemRuntime.Travel, TestItemRuntime.Settings));
        Viewer(0, 0);
        var pet = new Pet(81, 7, 0, "pixel", 1, "0", "ffffff", 0, 100, 100, 0,
            DateTimeOffset.FromUnixTimeMilliseconds(1609459200123), 0, 0, 0, 0, 0, 0, 0, "", "owner");
        _client.GetHabbo().Inventory = new InventoryComponent
        {
            Furniture = new FurnitureInventoryComponent([], []),
            Pets = new PetsInventoryComponent([pet])
        };
        _room.Behavior = RoomBehaviorSettings.Default with { MuteAllPets = muted };
        var settings = Proxy<Plus.Core.Settings.ISettingsManager>((method, args) =>
            method == "TryGetValue" && (string)args[0]! == "room.pets.placement_limit" ? "5" : null);
        _client.Sent.Clear();

        new PetPlacementService(new AcceptingPetStore(), TestGameClientManager.Empty, settings).Place(_room, _client, pet.PetId, 2, 2);

        Assert.True(_room.GetRoomUserManager().TryGetPet(pet.PetId, out _));
        Assert.Equal(lines, _client.Sent.Count(header => header == ServerPacketHeader.ChatComposer));
    }

    [Fact]
    public void APetNeverChatsOnItsOwn()
    {
        LegacyRider();
        var (petUser, _) = HighLevelPet(100);
        var ai = new PetBot(petUser.VirtualId, new RecordingLocale(), new RecordingCommands(3));
        ai.Init(50, petUser.VirtualId, RoomId, petUser, _room);
        _client.Sent.Clear();

        for (var tick = 0; tick < 600; tick++) {
            ai.OnTimerTick();
        }

        Assert.DoesNotContain(ServerPacketHeader.ChatComposer, _client.Sent);
    }

    [Theory]
    [InlineData(6, 100)]
    [InlineData(99, 100)]
    [InlineData(3, 5)]
    public void APetAnswersACommandWithoutChat(int command, int energy)
    {
        var owner = LegacyRider();
        var (petUser, _) = HighLevelPet(energy);
        var ai = new PetBot(petUser.VirtualId, new RecordingLocale(), new RecordingCommands(command));
        ai.Init(50, petUser.VirtualId, RoomId, petUser, _room);
        _client.Sent.Clear();

        ai.OnUserSay(owner, "horse do it");

        Assert.DoesNotContain(ServerPacketHeader.ChatComposer, _client.Sent);
    }

    [Fact]
    public void ALowLevelPetThatRefusesACommandDoesNotChat()
    {
        var owner = LegacyRider();
        var petUser = LegacyHorse(2, 1);
        var pet = Assert.IsType<Pet>(petUser.PetData);
        pet.ExperienceLevels = Enumerable.Range(1, 20).Select(level => level * 1_000_000).ToArray();
        var ai = new PetBot(petUser.VirtualId, new RecordingLocale(), new RecordingCommands(3));
        ai.Init(50, petUser.VirtualId, RoomId, petUser, _room);
        _client.Sent.Clear();

        for (var attempt = 0; attempt < 40; attempt++) {
            pet.Energy = 100;
            ai.OnUserSay(owner, "horse sit");
        }

        Assert.DoesNotContain(ServerPacketHeader.ChatComposer, _client.Sent);
    }

    private sealed class AcceptingPetStore : IPetRoomStore
    {
        public bool TryMove(PetRoomMove move) => true;
    }
}
