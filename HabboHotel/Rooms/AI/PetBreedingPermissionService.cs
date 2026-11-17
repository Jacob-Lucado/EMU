using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.AI;

public interface IPetBreedingPermissionStore
{
    void Save(int petId, bool allowed);
}

public sealed class PetBreedingPermissionStore(IDatabase database) : IPetBreedingPermissionStore
{
    public void Save(int petId, bool allowed)
    {
        using var connection = database.Connection();

        if (connection.Execute("UPDATE bots_petdata SET allow_breeding = @allowed WHERE id = @petId LIMIT 1", new { allowed, petId }) != 1) {
            throw new InvalidOperationException("The breeding permission was not persisted.");
        }
    }
}

public interface IPetBreedingPermissionService
{
    void Toggle(Room room, GameClient session, int petId);
}

public sealed class PetBreedingPermissionService(IPetBreedingPermissionStore store) : IPetBreedingPermissionService
{
    public void Toggle(Room room, GameClient session, int petId)
    {
        if (!room.GetRoomUserManager().TryGetPet(petId, out var pet) || pet.PetData is not { } data || data.OwnerId != session.GetHabbo().Id) {
            return;
        }

        var allowed = !data.AllowBreeding;
        store.Save(data.PetId, allowed);
        data.AllowBreeding = allowed;
        room.SendPacket(new PetStatusUpdateComposer(pet.VirtualId, data.PetId, allowed));
    }
}
