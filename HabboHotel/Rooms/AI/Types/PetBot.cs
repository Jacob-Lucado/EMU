using System.Drawing;
using Plus.Core;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Rooms.PathFinding;
using Plus.Utilities;
using Plus.HabboHotel.Rooms.Chat.Pets.Commands;
using Plus.HabboHotel.Rooms.Chat.Pets.Locale;

namespace Plus.HabboHotel.Rooms.AI.Types;

public class PetBot : BotAi
{
    private int _actionTimer;
    private static readonly int EnergyRestTicks = (int)(TimeSpan.FromSeconds(40) / RoomCycle.Interval);
    private int _energyTimer;
    private uint _visitId;
    private Point _visitSpot;
    private int _speechTimer;
    private readonly IPetLocale _locale;
    private readonly IPetCommandManager _commands;

    public PetBot(int virtualId, IPetLocale locale, IPetCommandManager commands)
    {
        _locale = locale;
        _commands = commands;
        _speechTimer = Random.Shared.Next(10, 60);
        _actionTimer = Random.Shared.Next(10, 30 + virtualId);
        _energyTimer = EnergyRestTicks - 1;
    }

    private void RemovePetStatus()
    {
        var botUser = GetRoomUser();

        if (botUser == null) {
            return;
        }

        var pet = botUser;

        if (pet != null) {
            foreach (var kvp in pet.Statusses.ToList()) {
                if (pet.Statusses.ContainsKey(kvp.Key)) {
                    pet.Statusses.Remove(kvp.Key);
                }
            }
        }
    }

    public override void OnSelfEnterRoom()
    {
        var botRoom = GetRoom();

        if (botRoom == null) {
            return;
        }

        var botUser = GetRoomUser();

        if (botUser == null) {
            return;
        }

        if (botUser != null && botRoom.GetGameMap().TryGetRandomWalkableSquare(out var nextCoord)) {
            botUser.MoveTo(nextCoord.X, nextCoord.Y);
        }
    }

    public override void OnSelfLeaveRoom(bool kicked) { }


    public override void OnUserEnterRoom(RoomUser user)
    {
        var botUser = GetRoomUser();

        if (botUser == null) {
            return;
        }

        var speakerSession = user?.GetClient();

        if (speakerSession == null) {
            return;
        }

        if (speakerSession != null && speakerSession.GetHabbo() != null) {
            var pet = botUser;

            if (pet?.PetData is { } petData) {
                if (speakerSession.GetHabbo().Username == petData.OwnerName) {
                    Greet();
                }
            }
        }
    }

    /// <summary>Says one welcome line, as a pet does when its owner places it.</summary>
    public void Greet()
    {
        if (GetRoomUser() is { PetData: { } petData } pet) {
            var speech = _locale.GetValue($"welcome.speech.pet{petData.Type}");
            pet.Chat(speech[Random.Shared.Next(0, speech.Length)]);
        }
    }

    public override void OnUserLeaveRoom(GameClient client) { }

    public override void OnUserShout(RoomUser user, string message) { }

    public override void OnTimerTick()
    {
        var botRoom = GetRoom();

        if (botRoom == null) {
            return;
        }

        var botUser = GetRoomUser();

        if (botUser == null) {
            return;
        }

        var pet = botUser;

        if (pet?.PetData is not { } petData) {
            return;
        }

        if (_visitId != 0 && Visit(botRoom, pet, petData)) {
            return;
        }

        if (_speechTimer <= 0) {
            if (petData.DbState != PetDatabaseUpdateState.NeedsInsert) {
                petData.DbState = PetDatabaseUpdateState.NeedsUpdate;
            }

            if (pet != null) {
                RemovePetStatus();
                // A pet never chats on its own; it only shows the three letter gestures from its speech list.
                var gestures = _locale.GetValue($"speech.pet{petData.Type}").Where(entry => entry.Length == 3).ToArray();

                if (gestures.Length > 0) {
                    pet.Statusses.Add(gestures[Random.Shared.Next(0, gestures.Length)], TextHandling.GetString(pet.Z));
                }
            }

            _speechTimer = Random.Shared.Next(20, 120 + 1);
        }
        else {
            _speechTimer--;
        }

        if (_actionTimer <= 0) {
            try {
                RemovePetStatus();
                _actionTimer = Random.Shared.Next(15, 40 + petData.VirtualId + 1);

                if (!botUser.RidingHorse) {
                    // Remove Status
                    RemovePetStatus();

                    if (petData.Energy < petData.MaxEnergy && FindFood(botRoom, botUser, petData) is { } food) {
                        GoTo(botRoom, botUser, food);
                    }
                    else if (Random.Shared.Next(DrinkChance) == 0 && FindWater(botRoom, botUser) is { } water) {
                        GoTo(botRoom, botUser, water);
                    }
                    else if (botUser.CanWalk && botRoom.GetGameMap().TryGetRandomWalkableSquare(out var nextCoord)) {
                        botUser.MoveTo(nextCoord.X, nextCoord.Y);
                    }
                }
            }
            catch (Exception e) {
                ExceptionLogger.LogException(e);
            }
        }
        else {
            _actionTimer--;
        }

        if (_energyTimer <= 0) {
            RemovePetStatus(); // Remove Status
            petData.PetEnergy(true); // Add Energy
            _energyTimer = EnergyRestTicks - 1;
        }
        else {
            _energyTimer--;
        }
    }

    public override void OnUserSay(RoomUser user, string message)
    {
        if (GetRoom() is not { } botRoom || GetRoomUser() is not { PetData: { } petData } pet || user?.GetClient()?.GetHabbo() is not { } speaker) {
            return;
        }

        if (message.Equals(petData.Name, StringComparison.OrdinalIgnoreCase)) {
            pet.SetRot(Rotation.Calculate(pet.X, pet.Y, user.X, user.Y), false);

            return;
        }

        if (speaker.Id != petData.OwnerId || !message.StartsWith(petData.Name + " ", StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        var commandId = _commands.TryInvoke(message[(petData.Name.Length + 1)..]);
        var commands = PetCommandSet.For(petData.Type);

        if (commandId < 0 || !commands.Has(commandId)) {
            return;
        }

        RemovePetStatus();
        pet.UpdateNeeded = true;

        if (!commands.HasLearned(commandId, petData.Level)) {
            pet.Statusses["gst"] = "puz";
            PetVocals.Say(pet, PetVocal.UnknownCommand);

            return;
        }

        if (petData.IsTired) {
            pet.Statusses["lay"] = TextHandling.GetString(pet.Z);
            _speechTimer = 50;
            _actionTimer = 45;

            return;
        }

        var food = commandId == PetCommandSet.Eat ? FindFood(botRoom, pet, petData) : null;
        var water = commandId == PetCommandSet.Drink ? FindWater(botRoom, pet) : null;

        // Without a bowl or food in the room a pet refuses to drink or eat.
        if (commandId == PetCommandSet.Drink && water == null || commandId == PetCommandSet.Eat && food == null || !petData.ObeysCommand(commandId)) {
            petData.SpendEnergy(2);
            PetVocals.Say(pet, PetVocal.Refusal);

            return;
        }

        if (food != null) {
            GoTo(botRoom, pet, food);
            petData.Addexperience(0);

            return;
        }

        Obey(botRoom, pet, user, commandId);

        if (water != null) {
            GoTo(botRoom, pet, water);
        }

        if (commandId != PetCommandSet.Free) {
            petData.Addexperience(5);
            petData.SpendEnergy(5);
        }
    }

    private static Item? FindFood(Room room, RoomUser pet, Pet petData) => room.GetRoomItemHandler().GetFloor
        .Where(item => PetFood.Of(item.Definition)?.Feeds(petData.Type) == true)
        .MinBy(item => Math.Abs(item.GetX - pet.X) + Math.Abs(item.GetY - pet.Y));

    private static Item? FindWater(Room room, RoomUser pet) => room.GetRoomItemHandler().GetFloor
        .Where(item => PetWater.Is(item.Definition))
        .MinBy(item => Math.Abs(item.GetX - pet.X) + Math.Abs(item.GetY - pet.Y));

    // A pet stands on food and bowls; it stands next to furni it cannot walk on, such as the trough.
    private void GoTo(Room room, RoomUser pet, Item item)
    {
        var map = room.GetGameMap();
        var spot = item.Definition.Walkable
            ? item.Coordinate
            : Around(item.Coordinate).Where(tile => map.CanWalk(tile.X, tile.Y, false))
                .OrderBy(tile => Math.Abs(tile.X - pet.X) + Math.Abs(tile.Y - pet.Y)).FirstOrDefault(item.SquareInFront);
        _visitId = item.Id;
        _visitSpot = spot;
        pet.MoveTo(spot);
    }

    private static IEnumerable<Point> Around(Point tile)
    {
        for (var dx = -1; dx <= 1; dx++) {
            for (var dy = -1; dy <= 1; dy++) {
                if (dx != 0 || dy != 0) {
                    yield return new(tile.X + dx, tile.Y + dy);
                }
            }
        }
    }

    // Returns false when the pet is not at the furni it went to, so it goes on with its day.
    private bool Visit(Room room, RoomUser pet, Pet petData)
    {
        var item = room.GetRoomItemHandler().GetItem(_visitId);

        if (item == null) {
            _visitId = 0;

            return false;
        }

        if (pet.IsWalking) {
            return true;
        }

        _visitId = 0;

        if (pet.Coordinate != _visitSpot) {
            return false;
        }

        if (PetWater.Is(item.Definition)) {
            Drink(item, pet);

            return true;
        }

        return PetFood.Of(item.Definition) is { } values && values.Feeds(petData.Type) && Eat(room, item, values, pet, petData);
    }

    private void Drink(Item water, RoomUser pet)
    {
        RemovePetStatus();
        pet.Statusses["eat"] = TextHandling.GetString(pet.Z);
        pet.UpdateNeeded = true;
        _actionTimer = 10;
        PetVocals.Say(pet, PetVocal.Drinking);
        PetWater.Drink(water, DateTimeOffset.UtcNow);
        water.UpdateState(true, true);
    }

    // On the food the pet takes one bite, and more while it is below its maximum energy.
    private bool Eat(Room room, Item food, PetFood values, RoomUser pet, Pet petData)
    {
        var bites = int.TryParse(food.LegacyDataString, out var taken) ? Math.Clamp(taken, 0, PetFood.Bites - 1) : 0;
        var eaten = 0;

        do {
            petData.Feed(values.BiteEnergy);
            bites++;
            eaten++;
        } while (bites < PetFood.Bites && petData.Energy < petData.MaxEnergy);

        RemovePetStatus();
        pet.Statusses["eat"] = TextHandling.GetString(pet.Z);
        pet.UpdateNeeded = true;
        _actionTimer = 10;
        PetVocals.Say(pet, PetVocal.Eating);

        if (bites >= PetFood.Bites) {
            room.GetRoomItemHandler().DeleteFurniture(food);
        }
        else {
            if (food.ExtraData is not LegacyDataFormat) {
                food.ExtraData = new LegacyDataFormat();
            }

            food.LegacyDataString = bites.ToString();
            food.UpdateUsedState();
        }

        petData.Addexperience(eaten * values.BiteExperience);

        return true;
    }

    private void Obey(Room room, RoomUser pet, RoomUser user, int commandId)
    {
        switch (commandId) {
            case PetCommandSet.Free:
                if (room.GetGameMap().TryGetRandomWalkableSquare(out var free)) {
                    pet.MoveTo(free.X, free.Y);
                }

                break;
            case 3:
                _actionTimer = 30;
                var front = user.SquareInFront;
                pet.MoveTo(front.X, front.Y);
                break;
            default:
                if (Postures.TryGetValue(commandId, out var posture)) {
                    pet.Statusses[posture] = TextHandling.GetString(pet.Z);
                    _actionTimer = 30;
                }

                break;
        }
    }

    // About one action in six a pet goes to drink when there is water in the room.
    private const int DrinkChance = 6;

    private static readonly Dictionary<int, string> Postures = new()
    {
        [1] = "sit",
        [2] = "lay",
        [5] = "ded",
        [9] = "jmp",
        [44] = "wag"
    };
}
