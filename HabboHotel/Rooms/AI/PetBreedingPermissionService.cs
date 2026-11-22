using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.AI;

public interface IPetBreedingPermissionStore
{
    void Save(Pet pet, bool allowed);
}

public sealed class PetBreedingPermissionStore(IDatabase database) : IPetBreedingPermissionStore
{
    public void Save(Pet pet, bool allowed)
    {
        using var connection = database.Connection();

        var sql = pet.Plant == null
            ? "UPDATE bots_petdata SET allow_breeding = @allowed WHERE id = @petId LIMIT 1"
            : "UPDATE bots_monsterplants SET breeding_permission = @allowed WHERE pet_id = @petId LIMIT 1";

        if (connection.Execute(sql, new { allowed, petId = pet.PetId }) != 1) {
            throw new InvalidOperationException("The breeding permission was not persisted.");
        }
    }
}

public interface IPetBreedingPermissionService
{
    void Toggle(Room room, GameClient session, int petId);
}

public sealed class PetBreedingPermissionService(IPetBreedingPermissionStore store, TimeProvider? clock = null) : IPetBreedingPermissionService
{
    public void Toggle(Room room, GameClient session, int petId)
    {
        if (!room.GetRoomUserManager().TryGetPet(petId, out var pet) || pet.PetData is not { } data || data.OwnerId != session.GetHabbo().Id) {
            return;
        }

        if (data.Plant != null) {
            lock (room.MonsterPlantSync) {
                var plant = data.Plant;
                var now = (clock ?? TimeProvider.System).GetUtcNow();

                if (!plant.CanBreedNow(now)) {
                    return;
                }

                var permission = !plant.BreedingPermission;
                store.Save(data, permission);
                plant.BreedingPermission = permission;
                room.SendPacket(new PetStatusUpdateComposer(pet.VirtualId, data.PetId, true, false, false, permission));
            }

            return;
        }

        var allowed = !data.AllowBreeding;
        store.Save(data, allowed);
        data.AllowBreeding = allowed;
        room.SendPacket(new PetStatusUpdateComposer(pet.VirtualId, data.PetId, false, false, false, allowed));
    }
}
