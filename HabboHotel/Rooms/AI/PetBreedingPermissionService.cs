using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.AI;

public interface IPetBreedingPermissionStore
{
    void Save(int petId, bool allowed);
}

public interface IPetBreedingPermissionService
{
    void Toggle(Room room, GameClient session, int petId);
}

public sealed class PetBreedingPermissionService(IPetBreedingPermissionStore store) : IPetBreedingPermissionService
{
    public void Toggle(Room room, GameClient session, int petId) => throw new NotImplementedException();
}
