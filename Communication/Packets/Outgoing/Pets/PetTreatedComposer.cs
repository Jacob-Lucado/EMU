using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Pets;

/// <summary>Respect for a pet: respect count, owner id, then the pet entry (id, name, figure, level, rarity).</summary>
public sealed class PetTreatedComposer(int respect, int ownerId, int petId, string name, int type, int race, string color,
    string customParts, int level, int rarity) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.RespectPetNotificationComposer;

    public void Compose(IOutgoingPacket packet) => throw new NotImplementedException();
}
