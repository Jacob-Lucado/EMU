using Plus.HabboHotel.Rooms.AI;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Fact]
    public void HorseWithoutASaddleCannotBeRidden()
    {
        var rider = ExecutorActor(1, 1);
        var horse = ExternalLifecycleHorse(2, 1);
        Assert.IsType<Pet>(horse.PetData).Saddle = 0;
        ExecutorTick();
        _client.Sent.Clear();

        RideExternalHorse(horse, mount: true);

        for (var tick = 0; tick < 6; tick++) {
            ExecutorTick();
        }

        Assert.False(rider.RidingHorse);
        Assert.False(horse.RidingHorse);
        Assert.Equal((1, 1), (rider.X, rider.Y));
        Assert.DoesNotContain(Plus.Communication.Packets.Outgoing.ServerPacketHeader.PetHorseFigureInformationComposer, _client.Sent);
    }
}
