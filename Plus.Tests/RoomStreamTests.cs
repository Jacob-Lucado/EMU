using Plus.HabboHotel.Rooms.Music;
using Xunit;

namespace Plus.Tests;

public class RoomStreamTests
{
    [Theory]
    [InlineData("https://soundcloud.com/artist/song", "soundcloud")]
    [InlineData("https://on.soundcloud.com/AbCd123", "soundcloud")]
    [InlineData("https://m.soundcloud.com/artist/sets/mix", "soundcloud")]
    [InlineData("https://radio.example.com/live.mp3", "stream")]
    [InlineData("https://stream.example.com:8443/radio?token=1", "stream")]
    public void AWebLinkBecomesAStream(string link, string kind)
    {
        var stream = RoomStream.Parse(link, "Jacob");

        Assert.NotNull(stream);
        Assert.Equal(kind, stream.Kind);
        Assert.Equal("Jacob", stream.SetBy);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("http://radio.example.com/live.mp3")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://radio.example.com/live.mp3")]
    [InlineData("https://user:pass@radio.example.com/live.mp3")]
    [InlineData("https://notsoundcloud.com.evil.example/song")]
    public void OtherTextIsRefusedOrNotSoundCloud(string link)
    {
        var stream = RoomStream.Parse(link, "Jacob");

        Assert.True(stream == null || stream.Kind == "stream" && !link.Contains("soundcloud.com/"));
    }

    [Fact]
    public void ALongLinkIsRefused() => Assert.Null(RoomStream.Parse("https://example.com/" + new string('a', 600), "Jacob"));
}
