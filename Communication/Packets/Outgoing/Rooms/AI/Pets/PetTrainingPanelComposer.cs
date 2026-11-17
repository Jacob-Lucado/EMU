using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;

public class PetTrainingPanelComposer : IServerPacket
{
    private readonly int _petId;
    private readonly IReadOnlyList<int> _commands;
    private readonly IReadOnlyList<int> _learned;

    public uint MessageId => ServerPacketHeader.PetTrainingPanelComposer;

    public PetTrainingPanelComposer(Pet pet)
    {
        var commands = PetCommandSet.For(pet.Type);
        _petId = pet.PetId;
        _commands = commands.Commands;
        _learned = commands.Learned(pet.Level);
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_petId);
        packet.WriteInteger(_commands.Count);

        foreach (var command in _commands) {
            packet.WriteInteger(command);
        }

        packet.WriteInteger(_learned.Count);

        foreach (var command in _learned) {
            packet.WriteInteger(command);
        }
    }
}
