using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RiderIgnoresAWalkToASeatOrABed(bool bed)
    {
        var item = Furni(10, bed ? InteractionType.Bed : InteractionType.None, WiredBoxType.None);
        item.Definition.Width = 1;
        item.Definition.Length = bed ? 3 : 1;
        item.Definition.Height = bed ? 1.6 : 1.0;
        item.Definition.IsSeat = !bed;
        Assert.True(_room.GetRoomItemHandler().SetFloorItem(null!, item, 2, 0, 0, true, false, false));
        var rider = ExecutorActor(0, 1);
        var horse = ExternalLifecycleHorse(0, 1);
        ExecutorTick();
        EstablishExternalMountedGroup(rider, horse);
        ExecutorTick();

        rider.MoveTo(2, bed ? 1 : 0);

        for (var tick = 0; tick < 10; tick++) {
            ExecutorTick();
        }

        Assert.Equal((0, 1), (rider.X, rider.Y));
        Assert.Equal((0, 1), (horse.X, horse.Y));
        Assert.False(rider.HasStatus("sit"));
        Assert.False(rider.HasStatus("lay"));
        Assert.True(rider.RidingHorse);
    }

    [Fact]
    public void RiderStillWalksToAFreeTile()
    {
        var rider = ExecutorActor(0, 1);
        var horse = ExternalLifecycleHorse(0, 1);
        ExecutorTick();
        EstablishExternalMountedGroup(rider, horse);
        ExecutorTick();

        rider.MoveTo(3, 3);

        for (var tick = 0; tick < 10; tick++) {
            ExecutorTick();
        }

        Assert.Equal((3, 3), (rider.X, rider.Y));
        Assert.Equal((3, 3), (horse.X, horse.Y));
    }
}
