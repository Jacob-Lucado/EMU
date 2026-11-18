using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.AI.MonsterPlants;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.AI;

public sealed record PetInformationSnapshot(int Id, string Name, int Level, int MaxLevel, int Experience,
    int ExperienceGoal, int Energy, int MaxEnergy, int Nutrition, int MaxNutrition, int Respect, int OwnerId,
    int AgeInDays, string OwnerName, bool HasSaddle, int AnyoneCanRide, IReadOnlyList<int> SkillThresholds, bool HasBreedingPermission = false, PetPlantInformation? Plant = null);

/// <summary>The monster plant fields of the pet information window.</summary>
public sealed record PetPlantInformation(int BreedId, bool CanBreed, bool CanRevive, int Rarity, int MaxWellbeingSeconds,
    int RemainingWellbeingSeconds, int RemainingGrowthSeconds, bool BreedingPermission);

public interface IPetInformationService
{
    void SendInformation(GameClient session, int petId);
    void SendTrainingPanel(GameClient session, int petId);
    PetInformationSnapshot Capture(Pet pet);
}

public sealed class PetInformationService(TimeProvider clock) : IPetInformationService
{
    private const int HorseType = 15;

    // The levels at which a horse's jump skill bar starts again.
    private static readonly int[] HorseJumpSkillLevels = [0, 2, 6, 10, 14];

    public void SendInformation(GameClient session, int petId)
    {
        var habbo = session.GetHabbo();

        if (!habbo.InRoom || habbo.CurrentRoom == null) {
            return;
        }

        var room = habbo.CurrentRoom;

        if (room.GetRoomUserManager().TryGetPet(petId, out var pet)) {
            if (pet.RoomId != room.RoomId || pet.PetData == null) {
                return;
            }

            session.Send(new PetInformationComposer(Capture(pet.PetData)));

            return;
        }

        var target = room.GetRoomUserManager().GetRoomUserByHabbo(petId)?.GetClient()?.GetHabbo();

        if (target != null) {
            session.Send(new PetInformationComposer(Capture(target, clock.GetUtcNow())));
        }
    }

    public void SendTrainingPanel(GameClient session, int petId)
    {
        var room = session.GetHabbo().CurrentRoom;

        if (room == null) {
            return;
        }

        if (!room.GetRoomUserManager().TryGetPet(petId, out var pet)) {
            var target = room.GetRoomUserManager().GetRoomUserByHabbo(petId)?.GetClient()?.GetHabbo();

            if (target != null) {
                session.SendWhisper("Maybe one day, boo boo.");
            }

            return;
        }

        if (pet.RoomId != room.RoomId || pet.PetData == null || pet.PetData.OwnerId != session.GetHabbo().Id) {
            return;
        }

        session.Send(new PetTrainingPanelComposer(pet.PetData));
    }

    public PetInformationSnapshot Capture(Pet pet)
    {
        var now = clock.GetUtcNow();
        var age = pet.CreatedAt is { } createdAt ? (now - createdAt).Ticks / TimeSpan.TicksPerDay : 0;
        var days = (int)Math.Clamp(age, 0, int.MaxValue);

        if (pet.Plant is { } plant) {
            return new(pet.PetId, pet.Name, plant.Level(now), MonsterPlant.AdultLevel, plant.Experience(now),
                plant.ExperienceGoal(now), plant.Energy(now), MonsterPlant.BarMaximum, 100, 100, pet.Respect, pet.OwnerId,
                days, pet.OwnerName, false, pet.AnyoneCanRide, [],
                Plant: new(int.TryParse(pet.Race, out var breed) ? breed : 0, plant.CanBreedNow(now), plant.IsDead(now), plant.Rarity,
                    (int)plant.MaxWellbeing(now).TotalSeconds, (int)Math.Ceiling(plant.RemainingWellbeing(now).TotalSeconds),
                    (int)Math.Ceiling(plant.RemainingGrowth(now).TotalSeconds), plant.BreedingPermission));
        }

        return new(pet.PetId, pet.Name, pet.Level, Pet.MaxLevel, pet.Experience, pet.ExperienceGoal,
            pet.Energy, pet.MaxEnergy, pet.Nutrition, Pet.MaxNutrition, pet.Respect, pet.OwnerId,
            days, pet.OwnerName, pet.Saddle > 0, pet.AnyoneCanRide, pet.Type == HorseType ? HorseJumpSkillLevels : [], pet.AllowBreeding);
    }

    internal static PetInformationSnapshot Capture(Habbo habbo, DateTimeOffset now)
    {
        var days = habbo.AccountCreatedAt is { } createdAt ? (now - createdAt).Ticks / TimeSpan.TicksPerDay : 0;
        var age = (int)Math.Clamp(days, 0, int.MaxValue);

        return new(habbo.Id, habbo.Username, habbo.Access.SecurityLevel, 10, 0, 0, 100, 100, 100, 100,
            habbo.HabboStats?.Respect ?? 0, habbo.Id, age, habbo.Username, false, 0, []);
    }
}
