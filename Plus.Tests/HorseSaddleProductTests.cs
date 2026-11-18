using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.AI;
using Xunit;

namespace Plus.Tests;

public sealed class HorseSaddleProductTests
{
    private static readonly ItemDefinition Postit = new() { Id = 4221, SpriteId = 4221, ItemName = "post_it", InteractionType = InteractionType.Postit };
    private static readonly ItemDefinition Saddle = new() { Id = 7100, SpriteId = 4221, ItemName = "horse_saddle1", InteractionType = InteractionType.HorseSaddle1, CustomParams = "15 4 9 0 77" };
    private static readonly ItemDefinition SaddleStand = new() { Id = 4450, SpriteId = 4450, ItemName = "horse_saddle2", InteractionType = InteractionType.HorseSaddle2, CustomParams = "15 4 10 0 103" };
    private static readonly ItemDefinition HairDye = new() { Id = 4232, ItemName = "horse_hairdye_00", InteractionType = InteractionType.HorseHairDye, CustomParams = "15 2,3 9,9" };

    [Theory]
    [InlineData(9, 7100u)]
    [InlineData(10, 4450u)]
    public void SaddleComesBackAsTheProductThatPutItOn(int saddle, uint furniture)
    {
        Assert.Equal(furniture, HorseProducts.SaddleProduct([Postit, HairDye, SaddleStand, Saddle], saddle)?.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void HorseWithoutAKnownSaddleGetsNothingBack(int saddle)
    {
        Assert.Null(HorseProducts.SaddleProduct([Postit, HairDye, SaddleStand, Saddle], saddle));
    }
}
