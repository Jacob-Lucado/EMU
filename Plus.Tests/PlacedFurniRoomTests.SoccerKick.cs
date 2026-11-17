using System.Collections.Concurrent;
using System.Reflection;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Games.Football;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    private readonly List<(double Delay, Action Tick)> _ballTicks = [];
    private int _ballChoice;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PickingUpABallEndsItsPreviousMove(bool kick)
    {
        var (ball, actor) = KickPitch(2, 2, 1);
        Step(actor, 2, 2, kick);
        var handler = _room.GetRoomItemHandler();
        Assert.True(handler.RemoveFurniture(null!, ball));
        Assert.True(handler.SetFloorItem(null!, ball, 3, 2, 0, true, false, false));

        if (kick) {
            RunBall(ball);
            Assert.Equal(3, ball.GetX);
        }
        else {
            Step(actor, 3, 2, true);
            Assert.Equal("55", ball.LegacyDataString);
        }
    }

    [Fact]
    public void BallFlightDefersPositionWritesUntilTheRoomSaves()
    {
        var store = new RecordingRoomItemStore();
        Set("_roomItemHandling", new RoomItemHandling(_room, store, TestRoomItemMetadataStore.Instance, TestGameClientManager.Empty, TestLanguageManager.RoomItems, TestItemRuntime.Interactors, TestItemRuntime.Travel, TestItemRuntime.Rewards));
        var (ball, kicker) = KickPitch(ballX: 2, ballY: 2, kickerX: 1);
        var placements = store.FloorPlacements;

        Step(kicker, 2, 2, lastStep: true);
        RunBall(ball);

        Assert.Equal(8, ball.GetX);
        Assert.Equal(placements, store.FloorPlacements);
        _room.GetRoomItemHandler().Dispose();
        var saved = Assert.Single(store.Saved);
        Assert.Equal((ball.Id, 8, 2, 2), (saved.Id, saved.X, saved.Y, saved.Rotation));
    }

    [Fact]
    public void LastStepOntoTheBallKicksItSixTilesWithFallingStatesAndDelays()
    {
        var (ball, kicker) = KickPitch(ballX: 2, ballY: 2, kickerX: 1);

        Step(kicker, 2, 2, lastStep: true);

        Assert.Equal((3, 2, "55", 2), BallAt(ball));
        var moves = RunBall(ball);
        Assert.Equal(new[] { (4, 2, "44"), (5, 2, "33"), (6, 2, "22"), (7, 2, "11"), (8, 2, "11") }, moves);
        Assert.Equal(new[] { 100.0, 125, 500.0 / 3, 250, 500 }, _ballDelays);
    }

    [Fact]
    public void WalkEndingOnTheBallKicksItWhenTheLastStepIsAnnounced()
    {
        IceTagSetup(v2: true);
        Set("_soccer", new Soccer(_room, (delay, tick) => _ballTicks.Add((delay, tick)), _ => _ballChoice));
        var ball = Furni(50, InteractionType.Football, WiredBoxType.None);
        ball.Definition.Walkable = true;
        ball.Definition.Height = 0;
        ball.ExtraData = new LegacyDataFormat { Data = "0" };
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null!, ball, 1, 1, 0, true, false, false));
        var actor = IcePlayer(_client, 0, 1);
        var actions = new RoomAvatarActionService(_interactionClock, Proxy<Plus.HabboHotel.Quests.IQuestManager>((method, _) => throw new InvalidOperationException(method)), TestNavigationRewards.Instance);

        actions.Move(_client, 1, 1);

        for (var i = 0; i < 4 && ball.GetX == 1; i++) {
            _room.ProcessRoom();
        }

        Assert.Equal((2, 1, "55", 2), BallAt(ball));
        Assert.True(actor.SetStep);
        Assert.Equal((1, 1), (actor.SetX, actor.SetY));
    }

    [Fact]
    public void KickedHockeyPuckSlidesNineTiles()
    {
        var (puck, kicker) = KickPitch(ballX: 1, ballY: 2, kickerX: 0);
        puck.Definition.ItemName = "es_puck";

        Step(kicker, 1, 2, lastStep: true);

        Assert.Equal((2, 2, "88", 2), BallAt(puck));
        var moves = RunBall(puck);
        Assert.Equal(new[] { (3, 2, "77"), (4, 2, "66"), (5, 2, "55"), (6, 2, "44"), (7, 2, "33"), (8, 2, "22"), (9, 2, "11"), (10, 2, "11") }, moves);
        Assert.Equal(new[] { 500.0 / 8, 500.0 / 7, 500.0 / 6, 100, 125, 500.0 / 3, 250, 500 }, _ballDelays);
    }

    [Fact]
    public void KickTurnsTheBallToTheWalkDirectionDiagonalsIncluded()
    {
        var (ball, kicker) = KickPitch(ballX: 4, ballY: 2, kickerX: 5, kickerY: 3);

        Step(kicker, 4, 2, lastStep: true);

        Assert.Equal((3, 1, "55", 7), BallAt(ball));
        Assert.Contains(ServerPacketHeader.ObjectUpdateComposer, _client.Sent);
    }

    [Fact]
    public void StepThroughTheBallDribblesItOneTile()
    {
        var (ball, kicker) = KickPitch(ballX: 2, ballY: 2, kickerX: 1);

        Step(kicker, 2, 2, lastStep: false);

        Assert.Equal((3, 2, "11", 2), BallAt(ball));
        Assert.Empty(RunBall(ball));
    }

    [Fact]
    public void LastStepOntoABallDribbledOnceLeavesItThere()
    {
        var (ball, kicker) = KickPitch(ballX: 2, ballY: 2, kickerX: 1);

        Step(kicker, 2, 2, lastStep: false);
        Step(kicker, 3, 2, lastStep: true);

        Assert.Equal((3, 2), (ball.GetX, ball.GetY));
        Assert.Empty(RunBall(ball));
    }

    [Fact]
    public void LastStepOntoABallDribbledTwiceDribblesItAgain()
    {
        var (ball, kicker) = KickPitch(ballX: 2, ballY: 2, kickerX: 1);

        Step(kicker, 2, 2, lastStep: false);
        Step(kicker, 3, 2, lastStep: false);
        Step(kicker, 4, 2, lastStep: true);

        Assert.Equal((5, 2, "11", 2), BallAt(ball));
        Assert.Empty(RunBall(ball));
    }

    [Fact]
    public void StepOffTheBallTapsItOneTileBack()
    {
        var (ball, kicker) = KickPitch(ballX: 4, ballY: 2, kickerX: 4);

        Step(kicker, 5, 2, lastStep: true);

        Assert.Equal((3, 2, "11", 6), BallAt(ball));
    }

    [Fact]
    public void StepThatDoesNotTouchTheBallLeavesItInPlace()
    {
        var (ball, kicker) = KickPitch(ballX: 4, ballY: 2, kickerX: 1);

        Step(kicker, 2, 2, lastStep: true);

        Assert.Equal((4, 2, "0", 0), BallAt(ball));
        Assert.Empty(_ballTicks);
    }

    [Fact]
    public void BallBouncesOffTheRoomEdgeAndLosesOneStep()
    {
        var (ball, kicker) = KickPitch(ballX: 9, ballY: 2, kickerX: 8);

        Step(kicker, 9, 2, lastStep: true);
        var moves = RunBall(ball);

        Assert.Equal(new[] { (11, 2, "44"), (10, 2, "22"), (9, 2, "11"), (8, 2, "11") }, moves);
        Assert.Equal(6, ball.Rotation);
        Assert.Equal(new[] { 100.0, 125, 250, 500 }, _ballDelays);
    }

    [Fact]
    public void KickIntoAnAdjacentBlockerBouncesBeforeTheFirstMove()
    {
        var (ball, kicker) = KickPitch(ballX: 5, ballY: 2, kickerX: 4);
        Blocker(60, 6, 2);

        Step(kicker, 5, 2, lastStep: true);

        Assert.Equal((4, 2, "44", 6), BallAt(ball));
    }

    [Theory]
    [InlineData(0, 8, "11")]
    [InlineData(1, 5, "33")]
    [InlineData(2, 3, "11")]
    public void BallMeetingAnAvatarPassesStopsOrBouncesAtRandom(int choice, int endX, string endState)
    {
        var (ball, kicker) = KickPitch(ballX: 2, ballY: 2, kickerX: 1);
        Player(2, 6, 2);
        _ballChoice = choice;

        Step(kicker, 2, 2, lastStep: true);
        RunBall(ball);

        Assert.Equal((endX, endState), (ball.GetX, ball.LegacyDataString));
    }

    [Fact]
    public void BallEnteringAGoalFromItsOpenSideScoresAndRollsOn()
    {
        var (ball, kicker) = KickPitch(ballX: 8, ballY: 2, kickerX: 9);
        var goal = Goal(61, 6, 2, rotation: 2, InteractionType.FootballGoalRed);
        var red = Goal(62, 0, 0, rotation: 2, InteractionType.Footballcounterred);
        var blue = Goal(63, 1, 0, rotation: 2, InteractionType.Footballcounterblue);
        var achievements = new List<string>();
        Set("_achievements", new TestRoomAchievements((_, group, _) => achievements.Add(group)));
        _client.Sent.Clear();

        Step(kicker, 8, 2, lastStep: true);
        var moves = RunBallUntil(ball, () => ball.GetX == 6);

        Assert.Equal((6, "1"), (moves[^1].Item1, goal.LegacyDataString));
        Assert.Equal(("1", "0"), (red.LegacyDataString, blue.LegacyDataString));
        Assert.Contains(ServerPacketHeader.ActionComposer, _client.Sent);
        Assert.Equal(["ACH_FootballGoalScored"], achievements);
        RunBall(ball);
        Assert.Equal(2, ball.GetX);
        Assert.Contains(2000.0, _ballDelays);
        Assert.Equal("0", goal.LegacyDataString);
    }

    [Fact]
    public void GoalBlocksTheBallFromTheBack()
    {
        var (ball, kicker) = KickPitch(ballX: 4, ballY: 2, kickerX: 3);
        var goal = Goal(61, 6, 2, rotation: 2, InteractionType.FootballGoalRed);
        var red = Goal(62, 0, 0, rotation: 2, InteractionType.Footballcounterred);

        Step(kicker, 4, 2, lastStep: true);
        RunBall(ball);

        Assert.True(ball.GetX < 6);
        Assert.Equal(("0", "0"), (goal.LegacyDataString, red.LegacyDataString));
    }

    private readonly List<double> _ballDelays = [];

    // A 12 x 5 open pitch with the door in the bottom left corner.
    private (Item Ball, RoomUser Kicker) KickPitch(int ballX, int ballY, int kickerX, int kickerY = -1)
    {
        Set("_gamemap", new Gamemap(_room, new RoomModel("pitch", 0, 4, 0, 0, "000000000000\r000000000000\r000000000000\r000000000000\r000000000000", 0, 0, false),
            TestLogging.Navigation, _roomSettings, TestGroupManager.Empty, TestNavigationDatabase.Instance, TestNavigationRewards.Instance));
        Set("_soccer", new Soccer(_room, (delay, tick) => _ballTicks.Add((delay, tick)), _ => _ballChoice));
        _room.GetGameMap().GenerateMaps();
        var ball = Furni(50, InteractionType.Football, WiredBoxType.None);
        ball.Definition.Walkable = true;
        ball.Definition.Height = 0;
        ball.ExtraData = new LegacyDataFormat { Data = "0" };
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null!, ball, ballX, ballY, 0, true, false, false));
        var kicker = Viewer(kickerX, kickerY < 0 ? ballY : kickerY);
        _client.Sent.Clear();

        return (ball, kicker);
    }

    // The announce runs while the avatar still stands on its tile, with the step target set.
    private void Step(RoomUser actor, int x, int y, bool lastStep)
    {
        (actor.SetStep, actor.SetX, actor.SetY) = (true, x, y);
        _room.GetSoccer().OnStepAnnounced(actor, x, y, lastStep);
        (actor.X, actor.Y, actor.SetStep) = (x, y, false);
    }

    private static (int, int, string, int) BallAt(Item ball) => (ball.GetX, ball.GetY, ball.LegacyDataString, ball.Rotation);

    private List<(int, int, string)> RunBall(Item ball) => RunBallUntil(ball, () => false);

    private List<(int, int, string)> RunBallUntil(Item ball, Func<bool> stop)
    {
        var moves = new List<(int, int, string)>();

        while (_ballTicks.Count > 0 && !stop()) {
            var (delay, tick) = _ballTicks[0];
            _ballTicks.RemoveAt(0);
            _ballDelays.Add(delay);
            var before = (ball.GetX, ball.GetY);
            tick();

            if ((ball.GetX, ball.GetY) != before) {
                moves.Add((ball.GetX, ball.GetY, ball.LegacyDataString));
            }
        }

        return moves;
    }

    private void Blocker(uint id, int x, int y)
    {
        var item = Furni(id, InteractionType.None, WiredBoxType.None);
        item.Definition.Height = 1;
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null!, item, x, y, 0, true, false, false));
    }

    private Item Goal(uint id, int x, int y, int rotation, InteractionType type)
    {
        var item = Furni(id, type, WiredBoxType.None);
        item.Definition.Height = 0.000001;
        item.ExtraData = new LegacyDataFormat { Data = "0" };
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null!, item, x, y, rotation, true, false, false));

        return item;
    }

    private RoomUser Player(int virtualId, int x, int y)
    {
        var user = new RoomUser(8, RoomId, virtualId, _room, null, TestChatEmotions.Unused, new TestRewardProgress()) { X = x, Y = y };
        var users = (ConcurrentDictionary<int, RoomUser>)typeof(RoomUserManager).GetField("_users", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_room.GetRoomUserManager())!;
        users.TryAdd(virtualId, user);

        return user;
    }

    // Tile (0,0) must be open floor, not the door, for a stray move there to be observable.
    private void OpenCornerPitch()
    {
        Set("_gamemap", new Gamemap(_room, new RoomModel("pitch", 0, 3, 0, 0, "0000\r0000\r0000\r0000", 0, 0, false),
            TestLogging.Navigation, _roomSettings, TestGroupManager.Empty, TestNavigationDatabase.Instance, TestNavigationRewards.Instance));
        _room.GetGameMap().GenerateMaps();
    }
}
