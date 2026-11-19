using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.Types;
using Xunit;

namespace Plus.Tests;

// Pets play with the pet ball like on Habbo: now and then a pet walks onto the ball by itself and plays with it.
// The pet shows "pla" (a horse kicks with "kck") and a smile, says a playing line, and the ball shows state 1
// for three and a half seconds.
public partial class PlacedFurniRoomTests
{
    private static readonly string[] PlayingLines =
        ["I'm having a blast!", "I can't stop playing!", "See! I knew this would be fun!", "Wooo! Yay!", "Whee!"];

    [Theory]
    [InlineData("pet_toy_ball", true)]
    [InlineData("nest_plow_red", false)]
    [InlineData("waterbowl*1", false)]
    public void ThePetBallIsAToy(string name, bool toy)
    {
        Assert.Equal(toy, PetToy.Is(new ItemDefinition { ItemName = name }));
    }

    [Theory]
    [InlineData(29, "pla")]
    [InlineData(15, "kck")]
    public void APetPlaysWithTheBallByItself(int type, string posture)
    {
        LegacyRider();
        var pet = CommandPet(type, 17_600, 420);
        var petUser = PetUser(pet);
        var ball = Ball(95, 3, 3);
        var ai = CommandBot(petUser, -1);
        _client.Packets.Clear();

        PlayUntilTheBallMoves(ai, petUser, ball);

        Assert.Equal("1", ball.LegacyDataString);
        Assert.Contains(posture, petUser.Statusses.Keys);
        Assert.Equal("sml", petUser.Statusses.GetValueOrDefault("gst"));
        Assert.Contains(Assert.Single(ChatLines()), PlayingLines);
    }

    [Fact]
    public void TheBallRestsAgainAfterThreeAndAHalfSeconds()
    {
        LegacyRider();
        var pet = CommandPet(29, 17_600, 420);
        var petUser = PetUser(pet);
        var ball = Ball(95, 3, 3);
        var ai = CommandBot(petUser, -1);

        PlayUntilTheBallMoves(ai, petUser, ball);

        for (var tick = 0; tick < 6; tick++) {
            _room.GetRoomItemHandler().OnCycle();
        }

        Assert.Equal("1", ball.LegacyDataString);
        _room.GetRoomItemHandler().OnCycle();
        Assert.Equal("0", ball.LegacyDataString);
    }

    [Fact]
    public void AnotherPlayRestartsTheBallTimer()
    {
        var handler = _room.GetRoomItemHandler();
        var ball = Ball(95, 3, 3);
        handler.PlayPetBall(ball);
        handler.OnCycle();
        handler.PlayPetBall(ball);

        for (var tick = 0; tick < 6; tick++) {
            handler.OnCycle();
        }

        Assert.Equal("1", ball.LegacyDataString);
        handler.OnCycle();
        Assert.Equal("0", ball.LegacyDataString);
    }

    [Fact]
    public void RemovingTheBallCancelsItsReset()
    {
        var handler = _room.GetRoomItemHandler();
        var ball = Ball(95, 3, 3);
        handler.PlayPetBall(ball);
        Assert.True(handler.DeleteFurniture(ball));

        for (var tick = 0; tick < 7; tick++) {
            handler.OnCycle();
        }

        Assert.Equal("1", ball.LegacyDataString);
    }

    [Fact]
    public void IdlePetTicksAllocateNoMemory()
    {
        var user = PetUser(CommandPet(15, 17_600, 420));
        var bytes = 0L;

        for (var sample = 0; sample < 128; sample++) {
            var ai = CommandBot(user, -1);
            ai.OnTimerTick();
            var before = GC.GetAllocatedBytesForCurrentThread();

            for (var tick = 0; tick < 8; tick++) {
                ai.OnTimerTick();
            }

            bytes += GC.GetAllocatedBytesForCurrentThread() - before;
        }

        Assert.Equal(0, bytes);
    }

    private static void PlayUntilTheBallMoves(PetBot ai, RoomUser petUser, Item ball)
    {
        for (var tick = 0; tick < 6000 && ball.LegacyDataString != "1"; tick++) {
            ai.OnTimerTick();

            if (WalkTarget(petUser) == (3, 3) && petUser.Coordinate != new System.Drawing.Point(3, 3)) {
                ArriveAt(ai, petUser, 3, 3);
            }
        }
    }

    private Item Ball(uint id, int x, int y)
    {
        var ball = Furni(id, InteractionType.None, WiredBoxType.None);
        ball.Definition.ItemName = "pet_toy_ball";
        ball.Definition.FurniLine = "pet_accessories";
        ball.Definition.Walkable = true;
        ball.ExtraData = new LegacyDataFormat { Data = "0" };
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null, ball, x, y, 0, true, false, false));

        return ball;
    }
}
