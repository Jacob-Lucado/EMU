using System.Text;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Xunit;

namespace Plus.Tests;

// On Habbo a pet that levels up tells its owner with a level notification and a fresh command list,
// and says "Leveling up" in the room. Level 5 needs 900 experience.
public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData(599, 4, 600)]
    [InlineData(787, 5, 900)]
    [InlineData(900, 6, 1300)]
    public void LevelFiveEndsAtNineHundredExperience(int experience, int level, int goal)
    {
        var pet = new Pet(81, 7, 0, "pixel", 15, "2", "ffffff", experience, 120, 100, 0, null, 0, 0, 0, 0, 0, 0, 0, "", "owner");

        Assert.Equal((level, goal), (pet.Level, pet.ExperienceGoal));
    }

    [Fact]
    public void LevelUpNotifiesTheOwnerAndSendsTheNewCommandList()
    {
        LegacyRider();
        var pet = LevellingHorse(95);
        _client.Packets.Clear();

        pet.Addexperience(5);

        Assert.Equal(2, pet.Level);
        Assert.Single(_client.Packets, packet => packet.Header == ServerPacketHeader.PetLevelNotificationComposer);
        Assert.Single(_client.Packets, packet => packet.Header == ServerPacketHeader.PetTrainingPanelComposer);
    }

    [Fact]
    public void LevelUpMakesThePetSayLevelingUp()
    {
        LegacyRider();
        var pet = LevellingHorse(95);
        _client.Packets.Clear();

        pet.Addexperience(5);

        var chats = _client.Packets.Where(packet => packet.Header == ServerPacketHeader.ChatComposer).Select(packet => Encoding.UTF8.GetString(packet.Body)).ToList();
        Assert.Single(chats);
        Assert.Contains("Leveling up", chats[0]);
    }

    [Fact]
    public void ExperienceWithoutLevelUpSendsNoLevelPackets()
    {
        LegacyRider();
        var pet = LevellingHorse(10);
        _client.Packets.Clear();

        pet.Addexperience(5);

        Assert.DoesNotContain(_client.Packets, packet => packet.Header is ServerPacketHeader.PetLevelNotificationComposer or ServerPacketHeader.PetTrainingPanelComposer or ServerPacketHeader.ChatComposer);
    }

    private Pet LevellingHorse(int experience)
    {
        var horse = LegacyHorse(2, 1);
        var pet = Assert.IsType<Pet>(horse.PetData);
        pet.Detach(_room);
        pet.Attach(_room, new TestGameClientManager(id => id == 7 ? _client : null), new TestRewardProgress());
        pet.ExperienceLevels = [100, 200, 400, 600, 900, 1300];
        pet.Experience = experience;

        return pet;
    }
}
