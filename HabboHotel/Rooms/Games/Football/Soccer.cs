using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms.Games.Teams;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Rooms.PathFinding;

namespace Plus.HabboHotel.Rooms.Games.Football;

public class Soccer
{
    private const double GoalFlashMilliseconds = 2000;
    private ConcurrentDictionary<uint, Item> _balls;
    private readonly Dictionary<uint, Flight> _flights = [];
    private readonly Dictionary<uint, DribbleStreak> _dribbles = [];
    private readonly Action<double, Action> _schedule;
    private readonly Func<int, int> _random;
    private Item[] _gates;
    private Room _room;

    /// <param name="schedule">Runs a ball tick after the given milliseconds, on the room's own pass.</param>
    /// <param name="random">Returns a number from 0 to the given bound, exclusive.</param>
    public Soccer(Room room, Action<double, Action>? schedule = null, Func<int, int>? random = null)
    {
        _room = room;
        _gates = new Item[4];
        _balls = new();
        _schedule = schedule ?? ((delay, tick) => WiredWakeClock.Shared.After(delay, () => Task.Run(() => room.RunFastPass(tick))));
        _random = random ?? Random.Shared.Next;
        GameIsStarted = false;
    }

    public bool GameIsStarted { get; private set; }

    public void StopGame(bool triggeredByUser = false)
    {
        GameIsStarted = false;

        if (!triggeredByUser) {
            _room.GetWired().TriggerEvent(WiredBoxType.TriggerGameEnds, null);
        }
    }

    public void StartGame()
    {
        GameIsStarted = true;
    }

    public void AddBall(Item item)
    {
        _balls.TryAdd(item.Id, item);
    }

    public void RemoveBall(uint itemId)
    {
        _balls.TryRemove(itemId, out _);

        if (_room.GetRoomItemHandler().GetItem(itemId) == null) {
            _flights.Remove(itemId);
            _dribbles.Remove(itemId);
        }
    }

    /// <summary>
    /// A ball reacts when a step into its tile is announced. The last step of a walk kicks it, any other step dribbles it one tile.
    /// A step off the ball taps it one tile back.
    /// </summary>
    public void OnStepAnnounced(RoomUser actor, int x, int y, bool lastStep)
    {
        if (_room == null || actor.IsBot) {
            return;
        }

        var direction = Rotation.Calculate(actor.X, actor.Y, x, y);

        foreach (var ball in _balls.Values.ToList()) {
            if (ball.GetX == x && ball.GetY == y) {
                Touch(actor, ball, direction, lastStep);
            }
            else if (ball.GetX == actor.X && ball.GetY == actor.Y && (x, y) != (actor.X, actor.Y)) {
                _dribbles.Remove(ball.Id);
                Launch(ball, new((direction + 4) % 8, 1, actor));
            }
        }
    }

    // A walk that dribbled the ball once and then ends on it keeps the ball under the avatar.
    private void Touch(RoomUser actor, Item ball, int direction, bool lastStep)
    {
        var streak = _dribbles.GetValueOrDefault(ball.Id);
        var count = streak != null && ReferenceEquals(streak.Actor, actor) && (streak.X, streak.Y) == (ball.GetX, ball.GetY)
            && (streak.FromX, streak.FromY) == (actor.X, actor.Y) ? streak.Count : 0;
        _dribbles.Remove(ball.Id);

        if (lastStep && count == 0) {
            Launch(ball, new(direction, KickPower(ball), actor));

            return;
        }

        if (lastStep && count == 1) {
            _flights.Remove(ball.Id);

            return;
        }

        var (fromX, fromY) = (ball.GetX, ball.GetY);
        Launch(ball, new(direction, 1, actor));

        if ((ball.GetX, ball.GetY) != (fromX, fromY)) {
            _dribbles[ball.Id] = new(actor, count + 1, ball.GetX, ball.GetY, fromX, fromY);
        }
    }

    // A hockey puck slides nine tiles, a ball rolls six.
    private static int KickPower(Item ball) => ball.Definition.ItemName.StartsWith("es_puck", StringComparison.Ordinal) ? 9 : 6;

    private void Launch(Item ball, Flight flight)
    {
        _flights[ball.Id] = flight;
        Advance(ball, flight);
    }

    private void Tick(Item ball, Flight flight)
    {
        if (_room == null || !_flights.TryGetValue(ball.Id, out var current) || !ReferenceEquals(current, flight)
            || !ReferenceEquals(_room.GetRoomItemHandler().GetItem(ball.Id), ball)) {
            return;
        }

        Advance(ball, flight);
    }

    // A blocked tile turns the ball around and costs one extra step. An avatar in the way lets it pass, stops it or bounces it at random.
    private void Advance(Item ball, Flight flight)
    {
        for (var turn = 0; turn < 2; turn++) {
            var step = Rotation.Offset(flight.Direction);
            var (x, y) = (ball.GetX + step.X, ball.GetY + step.Y);
            var goal = GoalAt(x, y);
            var tile = goal != null ? goal.Rotation == (flight.Direction + 4) % 8 ? Tile.Open : Tile.Blocked : Classify(x, y);

            if (tile == Tile.Avatar) {
                tile = _random(3) switch
                {
                    0 => Tile.Open,
                    1 => Tile.Stop,
                    _ => Tile.Blocked
                };
            }

            if (tile == Tile.Stop) {
                break;
            }

            if (tile == Tile.Blocked) {
                flight.Direction = (flight.Direction + 4) % 8;

                if (--flight.Power <= 0) {
                    break;
                }

                continue;
            }

            if (!MoveTo(ball, x, y, flight)) {
                break;
            }

            if (goal != null) {
                Score(goal, flight.Kicker);
            }

            if (flight.Power <= 1) {
                break;
            }

            var delay = 500.0 / Digit(flight.Power);
            flight.Power--;
            _schedule(delay, () => Tick(ball, flight));

            return;
        }

        if (_flights.TryGetValue(ball.Id, out var current) && ReferenceEquals(current, flight)) {
            _flights.Remove(ball.Id);
        }
    }

    // A move shows the speed as a repeated digit, 55 down to 11, and waits 500 ms divided by that digit before the next move.
    private static int Digit(int power) => Math.Max(power - 1, 1);

    private bool MoveTo(Item ball, int x, int y, Flight flight)
    {
        var data = (Digit(flight.Power) * 11).ToString();
        var previous = ball.LegacyDataString;
        ball.LegacyDataString = data;

        if (!_room.GetRoomItemHandler().SetFloorItem(null, ball, x, y, ball.Rotation, false, false, false, deferPositionSave: true)) {
            ball.LegacyDataString = previous;

            return false;
        }

        ball.Rotation = flight.Direction;
        _room.SendPacket(new ObjectUpdateComposer(RoomItemSnapshot.Capture(ball)));

        return true;
    }

    private Tile Classify(int x, int y)
    {
        var map = _room.GetGameMap();
        var model = map.Model;

        if (x < 0 || y < 0 || x >= model.MapSizeX || y >= model.MapSizeY || model.SqState[x, y] == SquareState.Blocked) {
            return Tile.Blocked;
        }

        if (map.GameMap[x, y] != 1 && (x, y) != (model.DoorX, model.DoorY)) {
            return Tile.Blocked;
        }

        return _room.GetRoomUserManager().GetUserList().Any(user => user.SetStep ? user.SetX == x && user.SetY == y : user.X == x && user.Y == y)
            ? Tile.Avatar
            : Tile.Open;
    }

    // A goal takes the ball only from its open side, the side its rotation faces away from.
    private Item? GoalAt(int x, int y) => _room.GetGameMap().GetCoordinatedItems(new(x, y))
        .FirstOrDefault(item => ScoreFor(item.Definition.InteractionType) != null);

    private static InteractionType? ScoreFor(InteractionType goal) => goal switch
    {
        InteractionType.FootballGoalRed => InteractionType.Footballcounterred,
        InteractionType.FootballGoalBlue => InteractionType.Footballcounterblue,
        InteractionType.FootballGoalGreen => InteractionType.Footballcountergreen,
        InteractionType.FootballGoalYellow => InteractionType.Footballcounteryellow,
        _ => null
    };

    private void Score(Item goal, RoomUser? kicker)
    {
        goal.LegacyDataString = "1";
        goal.UpdateState();
        _schedule(GoalFlashMilliseconds, () =>
        {
            if (_room != null && ReferenceEquals(_room.GetRoomItemHandler().GetItem(goal.Id), goal)) {
                goal.LegacyDataString = "0";
                goal.UpdateState();
            }
        });

        var score = ScoreFor(goal.Definition.InteractionType);

        foreach (var board in _room.GetRoomItemHandler().GetFloor.Where(item => item.Definition.InteractionType == score).ToList()) {
            board.LegacyDataString = (int.TryParse(board.LegacyDataString, out var value) ? value + 1 : 1).ToString();
            board.UpdateState();
        }

        if (kicker != null && kicker.IsAttachedTo(_room)) {
            _room.OnGoalScored(kicker);
        }
    }

    public void RegisterGate(Item item)
    {
        if (_gates[0] == null) {
            item.Team = Team.Blue;
            _gates[0] = item;
        }
        else if (_gates[1] == null) {
            item.Team = Team.Red;
            _gates[1] = item;
        }
        else if (_gates[2] == null) {
            item.Team = Team.Green;
            _gates[2] = item;
        }
        else if (_gates[3] == null) {
            item.Team = Team.Yellow;
            _gates[3] = item;
        }
    }

    public void UnRegisterGate(Item item)
    {
        switch (item.Team) {
            case Team.Blue: {
                    _gates[0] = null;
                    break;
                }
            case Team.Red: {
                    _gates[1] = null;
                    break;
                }
            case Team.Green: {
                    _gates[2] = null;
                    break;
                }
            case Team.Yellow: {
                    _gates[3] = null;
                    break;
                }
        }
    }

    public void OnGateRemove(Item item)
    {
        switch (item.Definition.InteractionType) {
            case InteractionType.FootballGoalRed:
            case InteractionType.Footballcounterred: {
                    _room.GetGameManager().RemoveFurnitureFromTeam(item, Team.Red);
                    break;
                }
            case InteractionType.FootballGoalGreen:
            case InteractionType.Footballcountergreen: {
                    _room.GetGameManager().RemoveFurnitureFromTeam(item, Team.Green);
                    break;
                }
            case InteractionType.FootballGoalBlue:
            case InteractionType.Footballcounterblue: {
                    _room.GetGameManager().RemoveFurnitureFromTeam(item, Team.Blue);
                    break;
                }
            case InteractionType.FootballGoalYellow:
            case InteractionType.Footballcounteryellow: {
                    _room.GetGameManager().RemoveFurnitureFromTeam(item, Team.Yellow);
                    break;
                }
        }
    }

    public void Dispose()
    {
        Array.Clear(_gates, 0, _gates.Length);
        _gates = null;
        _room = null;
        _balls.Clear();
        _balls = null;
        _flights.Clear();
        _dribbles.Clear();
    }

    private enum Tile
    {
        Open, Blocked, Avatar, Stop
    }

    private sealed class Flight(int direction, int power, RoomUser kicker)
    {
        public int Direction = direction;
        public int Power = power;
        public RoomUser Kicker { get; } = kicker;
    }

    private sealed record DribbleStreak(RoomUser Actor, int Count, int X, int Y, int FromX, int FromY);
}
