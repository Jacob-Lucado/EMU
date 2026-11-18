using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;

public sealed class PetInformationComposer(PetInformationSnapshot pet) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.PetInformationComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(pet.Id);
        packet.WriteString(pet.Name);
        packet.WriteInteger(pet.Level);
        packet.WriteInteger(pet.MaxLevel);
        packet.WriteInteger(pet.Experience);
        packet.WriteInteger(pet.ExperienceGoal);
        packet.WriteInteger(pet.Energy);
        packet.WriteInteger(pet.MaxEnergy);
        packet.WriteInteger(pet.Nutrition);
        packet.WriteInteger(pet.MaxNutrition);
        packet.WriteInteger(pet.Respect);
        packet.WriteInteger(pet.OwnerId);
        packet.WriteInteger(pet.AgeInDays);
        packet.WriteString(pet.OwnerName);
        packet.WriteInteger(pet.Plant?.BreedId ?? 1);
        packet.WriteBoolean(pet.HasSaddle);
        packet.WriteBoolean(false);
        packet.WriteInteger(0);
        packet.WriteInteger(pet.AnyoneCanRide);

        if (pet.Plant is { } plant) {
            packet.WriteBoolean(plant.CanBreed);
            packet.WriteBoolean(false);
            packet.WriteBoolean(plant.CanRevive);
            packet.WriteInteger(plant.Rarity);
            packet.WriteInteger(plant.MaxWellbeingSeconds);
            packet.WriteInteger(plant.RemainingWellbeingSeconds);
            packet.WriteInteger(plant.RemainingGrowthSeconds);
            packet.WriteBoolean(plant.BreedingPermission);

            return;
        }

        packet.WriteBoolean(false);
        packet.WriteBoolean(true);
        packet.WriteBoolean(false);
        packet.WriteInteger(0);
        packet.WriteInteger(-1);
        packet.WriteInteger(-1);
        packet.WriteInteger(-1);
        packet.WriteBoolean(false);
    }
}
