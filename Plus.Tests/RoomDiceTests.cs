using Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;
using Xunit;

namespace Plus.Tests;

public class RoomDiceTests
{
    [Theory]
    [InlineData(null, 6)]
    [InlineData("", 6)]
    [InlineData("20", 20)]
    [InlineData("d12", 12)]
    [InlineData("D8", 8)]
    [InlineData(" 100 ", 100)]
    [InlineData("1000", 1000)]
    [InlineData("2", 2)]
    public void ParsesValidDice(string? text, int expected)
    {
        Assert.True(RoomDice.TryParseSides(text, out var sides));
        Assert.Equal(expected, sides);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1001")]
    [InlineData("abc")]
    [InlineData("d")]
    [InlineData("99999999999")]
    public void RejectsInvalidDice(string text) => Assert.False(RoomDice.TryParseSides(text, out _));

    [Fact]
    public void RollStaysInRangeAndHitsBothEnds()
    {
        var random = new Random(1);
        var seen = Enumerable.Range(0, 500).Select(_ => RoomDice.Roll(4, random)).ToHashSet();

        Assert.Equal(4, seen.Count);
        Assert.All(seen, message => Assert.Matches(@"^\*rolls a d4 and gets [1-4]\*$", message));
    }

    [Fact]
    public void FlipGivesHeadsAndTails()
    {
        var random = new Random(2);
        var seen = Enumerable.Range(0, 100).Select(_ => RoomDice.Flip(random)).ToHashSet();

        Assert.Equal(["*flips a coin: heads*", "*flips a coin: tails*"], seen.OrderBy(x => x));
    }
}
