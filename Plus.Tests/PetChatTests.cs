using System.Buffers.Binary;
using System.Text;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.Types;
using Plus.HabboHotel.Users;
using Xunit;

namespace Plus.Tests;

// A pet answers in chat when it refuses a command, when it gets a command it has
// not learned yet and when it is scratched. Each pet type has its own lines, and a muted room hears nothing.
public partial class PlacedFurniRoomTests
{
    private static readonly string[] HorseRefusals =
    [
        "Talk to the hoof!", "Well, I'm not going to do it...", "Maybe I'll do it later", "Hey, manure happens.",
        "Yes, yes, I'll do it later", "I'd rather not do it...", "I wish I could do as you ask, but I cannot."
    ];

    [Fact]
    public void ARefusingHorseSaysAHorseRefusalLine()
    {
        var owner = LegacyRider();
        var pet = ChatPet(15, 0, 120);
        var petUser = PetUser(pet);
        var ai = ChatBot(petUser, 44);
        _client.Packets.Clear();

        for (var attempt = 0; attempt < 60; attempt++) {
            pet.Energy = 120;
            pet.Experience = 0;
            ai.OnUserSay(owner, "horse wag tail");
            DrainPetCommands();
        }

        var lines = ChatLines();
        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.Contains(line, HorseRefusals));
    }

    [Fact]
    public void AHorseSaysItsPuzzledLineForACommandItHasNotLearned()
    {
        var owner = LegacyRider();
        var pet = ChatPet(15, 0, 120);
        var petUser = PetUser(pet);
        _client.Packets.Clear();

        ChatBot(petUser, 2).OnUserSay(owner, "horse down");
        DrainPetCommands();

        Assert.Equal("puz", petUser.Statusses.GetValueOrDefault("gst"));
        var line = Assert.Single(ChatLines());
        Assert.StartsWith("Billy McCoy says...", line);
    }

    [Fact]
    public void AnObeyingPetSaysNothing()
    {
        var owner = LegacyRider();
        var pet = ChatPet(15, 17_600, 300);
        var petUser = PetUser(pet);
        _client.Packets.Clear();

        ChatBot(petUser, 44).OnUserSay(owner, "horse wag tail");
        DrainPetCommands();

        Assert.Equal(17_605, pet.Experience);
        Assert.Empty(ChatLines());
    }

    [Fact]
    public void APetInAMutedRoomDoesNotAnswerInChat()
    {
        var owner = LegacyRider();
        var pet = ChatPet(15, 0, 120);
        var petUser = PetUser(pet);
        _room.Behavior = RoomBehaviorSettings.Default with { MuteAllPets = true };
        var ai = ChatBot(petUser, 2);
        _client.Packets.Clear();

        ai.OnUserSay(owner, "horse down");
        DrainPetCommands();

        Assert.Equal("puz", petUser.Statusses.GetValueOrDefault("gst"));
        Assert.Empty(ChatLines());
    }

    [Fact]
    public void APetTypeWithoutLinesOnlyLooksPuzzled()
    {
        var owner = LegacyRider();
        var pet = ChatPet(13, 0, 120);
        var petUser = PetUser(pet);
        _client.Packets.Clear();

        ChatBot(petUser, 13).OnUserSay(owner, "horse nest");

        Assert.Equal("puz", petUser.Statusses.GetValueOrDefault("gst"));
        Assert.Empty(ChatLines());
    }

    [Fact]
    public void AScratchedHorseSaysAHappyLine()
    {
        LegacyRider();
        var pet = ChatPet(15, 0, 120);
        _client.GetHabbo().HabboStats = new HabboStats(0, 0, 0, 0, 0, 3, 0, 0, 0, "old");
        var service = new PetRespectService(Proxy<IAchievementManager>((_, _) => null), Proxy<IQuestManager>((_, _) => null),
            Proxy<IRewardTrackManager>((_, _) => null));
        _client.Packets.Clear();

        service.Respect(_room, _client, pet.PetId);

        var line = Assert.Single(ChatLines());
        Assert.Contains(line, new[] { "I'm happier than a turkey after Thanksgiving.", "*Happy*" });
    }

    private Pet ChatPet(int type, int experience, int energy)
    {
        var pet = Assert.IsType<Pet>(LegacyHorse(2, 1).PetData);
        pet.Type = type;
        pet.ExperienceLevels = [100, 200, 400, 600, 900, 1300, 1800, 2400, 3200, 4300, 7200, 8500, 10100, 13300, 17500, 23000, 51900, 75000, 128000, 150000];
        pet.Experience = experience;
        pet.Energy = energy;

        return pet;
    }

    private PetBot ChatBot(RoomUser petUser, int command)
    {
        var ai = new PetBot(petUser.VirtualId, new RecordingLocale(), new RecordingCommands(command));
        ai.Init(50, petUser.VirtualId, RoomId, petUser, _room);
        petUser.BotAi = ai;

        return ai;
    }

    private List<string> ChatLines() => _client.Packets.Where(packet => packet.Header == ServerPacketHeader.ChatComposer)
        .Select(packet =>
        {
            var length = BinaryPrimitives.ReadUInt16BigEndian(packet.Body.AsSpan(4));

            return Encoding.UTF8.GetString(packet.Body, 6, length);
        }).ToList();
}
