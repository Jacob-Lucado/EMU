using System.Collections.Concurrent;
using System.Drawing;
using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.Communication.Packets.Outgoing.Rooms.Furni.Wired;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms.Games.Teams;

namespace Plus.HabboHotel.Rooms.Games.Freeze;

public class Freeze
{
    private readonly ConcurrentDictionary<uint, Item> _freezeBlocks;
    private readonly ConcurrentDictionary<uint, Item> _freezeTiles;
    private Room _room;

    public Freeze(Room room)
    {
        _room = room;
        GameIsStarted = false;
        ExitTeleports = new();
        _freezeTiles = new();
        _freezeBlocks = new();
    }

    public bool GameIsStarted { get; private set; }

    public ConcurrentDictionary<uint, Item> ExitTeleports { get; }

    public void AddExitTile(Item item)
    {
        if (!ExitTeleports.ContainsKey(item.Id)) {
            ExitTeleports.TryAdd(item.Id, item);
        }
    }

    public void RemoveExitTile(uint id)
    {
        if (ExitTeleports.ContainsKey(id)) {
            ExitTeleports.TryRemove(id, out var temp);
        }
    }

    public Item GetRandomExitTile() => ExitTeleports.Values.ToList()[Random.Shared.Next(0, ExitTeleports.Count)];

    public void StartGame()
    {
        GameIsStarted = true;
        CountTeamPoints();
        ResetGame();

        if (ExitTeleports.Count > 0) {
            foreach (var exitTile in ExitTeleports.Values.ToList()) {
                if (exitTile.LegacyDataString == "0" || string.IsNullOrEmpty(exitTile.LegacyDataString)) {
                    exitTile.LegacyDataString = "1";
                }

                exitTile.UpdateState();
            }
        }

        _room.GetGameManager().LockGates();
        RemoveSoloPlayer();
    }

    // A Freeze game started by a single player in a team takes that player out of the team. The effect goes, the gate counts go to 0 and YouArePlayingGame is false.
    // This does not change the team size cap, the lives or a start with more players.
    private void RemoveSoloPlayer()
    {
        var teams = _room.GetTeamManagerForFreeze();
        var players = teams.BlueTeam.Concat(teams.RedTeam).Concat(teams.GreenTeam).Concat(teams.YellowTeam).ToList();

        if (players.Count != 1 || players[0].GetClient() is not { } client) {
            return;
        }

        var player = players[0];
        client.Send(new YouArePlayingGameComposer(false));
        teams.OnUserLeave(player);
        player.Team = Team.None;
        player.ApplyEffect(0);
    }

    public void StopGame(bool userTriggered = false, bool keepGatesLocked = false)
    {
        GameIsStarted = false;

        if (!keepGatesLocked) {
            _room.GetGameManager().UnlockGates();
        }

        _room.GetGameManager().StopGame();
        CloseField();

        var winners = _room.GetGameManager().GetWinningTeam();

        foreach (var user in _room.GetRoomUserManager().GetUserList().ToList()) {
            user.FreezeLives = 0;

            if (user.Team == winners) {
                user.UnIdle();
                user.DanceId = 0;
                _room.SendPacket(new ActionComposer(user.VirtualId, 1));
            }

            if (ExitTeleports.Count > 0) {
                var tile = _freezeTiles.Values.Where(x => x.GetX == user.X && x.GetY == user.Y).FirstOrDefault();

                if (tile != null) {
                    var exitTle = GetRandomExitTile();

                    if (exitTle != null) {
                        user.SetPos(exitTle.GetX, exitTle.GetY, exitTle.GetZ);
                        user.UpdateNeeded = true;

                        if (user.IsAsleep) {
                            user.UnIdle();
                        }
                    }
                }
            }
        }

        if (!userTriggered) {
            _room.GetWired().TriggerEvent(WiredBoxType.TriggerGameEnds, null);
        }
    }

    // The tiles keep their state. A broken block shows no power-up any more and the exits switch off, in one update.
    private void CloseField()
    {
        var changed = new List<Item>();

        foreach (var tile in _freezeTiles.Values) {
            if (tile.InteractingUser > 0) {
                tile.InteractingUser = 0;
                tile.InteractionCountHelper = 0;
                tile.UpdateNeeded = false;
                tile.UpdateCounter = 0;
                tile.LegacyDataString = "0";
                changed.Add(tile);
            }
        }

        foreach (var block in _freezeBlocks.Values) {
            if (int.TryParse(block.LegacyDataString, out var data) && data / 1000 is >= 1 and <= 7) {
                block.LegacyDataString = ((data / 1000 + 10) * 1000).ToString();
                block.FreezePowerUp = FreezePowerUp.None;
                changed.Add(block);
            }
        }

        foreach (var exit in ExitTeleports.Values) {
            exit.LegacyDataString = "0";
            changed.Add(exit);
        }

        foreach (var item in changed) {
            item.UpdateState(true, false);
        }

        _room.SendObjectsData(changed);
    }

    public void CycleUser(RoomUser user)
    {
        if (user.Freezed) {
            user.FreezeCounter++;

            if (user.FreezeCounter > 10) {
                user.Freezed = false;
                user.FreezeCounter = 0;
                ActivateShield(user);
            }
        }

        if (user.ShieldActive) {
            user.ShieldCounter++;

            if (user.ShieldCounter > 10) {
                user.ShieldActive = false;
                user.ShieldCounter = 10;
                user.ApplyEffect(Convert.ToInt32(user.Team) + 39);
            }
        }
    }

    public void ResetGame()
    {
        foreach (var item in _freezeTiles.Values.ToList()) {
            if (!string.IsNullOrEmpty(item.LegacyDataString)) {
                item.InteractionCountHelper = 0;
                item.LegacyDataString = "";
                item.UpdateState(false, true);
                _room.GetGameMap().AddItemToMap(item, false);
            }
        }

        foreach (var item in _freezeBlocks.Values) {
            if (!string.IsNullOrEmpty(item.LegacyDataString)) {
                item.LegacyDataString = "";
                item.UpdateState(false, true);
                _room.GetGameMap().AddItemToMap(item, false);
            }
        }
    }

    public void OnUserWalk(RoomUser user)
    {
        if (!GameIsStarted || user.Team == Team.None) {
            return;
        }

        foreach (var item in _freezeTiles.Values.ToList()) {
            if (user.GoalX == item.GetX && user.GoalY == item.GetY && user.FreezeInteracting) {
                if (item.InteractionCountHelper == 0) {
                    item.InteractionCountHelper = 1;
                    item.LegacyDataString = "1000";
                    item.UpdateState();
                    item.InteractingUser = user.UserId;
                    item.FreezePowerUp = user.BanzaiPowerUp;
                    item.RequestUpdate(4, true);

                    switch (user.BanzaiPowerUp) {
                        case FreezePowerUp.GreenArrow:
                        case FreezePowerUp.OrangeSnowball: {
                                user.BanzaiPowerUp = FreezePowerUp.None;
                                break;
                            }
                    }

                    break;
                }
            }
        }

        foreach (var item in _freezeBlocks.Values.ToList()) {
            if (user.GoalX == item.GetX && user.GoalY == item.GetY) {
                if (item.FreezePowerUp != FreezePowerUp.None) {
                    PickUpPowerUp(item, user);
                }
            }
        }
    }

    private void CountTeamPoints()
    {
        _room.GetGameManager().Reset();

        foreach (var user in _room.GetRoomUserManager().GetUserList().ToList()) {
            if (user.IsBot || user.Team == Team.None || user.GetClient() == null) {
                continue;
            }

            user.BanzaiPowerUp = FreezePowerUp.None;
            user.FreezeLives = 3;
            user.ShieldActive = false;
            user.ShieldCounter = 11;
            _room.GetGameManager().AddPointToTeam(user.Team, 40);
            user.GetClient().Send(new GamePlayerValueComposer(user.VirtualId, user.FreezeLives));
        }
    }

    private static readonly Size[] Straight = [new(1, 0), new(0, 1), new(-1, 0), new(0, -1)];
    private static readonly Size[] Diagonal = [new(1, 1), new(-1, -1), new(-1, 1), new(1, -1)];

    // A blast reaches each tile 100 ms per step from the snowball, and the snowball tile and its neighbours at 100 ms.
    public void OnFreezeTiles(Item item, FreezePowerUp powerUp)
    {
        var (directions, length) = powerUp switch
        {
            FreezePowerUp.BlueArrow => (Straight, 5),
            FreezePowerUp.GreenArrow => (Diagonal, 5),
            FreezePowerUp.OrangeSnowball => ([.. Straight, .. Diagonal], 5),
            _ => (Straight, 3)
        };
        var reached = new Dictionary<Point, int>();
        var centre = item.Coordinate;

        if (SquareGotFreezeTile(GetItemsForSquare(centre))) {
            reached[centre] = 0;
        }

        foreach (var direction in directions) {
            for (var step = 1; step < length; step++) {
                var point = new Point(centre.X + direction.Width * step, centre.Y + direction.Height * step);
                var items = GetItemsForSquare(point);

                if (!SquareGotFreezeTile(items)) {
                    break;
                }

                reached.TryAdd(point, step);

                if (SquareGotFreezeBlock(items)) {
                    break;
                }
            }
        }

        var changed = new List<Item>();

        foreach (var (point, step) in reached) {
            var delay = 100 * Math.Max(1, step);

            foreach (var square in GetItemsForSquare(point)) {
                if (square.Definition.InteractionType == InteractionType.FreezeTile) {
                    square.LegacyDataString = (11000 + delay).ToString();
                    changed.Add(square);
                }
                else if (square.Definition.InteractionType == InteractionType.FreezeTileBlock && SetRandomPowerUp(square, delay)) {
                    changed.Add(square);
                }
            }
        }

        foreach (var square in changed) {
            square.UpdateState(false, false);
        }

        _room.SendObjectsData(changed);

        foreach (var (point, step) in reached) {
            HandleUserFreeze(point, 100 * Math.Max(1, step));
        }
    }

    private static void ActivateShield(RoomUser user)
    {
        user.ApplyEffect(Convert.ToInt32(user.Team + 48));
        user.ShieldActive = true;
        user.ShieldCounter = 0;
    }

    private bool SetRandomPowerUp(Item item, int delay)
    {
        if (!string.IsNullOrEmpty(item.LegacyDataString)) {
            return false;
        }

        var next = Random.Shared.Next(1, 14);

        switch (next) {
            case 2: {
                    item.LegacyDataString = (2000 + delay).ToString();
                    item.FreezePowerUp = FreezePowerUp.BlueArrow;
                    break;
                }
            case 3: {
                    item.LegacyDataString = (3000 + delay).ToString();
                    item.FreezePowerUp = FreezePowerUp.Snowballs;
                    break;
                }
            case 4: {
                    item.LegacyDataString = (4000 + delay).ToString();
                    item.FreezePowerUp = FreezePowerUp.GreenArrow;
                    break;
                }
            case 5: {
                    item.LegacyDataString = (5000 + delay).ToString();
                    item.FreezePowerUp = FreezePowerUp.OrangeSnowball;
                    break;
                }
            case 6: {
                    item.LegacyDataString = (6000 + delay).ToString();
                    item.FreezePowerUp = FreezePowerUp.Heart;
                    break;
                }
            case 7: {
                    item.LegacyDataString = (7000 + delay).ToString();
                    item.FreezePowerUp = FreezePowerUp.Shield;
                    break;
                }
            default: {
                    item.LegacyDataString = (1000 + delay).ToString();
                    item.FreezePowerUp = FreezePowerUp.None;
                    break;
                }
        }

        _room.GetGameMap().RemoveFromMap(item, false);

        return true;
    }

    private void PickUpPowerUp(Item item, RoomUser user)
    {
        switch (item.FreezePowerUp) {
            case FreezePowerUp.Heart: {
                    if (user.FreezeLives < 5) {
                        user.FreezeLives++;
                        _room.GetGameManager().AddPointToTeam(user.Team, 10);
                    }

                    user.GetClient().Send(new GamePlayerValueComposer(user.VirtualId, user.FreezeLives));
                    break;
                }
            case FreezePowerUp.Shield: {
                    ActivateShield(user);
                    break;
                }
            case FreezePowerUp.BlueArrow:
            case FreezePowerUp.GreenArrow:
            case FreezePowerUp.OrangeSnowball: {
                    user.BanzaiPowerUp = item.FreezePowerUp;
                    break;
                }
        }

        item.FreezePowerUp = FreezePowerUp.None;
        item.LegacyDataString = ((int.Parse(item.LegacyDataString) / 1000 + 10) * 1000).ToString();
        item.UpdateState(false, true);
    }

    public void AddFreezeTile(Item item)
    {
        if (!_freezeTiles.ContainsKey(item.Id)) {
            _freezeTiles.TryAdd(item.Id, item);
        }
    }

    public void RemoveFreezeTile(uint itemId)
    {
        Item? item = null;

        if (_freezeTiles.ContainsKey(itemId)) {
            _freezeTiles.TryRemove(itemId, out item);
        }
    }

    public void AddFreezeBlock(Item item)
    {
        if (!_freezeBlocks.ContainsKey(item.Id)) {
            _freezeBlocks.TryAdd(item.Id, item);
        }
    }

    public void RemoveFreezeBlock(uint itemId)
    {
        Item? item = null;
        _freezeBlocks.TryRemove(itemId, out item);
    }

    private void HandleUserFreeze(Point point, int delay)
    {
        if (_room == null) {
            return;
        }

        var user = _room.GetGameMap().GetRoomUsers(point).FirstOrDefault();

        if (user != null) {
            if (user.IsWalking && user.SetX != point.X && user.SetY != point.Y) {
                return;
            }

            FreezeUser(user, delay);
        }
    }

    private void FreezeUser(RoomUser user, int delay)
    {
        if (!GameIsStarted || user.IsBot || user.ShieldActive || user.Team == Team.None || user.Freezed) {
            return;
        }

        user.Freezed = true;
        user.FreezeCounter = 0;
        user.FreezeLives--;

        if (user.FreezeLives <= 0) {
            user.GetClient().Send(new GamePlayerValueComposer(user.VirtualId, user.FreezeLives));
            user.ApplyEffect(-1);
            _room.GetGameManager().AddPointToTeam(user.Team, -10);
            var t = _room.GetTeamManagerForFreeze();
            t.OnUserLeave(user);
            user.Team = Team.None;

            if (ExitTeleports.Count > 0) {
                _room.GetGameMap().TeleportToItem(user, GetRandomExitTile());
            }

            user.Freezed = false;
            user.SetStep = false;
            user.IsWalking = false;
            user.UpdateNeeded = true;

            if (t.BlueTeam.Count <= 0 && t.RedTeam.Count <= 0 && t.GreenTeam.Count <= 0 && t.YellowTeam.Count > 0) {
                StopGame(); // yellow team win
            }
            else if (t.BlueTeam.Count > 0 && t.RedTeam.Count <= 0 && t.GreenTeam.Count <= 0 &&
                     t.YellowTeam.Count <= 0) {
                StopGame(); // blue team win
            }
            else if (t.BlueTeam.Count <= 0 && t.RedTeam.Count > 0 && t.GreenTeam.Count <= 0 &&
                     t.YellowTeam.Count <= 0) {
                StopGame(); // red team win
            }
            else if (t.BlueTeam.Count <= 0 && t.RedTeam.Count <= 0 && t.GreenTeam.Count > 0 &&
                     t.YellowTeam.Count <= 0) {
                StopGame(); // green team win
            }

            return;
        }

        _room.GetGameManager().AddPointToTeam(user.Team, -10);
        user.ApplyEffect(12, delay);
        user.GetClient().Send(new GamePlayerValueComposer(user.VirtualId, user.FreezeLives));
    }

    private List<Item> GetItemsForSquare(Point point) => _room.GetGameMap().GetCoordinatedItems(point);

    private static bool SquareGotFreezeTile(List<Item> items)
    {
        foreach (var item in items) {
            if (item.Definition.InteractionType == InteractionType.FreezeTile) {
                return true;
            }
        }

        return false;
    }

    private static bool SquareGotFreezeBlock(List<Item> items)
    {
        foreach (var item in items) {
            if (item.Definition.InteractionType == InteractionType.FreezeTileBlock) {
                return true;
            }
        }

        return false;
    }

    public void Dispose()
    {
        _room = null;
        ExitTeleports.Clear();
        _freezeTiles.Clear();
        _freezeBlocks.Clear();
    }
}
