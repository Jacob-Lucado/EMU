using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Pets;

/// <summary>One seed of a breeding result. <see cref="None"/> fills the place of a seed nobody got.</summary>
public sealed record PetBreedingResultSeed(int StuffId, int ClassId, string ProductCode, int UserId, string UserName, int Rarity, bool HasMutation)
{
    public static PetBreedingResultSeed None { get; } = new(-1, -1, "", -1, "", -1, false);
}

public sealed class PetBreedingResultComposer(PetBreedingResultSeed first, PetBreedingResultSeed second) : IServerPacket
{
    public uint MessageId => ServerPacketHeader.PetBreedingResultComposer;

    public void Compose(IOutgoingPacket packet)
    {
        Write(packet, first);
        Write(packet, second);
    }

    private static void Write(IOutgoingPacket packet, PetBreedingResultSeed seed)
    {
        packet.WriteInteger(seed.StuffId);
        packet.WriteInteger(seed.ClassId);
        packet.WriteString(seed.ProductCode);
        packet.WriteInteger(seed.UserId);
        packet.WriteString(seed.UserName);
        packet.WriteInteger(seed.Rarity);
        packet.WriteBoolean(seed.HasMutation);
    }
}
