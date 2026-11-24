using Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;
using Xunit;

namespace Plus.Tests;

public class RoomSocialTests
{
    [Fact]
    public void EightBallOnlyGivesKnownAnswers()
    {
        var random = new Random(7);
        var seen = Enumerable.Range(0, 2000).Select(_ => RoomSocial.EightBall(random)).ToHashSet();

        Assert.Equal(RoomSocial.EightBallAnswers.Length, seen.Count);
        Assert.All(seen, message => Assert.Contains(RoomSocial.EightBallAnswers, answer => message == $"*shakes the magic 8 ball: {answer}*"));
    }

    [Fact]
    public void EightBallHasTwentyDistinctAnswers()
    {
        Assert.Equal(20, RoomSocial.EightBallAnswers.Distinct().Count());
    }

    [Fact]
    public void HugNamesTheUser()
    {
        Assert.Equal("*hugs Alice*", RoomSocial.Hug("Alice"));
    }

    [Fact]
    public void HighFiveAndPokeNameTheUser()
    {
        Assert.Equal("*high fives Alice*", RoomSocial.HighFive("Alice"));
        Assert.Equal("*pokes Alice*", RoomSocial.Poke("Alice"));
    }
}
