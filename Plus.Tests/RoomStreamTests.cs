using System.Buffers.Binary;
using System.Text;
using Plus.Communication.Flash;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;
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

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?t=10")]
    [InlineData("https://music.youtube.com/watch?v=dQw4w9WgXcQ&list=abc")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    public void AYouTubeLinkBecomesAVideoWithACleanLink(string link)
    {
        var stream = RoomStream.Parse(link, "Jacob");

        Assert.NotNull(stream);
        Assert.Equal("youtube", stream.Kind);
        Assert.Equal("https://www.youtube.com/watch?v=dQw4w9WgXcQ", stream.Url);
    }

    [Theory]
    [InlineData("https://www.youtube.com/@somechannel")]
    [InlineData("https://www.youtube.com/watch?v=short")]
    [InlineData("https://notyoutube.com/watch?v=dQw4w9WgXcQ")]
    public void OtherYouTubeLinksAreNotVideos(string link)
    {
        var stream = RoomStream.Parse(link, "Jacob");

        Assert.True(stream == null || stream.Kind != "youtube");
    }

    [Fact]
    public void ALongLinkIsRefused() => Assert.Null(RoomStream.Parse("https://example.com/" + new string('a', 600), "Jacob"));

    [Fact]
    public void TheStreamPacketNamesTheRoomItBelongsTo()
    {
        using var memory = PlusMemoryStream.GetStream();
        new RoomStreamComposer(42, RoomStream.Parse("https://soundcloud.com/artist/song", "Jacob")).Compose(new FlashOutgoingPacket(memory));
        var body = memory.ToArray()[6..];

        Assert.Equal(42, BinaryPrimitives.ReadInt32BigEndian(body));
        Assert.Contains("soundcloud", Encoding.UTF8.GetString(body[4..]));
    }

    [Fact]
    public void AStopPacketStillNamesTheRoom()
    {
        using var memory = PlusMemoryStream.GetStream();
        new RoomStreamComposer(7, null).Compose(new FlashOutgoingPacket(memory));

        Assert.Equal(7, BinaryPrimitives.ReadInt32BigEndian(memory.ToArray()[6..]));
    }

    [Fact]
    public void TheStreamPacketEndsWithTheSongTokenAndTheNumberWaiting()
    {
        using var memory = PlusMemoryStream.GetStream();
        new RoomStreamComposer(42, null, 9, 3).Compose(new FlashOutgoingPacket(memory));
        var bytes = memory.ToArray();

        Assert.Equal(9, BinaryPrimitives.ReadInt32BigEndian(bytes[^8..]));
        Assert.Equal(3, BinaryPrimitives.ReadInt32BigEndian(bytes[^4..]));
    }

    [Theory]
    [InlineData("https://www.youtube.com/playlist?list=PLabcdefghijk123", "https://www.youtube.com/playlist?list=PLabcdefghijk123")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLabcdefghijk123&index=2", "https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLabcdefghijk123")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?list=PLabcdefghijk123", "https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLabcdefghijk123")]
    public void AYouTubeLinkKeepsItsPlaylist(string link, string url)
    {
        Assert.Equal(url, RoomStream.Parse(link, "Jacob")?.Url);
    }

    [Fact]
    public void AYouTubePlaylistIsBannedAsAWhole()
    {
        var stream = RoomStream.Parse("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLabcdefghijk123", "Jacob")!;

        Assert.Equal(["youtube-list:PLabcdefghijk123", "youtube:dQw4w9WgXcQ"], stream.BanKeys);
    }

    [Fact]
    public void ASoundCloudLinkIsBannedWhateverItsQueryOrCase()
    {
        var first = RoomStream.Parse("https://soundcloud.com/Jacob-LLLL/sets/related-tracks-beyonce-crazy?si=abc", "A")!;
        var second = RoomStream.Parse("https://www.soundcloud.com/jacob-llll/sets/related-tracks-beyonce-crazy/", "B")!;

        Assert.Equal(first.BanKeys, second.BanKeys);
    }

    [Fact]
    public void RemovingASongSkipsItWhenItIsPlaying()
    {
        var playlist = new RoomPlaylist();
        var bad = RoomStream.Parse("https://soundcloud.com/a/bad", "Jacob")!;
        var good = RoomStream.Parse("https://soundcloud.com/a/good", "Jacob")!;
        var now = DateTimeOffset.UtcNow;

        playlist.Add(bad, now, out _);
        playlist.Add(good, now, out _);
        playlist.Add(bad, now, out _);

        var removed = playlist.Remove(song => song.BanKeys.Contains(bad.BanKeys[0]), now);

        Assert.Equal(2, removed.Count);
        Assert.Equal(good, playlist.Current);
        Assert.Equal(0, playlist.Queued);
    }
}
