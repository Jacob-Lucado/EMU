using Plus.HabboHotel.Rooms.Music;
using Xunit;

namespace Plus.Tests;

public class RoomPlaylistTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    private static RoomStream Song(string name) => new("soundcloud", $"https://soundcloud.com/a/{name}", "Jacob");

    [Fact]
    public void AddingToAnIdleRoomStartsTheSong()
    {
        var playlist = new RoomPlaylist();

        Assert.True(playlist.Add(Song("one"), Start, out var startedNow));
        Assert.True(startedNow);
        Assert.Equal("https://soundcloud.com/a/one", playlist.Current?.Url);
        Assert.Equal(0, playlist.Queued);
    }

    [Fact]
    public void AddingWhileSomethingPlaysQueuesTheSong()
    {
        var playlist = new RoomPlaylist();
        playlist.Play(Song("one"), Start);

        Assert.True(playlist.Add(Song("two"), Start, out var startedNow));
        Assert.False(startedNow);
        Assert.Equal(1, playlist.Queued);
        Assert.Equal("https://soundcloud.com/a/one", playlist.Current?.Url);
    }

    [Fact]
    public void TheLineHoldsAtMostTwentySongs()
    {
        var playlist = new RoomPlaylist();
        playlist.Play(Song("now"), Start);

        for (var i = 0; i < RoomPlaylist.MaxQueued; i++) {
            Assert.True(playlist.Add(Song($"s{i}"), Start, out _));
        }

        Assert.False(playlist.Add(Song("extra"), Start, out _));
        Assert.Equal(RoomPlaylist.MaxQueued, playlist.Queued);
    }

    [Fact]
    public void SkipPlaysTheNextSongThenStops()
    {
        var playlist = new RoomPlaylist();
        playlist.Play(Song("one"), Start);
        playlist.Add(Song("two"), Start, out _);

        playlist.Skip(Start);
        Assert.Equal("https://soundcloud.com/a/two", playlist.Current?.Url);

        playlist.Skip(Start);
        Assert.Null(playlist.Current);
    }

    [Fact]
    public void EveryNewSongGetsANewToken()
    {
        var playlist = new RoomPlaylist();
        playlist.Play(Song("one"), Start);
        var first = playlist.Token;
        playlist.Add(Song("two"), Start, out _);
        playlist.Skip(Start);

        Assert.NotEqual(first, playlist.Token);
    }

    [Fact]
    public void AnEndedReportMovesOnOnlyForTheCurrentSongAfterItHadTimeToPlay()
    {
        var playlist = new RoomPlaylist();
        playlist.Play(Song("one"), Start);
        playlist.Add(Song("two"), Start, out _);
        var token = playlist.Token;

        Assert.False(playlist.Ended(token, Start.AddSeconds(1)));
        Assert.False(playlist.Ended(token + 1, Start.AddSeconds(30)));
        Assert.True(playlist.Ended(token, Start.AddSeconds(30)));
        Assert.Equal("https://soundcloud.com/a/two", playlist.Current?.Url);
        Assert.False(playlist.Ended(token, Start.AddSeconds(60)));
    }

    [Fact]
    public void StopEmptiesTheLineAndTheCurrentSong()
    {
        var playlist = new RoomPlaylist();
        playlist.Play(Song("one"), Start);
        playlist.Add(Song("two"), Start, out _);

        playlist.Stop();

        Assert.Null(playlist.Current);
        Assert.Equal(0, playlist.Queued);
    }

    [Fact]
    public void ClearKeepsTheCurrentSong()
    {
        var playlist = new RoomPlaylist();
        playlist.Play(Song("one"), Start);
        playlist.Add(Song("two"), Start, out _);

        playlist.ClearQueue();

        Assert.NotNull(playlist.Current);
        Assert.Equal(0, playlist.Queued);
    }
}
