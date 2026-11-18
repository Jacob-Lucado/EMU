using Plus.Core;
using Plus.Communication.Packets.Outgoing.Pets;
using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.GameClients;
using Plus.Utilities;

namespace Plus.HabboHotel.Rooms.AI;

public class Pet
{
    private Room? _room;
    private IGameClientManager? _clients;
    private IRewardTrackManager? _rewards;
    public int AnyoneCanRide;
    public bool AllowBreeding;
    public string Color;
    public DateTimeOffset? CreatedAt;
    public PetDatabaseUpdateState DbState;

    public int Energy;
    public int Experience;

    public int[] ExperienceLevels = { 100, 200, 400, 600, 900, 1300, 1800, 2400, 3200, 4300, 7200, 8500, 10100, 13300, 17500, 23000, 51900, 75000, 128000, 150000 };

    public string GnomeClothing;
    public int HairDye;
    public string Name;
    public int Nutrition;
    public int OwnerId;
    public int PetHair;
    public int PetId;
    public bool PlacedInRoom;
    public string Race;
    public int Respect;
    public uint RoomId;
    public int Saddle;

    public int Type;
    public int VirtualId;
    public int X;
    public int Y;
    public double Z;

    public Pet(int petId, int ownerId, uint roomId, string name, int type, string race, string color, int experience, int energy, int nutrition, int respect, DateTimeOffset? createdAt, int x, int y,
        double z, int saddle, int anyonecanride, int dye, int petHer, string gnomeClothing, string ownerName)
    {
        PetId = petId;
        OwnerId = ownerId;
        RoomId = roomId;
        Name = name;
        Type = type;
        Race = race;
        Color = color;
        Experience = experience;
        Energy = energy;
        Nutrition = nutrition;
        Respect = respect;
        CreatedAt = createdAt?.ToUniversalTime();
        X = x;
        Y = y;
        Z = z;
        PlacedInRoom = roomId > 0;
        DbState = PetDatabaseUpdateState.Updated;
        Saddle = saddle;
        AnyoneCanRide = anyonecanride;
        PetHair = petHer;
        HairDye = dye;
        GnomeClothing = gnomeClothing;

        OwnerName = ownerName;
    }

    public Room? Room => _room;

    internal void Attach(Room room, IGameClientManager clients, IRewardTrackManager rewards)
    {
        if (_room != null && !ReferenceEquals(_room, room)) {
            throw new InvalidOperationException("Pet is already attached to another room.");
        }

        _room = room;
        _clients = clients;
        _rewards = rewards;
    }

    internal void Detach(Room room)
    {
        if (!ReferenceEquals(_room, room)) {
            return;
        }

        _room = null;
        _clients = null;
        _rewards = null;
    }

    public bool IsInRoom => RoomId > 0;

    public int Level
    {
        get
        {
            for (var level = 0; level < ExperienceLevels.Length; ++level) {
                if (Experience < ExperienceLevels[level]) {
                    return level + 1;
                }
            }

            return ExperienceLevels.Length;
        }
    }

    public static int MaxLevel => 20;

    public int ExperienceGoal =>
        //will error index out of range (need to look into this sometime)
        ExperienceLevels[Level - 1];

    public int MaxEnergy => 100 + 20 * Level;

    public bool IsTired => Energy <= 10;

    public bool ObeysCommand(int commandId) => !IsTired && (Level > 15 || commandId == 8 || Random.Shared.Next(1, 9) < 6);

    public static int MaxNutrition => 100;

    public string CustomParts => !string.IsNullOrEmpty(GnomeClothing) && GnomeClothing != "-1"
        ? GnomeClothing
        : Saddle > 0
            ? $"3 2 {PetHair} {HairDye} 3 {PetHair} {HairDye} 4 {Saddle} 0"
            : $"2 2 {PetHair} {HairDye} 3 {PetHair} {HairDye}";

    public string Look => $"{Type} {Race} {Color} {CustomParts}";

    public string OwnerName { get; set; }

    public void OnRespect()
    {
        Respect++;
        Room.SendPacket(new RespectPetNotificationComposer(VirtualId, PetId, Name, Color));

        if (DbState != PetDatabaseUpdateState.NeedsInsert) {
            DbState = PetDatabaseUpdateState.NeedsUpdate;
        }

        if (Experience <= 150000) {
            Addexperience(10);
        }
    }

    public void Addexperience(int amount)
    {
        var before = Level;
        Experience = Experience + amount;

        if (Experience > 150000) {
            Experience = 150000;

            if (Room != null) {
                Room.SendPacket(new AddExperiencePointsComposer(PetId, VirtualId, amount));
            }
        }
        else {
            if (DbState != PetDatabaseUpdateState.NeedsInsert) {
                DbState = PetDatabaseUpdateState.NeedsUpdate;
            }

            if (Room != null) {
                Room.SendPacket(new AddExperiencePointsComposer(PetId, VirtualId, amount));
            }
        }

        var gained = Level - before;

        if (gained < 1) {
            return;
        }

        var client = OwnerId > 0 ? _clients?.GetClientByUserId(OwnerId) : null;

        if (client != null) {
            client.Send(new PetLevelNotificationComposer(PetId, Name, Level, Look));
            client.Send(new PetTrainingPanelComposer(this));
            _rewards!.Progress(client, RewardTrackActions.PetLevel, gained);
        }

        if (Room != null && !Room.Behavior.MuteAllPets) {
            Room.SendChat(receiver => new ChatComposer(VirtualId, "Leveling up", 0, 0, receiver));
        }
    }

    public void PetEnergy(bool add)
    {
        if (add) {
            if (Energy >= MaxEnergy) {
                return;
            }

            Energy++;

            if (DbState != PetDatabaseUpdateState.NeedsInsert) {
                DbState = PetDatabaseUpdateState.NeedsUpdate;
            }

            return;
        }

        SpendEnergy(5);
    }

    /// <summary>Food can push the energy above the maximum.</summary>
    public void Feed(int energy)
    {
        Energy += energy;

        if (DbState != PetDatabaseUpdateState.NeedsInsert) {
            DbState = PetDatabaseUpdateState.NeedsUpdate;
        }
    }

    public void SpendEnergy(int amount)
    {
        Energy = Math.Max(0, Energy - amount);

        if (DbState != PetDatabaseUpdateState.NeedsInsert) {
            DbState = PetDatabaseUpdateState.NeedsUpdate;
        }
    }
}
