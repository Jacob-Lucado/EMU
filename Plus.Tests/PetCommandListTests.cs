using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Rooms.AI;
using Xunit;

namespace Plus.Tests;

// Each Habbo pet type has its own command list and number of commands a level 1 pet already knows.
public partial class PlacedFurniRoomTests
{
    public static TheoryData<int, int[], int> HabboCommandLists => new()
    {
        { 1, [46, 0, 1, 14, 43, 2, 3, 4, 17, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 16, 24, 25, 26], 5 },
        { 8, [0, 2, 3, 17, 5, 6, 7, 23, 9, 10, 11, 24, 25, 26, 13, 14, 19, 20, 22, 21, 15, 16], 2 },
        { 11, [0, 1, 2, 3, 6, 4, 28, 5, 30, 9, 7, 27, 13, 17, 29, 31, 15, 16], 2 },
        { 17, [0, 2, 13, 3, 6, 11, 7, 17, 15, 16], 2 },
        { 22, [0, 1, 2, 3, 10, 5, 9, 7, 11, 13, 15, 16, 34, 32, 33], 2 },
        { 28, [0, 1, 14, 43, 2, 3, 4, 17, 6, 7, 8, 11, 13, 12, 15, 16, 24, 25, 26], 4 },
        { 30, [0, 1, 14, 43, 2, 3, 4, 11, 17, 6, 7, 8, 10, 12, 13, 15, 16, 24, 25, 26], 4 }
    };

    [Theory]
    [MemberData(nameof(HabboCommandLists))]
    public void ALevelOnePetGetsItsTypesCommandList(int type, int[] commands, int learned)
    {
        LegacyRider();
        var pet = CommandPet(type, 0, 100);
        _client.Packets.Clear();

        new PetInformationService(TimeProvider.System).SendTrainingPanel(_client, pet.PetId);

        var (_, all, enabled) = TrainingPanel(Assert.Single(_client.Packets, packet => packet.Header == ServerPacketHeader.PetTrainingPanelComposer).Body);
        Assert.Equal(commands, all);
        Assert.Equal(commands.Take(learned), enabled);
    }
}
