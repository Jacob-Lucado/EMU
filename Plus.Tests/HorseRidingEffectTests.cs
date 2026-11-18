using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.AI;
using Xunit;

namespace Plus.Tests;

public sealed class HorseRidingEffectTests
{
    private static readonly ItemDefinition Saddle = new() { Id = 7100, ItemName = "horse_saddle1", InteractionType = InteractionType.HorseSaddle1, CustomParams = "15 4 9 0 77" };
    private static readonly ItemDefinition SaddleStand = new() { Id = 4450, ItemName = "horse_saddle2", InteractionType = InteractionType.HorseSaddle2, CustomParams = "15 4 10 0 103" };

    [Theory]
    [InlineData(9, 77)]
    [InlineData(10, 103)]
    public void RiderGetsTheEffectOfTheHorsesSaddle(int saddle, int effect)
    {
        Assert.Equal(effect, HorseProducts.RidingEffect([Saddle, SaddleStand], saddle));
    }

    [Fact]
    public void UnknownSaddleRidesWithTheRidingEffect()
    {
        Assert.Equal(77, HorseProducts.RidingEffect([SaddleStand], 9));
    }
}
