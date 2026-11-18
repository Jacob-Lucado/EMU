using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.Types;
using Xunit;

namespace Plus.Tests;

// Habbo pets chat on their own with their type's lines, at the type's own pace: a kitten or a piglet says a lonely
// line about every 9 seconds. A muted room hears nothing, and a type without known lines stays silent.
public partial class PlacedFurniRoomTests
{
    private static readonly string[] LonelyLines =
        ["*Where's my owner?*", "*I miss you.*", "*Come out, come out, wherever you are!*", "*I'm lonely without you.*", "*Where did everybody go again?*"];

    [Theory]
    [InlineData(28)]
    [InlineData(30)]
    public void AKittenOrAPigletSaysALonelyLineOnItsOwn(int type)
    {
        LegacyRider();
        var ai = ChatterBot(type, 0.0);
        _client.Packets.Clear();

        ai.OnTimerTick();

        Assert.Contains(Assert.Single(ChatLines()), LonelyLines);
    }

    [Theory]
    [InlineData(0, "*Thirsty*")]
    [InlineData(1, "Food, please")]
    [InlineData(21, "Water takes the easiest path downward.")]
    [InlineData(22, "A lie told often enough becomes the truth.")]
    public void APetSaysItsOwnTypesLinesOnItsOwn(int type, string line)
    {
        LegacyRider();
        var petUser = PetUser(ChatPet(type, 0, 120));
        _client.Packets.Clear();

        for (var pick = 0; pick < 20; pick++) {
            ChatterBot(petUser, 0.0, pick).OnTimerTick();
        }

        Assert.Contains(line, ChatLines());
    }

    [Theory]
    [InlineData(0.05, 1)]
    [InlineData(0.06, 0)]
    public void AKittenSaysALineAboutEveryNineSeconds(double roll, int lines)
    {
        LegacyRider();
        var ai = ChatterBot(28, roll);
        _client.Packets.Clear();

        ai.OnTimerTick();

        Assert.Equal(lines, ChatLines().Count);
    }

    [Fact]
    public void APetInAMutedRoomSaysNothingOnItsOwn()
    {
        LegacyRider();
        var ai = ChatterBot(28, 0.0);
        _room.Behavior = RoomBehaviorSettings.Default with { MuteAllPets = true };
        _client.Packets.Clear();

        ai.OnTimerTick();

        Assert.Empty(ChatLines());
    }

    [Theory]
    [InlineData(15)]
    [InlineData(17)]
    [InlineData(29)]
    [InlineData(2)]
    public void APetTypeWithoutKnownLinesStaysSilent(int type)
    {
        LegacyRider();
        var ai = ChatterBot(type, 0.0);
        _client.Packets.Clear();

        for (var tick = 0; tick < 100; tick++) {
            ai.OnTimerTick();
        }

        Assert.Empty(ChatLines());
    }

    private PetBot ChatterBot(int type, double roll) => ChatterBot(PetUser(ChatPet(type, 0, 120)), roll, 0);

    private PetBot ChatterBot(RoomUser petUser, double roll, int pick)
    {
        var ai = new PetBot(petUser.VirtualId, new RecordingLocale(), new RecordingCommands(3), new FixedRandom(roll, pick));
        ai.Init(50, petUser.VirtualId, RoomId, petUser, _room);
        petUser.BotAi = ai;

        return ai;
    }

    private sealed class FixedRandom(double roll, int pick) : Random
    {
        public override double NextDouble() => roll;

        public override int Next(int maxValue) => Math.Min(pick, maxValue - 1);

        public override int Next(int minValue, int maxValue) => Math.Clamp(minValue + pick, minValue, maxValue - 1);
    }
}
