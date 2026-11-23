using Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;
using Xunit;

namespace Plus.Tests;

public class RoomDiceTests
{
    [Theory]
    [InlineData(null, 1, 6)]
    [InlineData("", 1, 6)]
    [InlineData("20", 1, 20)]
    [InlineData("d12", 1, 12)]
    [InlineData("D8", 1, 8)]
    [InlineData(" 100 ", 1, 100)]
    [InlineData("1000", 1, 1000)]
    [InlineData("2", 1, 2)]
    [InlineData("2d6", 2, 6)]
    [InlineData("10D20", 10, 20)]
    [InlineData("3d2", 3, 2)]
    public void ParsesValidDice(string? text, int expectedCount, int expectedSides)
    {
        Assert.True(RoomDice.TryParse(text, out var count, out var sides));
        Assert.Equal(expectedCount, count);
        Assert.Equal(expectedSides, sides);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1001")]
    [InlineData("abc")]
    [InlineData("d")]
    [InlineData("99999999999")]
    [InlineData("11d6")]
    [InlineData("0d6")]
    [InlineData("-1d6")]
    [InlineData("2d1")]
    [InlineData("2d")]
    [InlineData("ad6")]
    [InlineData("2d6d6")]
    public void RejectsInvalidDice(string text) => Assert.False(RoomDice.TryParse(text, out _, out _));

    [Fact]
    public void OneDiceStaysInRangeAndHitsEveryFace()
    {
        var random = new Random(1);
        var seen = Enumerable.Range(0, 500).Select(_ => RoomDice.Roll(1, 4, random)).ToHashSet();

        Assert.Equal(4, seen.Count);
        Assert.All(seen, message => Assert.Matches(@"^\*rolls a d4 and gets [1-4]\*$", message));
    }

    [Fact]
    public void SeveralDiceShowEachRollAndTheTotal()
    {
        var random = new Random(3);

        for (var i = 0; i < 200; i++) {
            var message = RoomDice.Roll(3, 6, random);
            var match = System.Text.RegularExpressions.Regex.Match(message, @"^\*rolls 3d6 and gets (\d) \+ (\d) \+ (\d) = (\d+)\*$");

            Assert.True(match.Success, message);
            var rolls = match.Groups.Cast<System.Text.RegularExpressions.Group>().Skip(1).Take(3).Select(g => int.Parse(g.Value)).ToArray();
            Assert.All(rolls, roll => Assert.InRange(roll, 1, 6));
            Assert.Equal(rolls.Sum(), int.Parse(match.Groups[4].Value));
        }
    }

    [Fact]
    public void FlipGivesHeadsAndTails()
    {
        var random = new Random(2);
        var seen = Enumerable.Range(0, 100).Select(_ => RoomDice.Flip(random)).ToHashSet();

        Assert.Equal(["*flips a coin: heads*", "*flips a coin: tails*"], seen.OrderBy(x => x));
    }
}
