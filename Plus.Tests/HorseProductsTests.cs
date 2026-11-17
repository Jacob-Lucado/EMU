using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.AI;
using Xunit;

namespace Plus.Tests;

public sealed class HorseProductsTests
{
    private static readonly HorseStyle Plain = new(Palette: 7, Hair: -1, HairPalette: 0, Saddle: 0);

    [Theory]
    [InlineData("15 2,3 50,50", 50)]
    [InlineData("15 3,2 1,1", 1)]
    [InlineData("15 2,3 60,60", 60)]
    [InlineData("15 2,3 81,81", 81)]
    [InlineData("15 2,3 85,85", 85)]
    public void HairDyeSetsTheHairPaletteOfItsCustomParams(string customParams, int palette)
    {
        var style = HorseProducts.Apply(Plain with { Hair = 102, HairPalette = 53 }, InteractionType.HorseHairDye, customParams);

        Assert.Equal(Plain with { Hair = 102, HairPalette = palette }, style);
    }

    [Theory]
    [InlineData("15 2,3 -1,-1 1,1", -1)]
    [InlineData("15 2,3 101,101 1,1", 101)]
    [InlineData("15 2,3 107,107 1,1", 107)]
    public void HairstyleSetsTheHairPartAndKeepsTheHairColour(string customParams, int hair)
    {
        var style = HorseProducts.Apply(Plain with { Hair = 103, HairPalette = 55 }, InteractionType.HorseHairstyle, customParams);

        Assert.Equal(Plain with { Hair = hair, HairPalette = 55 }, style);
    }

    [Fact]
    public void HairstyleOnHairWithoutColourUsesTheProductColour()
    {
        var style = HorseProducts.Apply(Plain, InteractionType.HorseHairstyle, "15 2,3 104,104 1,1");

        Assert.Equal(Plain with { Hair = 104, HairPalette = 1 }, style);
    }

    [Theory]
    [InlineData(7, "15 Beige", 3)]
    [InlineData(2, "15 Brown", 6)]
    [InlineData(5, "15 Orange", 49)]
    [InlineData(64, "15 Grey", 17)]
    [InlineData(2, "15 Dark-Blue", 61)]
    [InlineData(46, "15 Lime", 77)]
    [InlineData(48, "15 Lime", 79)]
    [InlineData(37, "15 Blue", 89)]
    [InlineData(87, "15 Cyan", 74)]
    public void BodyDyeTakesTheColourOfItsTagAndKeepsTheBreed(int palette, string customParams, int dyed)
    {
        var style = HorseProducts.Apply(Plain with { Palette = palette, Hair = 101, HairPalette = 50, Saddle = 9 }, InteractionType.HorseBodyDye, customParams);

        Assert.Equal(new HorseStyle(dyed, 101, 50, 9), style);
    }

    [Fact]
    public void BodyDyeWithoutAHorseColourDoesNothing()
    {
        Assert.Null(HorseProducts.Apply(Plain, InteractionType.HorseBodyDye, "15 Null"));
    }

    [Theory]
    [InlineData(InteractionType.HorseSaddle1, "15 4 9 0 77", 9)]
    [InlineData(InteractionType.HorseSaddle2, "15 4 10 0 103", 10)]
    public void SaddleSetsTheSaddlePart(InteractionType product, string customParams, int saddle)
    {
        Assert.Equal(Plain with { Saddle = saddle }, HorseProducts.Apply(Plain, product, customParams));
    }

    [Theory]
    [InlineData(InteractionType.HorseHairDye, "14 2,3 50,50")]
    [InlineData(InteractionType.HorseBodyDye, "")]
    [InlineData(InteractionType.HorseHairstyle, "15 2,3")]
    [InlineData(InteractionType.None, "15 2,3 50,50")]
    public void ProductForAnotherPetOrWithoutParamsDoesNothing(InteractionType product, string customParams)
    {
        Assert.Null(HorseProducts.Apply(Plain, product, customParams));
    }
}
