using System.Buffers.Binary;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.Types;
using Xunit;

namespace Plus.Tests;

// Habbo pet commands use the client's command ids (pet.command.N). Each pet type has its own list; a level 1
// pet knows the first few and learns the next one at every level. An obeyed command gives experience and costs
// 5 energy, a command the pet has not learned yet gets a puzzled look, and a command outside its list is ignored.
public partial class PlacedFurniRoomTests
{
    private static readonly int[] HorseCommands = [0, 14, 43, 44, 2, 3, 6, 7, 10, 12, 11, 15, 16, 24, 25, 26, 45];
    private static readonly int[] DogCommands = [46, 0, 1, 14, 43, 2, 3, 4, 17, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 16, 24, 25, 26];

    [Theory]
    [InlineData(15, 0, 4)]
    [InlineData(15, 110, 5)]
    [InlineData(15, 17_000, 17)]
    [InlineData(0, 0, 5)]
    public void TrainingPanelListsThePetTypesCommandsAndTheOnesItLearned(int type, int experience, int learned)
    {
        LegacyRider();
        var pet = CommandPet(type, experience, 100);
        _client.Packets.Clear();

        new PetInformationService(TimeProvider.System).SendTrainingPanel(_client, pet.PetId);

        var all = type == 15 ? HorseCommands : DogCommands;
        var (petId, commands, enabled) = TrainingPanel(Assert.Single(_client.Packets, packet => packet.Header == ServerPacketHeader.PetTrainingPanelComposer).Body);
        Assert.Equal(pet.PetId, petId);
        Assert.Equal(all, commands);
        Assert.Equal(all.Take(learned), enabled);
    }

    [Fact]
    public void TrainingPanelIsOnlyForTheOwner()
    {
        LegacyRider();
        var pet = CommandPet(15, 0, 100);
        pet.OwnerId = 99;
        _client.Packets.Clear();

        new PetInformationService(TimeProvider.System).SendTrainingPanel(_client, pet.PetId);

        Assert.DoesNotContain(_client.Packets, packet => packet.Header == ServerPacketHeader.PetTrainingPanelComposer);
    }

    [Theory]
    [InlineData(44, "wag")]
    [InlineData(2, "lay")]
    public void AnObeyedCommandGivesFiveExperienceAndCostsFiveEnergy(int command, string posture)
    {
        var owner = LegacyRider();
        var pet = CommandPet(15, 17_600, 300);
        var petUser = PetUser(pet);

        Command(petUser, owner, command);

        Assert.Equal((17_605, 295), (pet.Experience, pet.Energy));
        Assert.Contains(posture, petUser.Statusses.Keys);
    }

    [Fact]
    public void FreeGivesNoExperience()
    {
        var owner = LegacyRider();
        var pet = CommandPet(15, 17_600, 300);

        Command(PetUser(pet), owner, 0);

        Assert.Equal(17_600, pet.Experience);
    }

    [Fact]
    public void ACommandThePetHasNotLearnedGetsAPuzzledLook()
    {
        var owner = LegacyRider();
        var pet = CommandPet(15, 0, 120);
        var petUser = PetUser(pet);
        _client.Packets.Clear();

        Command(petUser, owner, 2);

        Assert.Equal("puz", petUser.Statusses.GetValueOrDefault("gst"));
        Assert.Equal((0, 120), (pet.Experience, pet.Energy));
        Assert.DoesNotContain(_client.Packets, packet => packet.Header == ServerPacketHeader.AddExperiencePointsComposer);
    }

    [Fact]
    public void ACommandOutsideThePetsListIsIgnored()
    {
        var owner = LegacyRider();
        var pet = CommandPet(15, 17_600, 300);
        var petUser = PetUser(pet);
        _client.Packets.Clear();

        Command(petUser, owner, 1);

        Assert.Empty(petUser.Statusses);
        Assert.Equal((17_600, 300), (pet.Experience, pet.Energy));
        Assert.Empty(_client.Packets);
    }

    [Fact]
    public void AnUnknownWordIsIgnored()
    {
        var owner = LegacyRider();
        var pet = CommandPet(15, 17_600, 300);
        var petUser = PetUser(pet);
        _client.Packets.Clear();

        Command(petUser, owner, -1);

        Assert.Equal((17_600, 300), (pet.Experience, pet.Energy));
        Assert.Empty(_client.Packets);
    }

    private Pet CommandPet(int type, int experience, int energy)
    {
        var pet = Assert.IsType<Pet>(LegacyHorse(2, 1).PetData);
        pet.Type = type;
        pet.ExperienceLevels = [100, 200, 400, 600, 900, 1300, 1800, 2400, 3200, 4300, 7200, 8500, 10100, 13300, 17500, 23000, 51900, 75000, 128000, 150000];
        pet.Experience = experience;
        pet.Energy = energy;

        return pet;
    }

    private RoomUser PetUser(Pet pet)
    {
        Assert.True(_room.GetRoomUserManager().TryGetPet(pet.PetId, out var petUser));
        petUser.Statusses.Clear();

        return petUser;
    }

    private void Command(RoomUser petUser, RoomUser owner, int command)
    {
        var ai = new PetBot(petUser.VirtualId, new RecordingLocale(), new RecordingCommands(command));
        ai.Init(50, petUser.VirtualId, RoomId, petUser, _room);
        ai.OnUserSay(owner, "horse do it");
        DrainPetCommands();
    }

    private void DrainPetCommands() => _room.RunFastPass(_room.GetGameMap().Navigation.DrainCommands);

    private static (int PetId, int[] Commands, int[] Enabled) TrainingPanel(byte[] body)
    {
        var offset = 0;
        int Next()
        {
            var value = BinaryPrimitives.ReadInt32BigEndian(body.AsSpan(offset));
            offset += 4;

            return value;
        }

        var petId = Next();
        var commands = Enumerable.Range(0, Next()).Select(_ => Next()).ToArray();
        var enabled = Enumerable.Range(0, Next()).Select(_ => Next()).ToArray();

        return (petId, commands, enabled);
    }
}
