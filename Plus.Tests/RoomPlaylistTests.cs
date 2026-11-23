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

    [Fact]
    public void ASuggestionIsKeptUntilItIsTaken()
    {
        var playlist = new RoomPlaylist();

        Assert.True(playlist.Suggest(Song("one") with { SetBy = "Anna" }));
        Assert.True(playlist.Suggest(Song("two") with { SetBy = "Ben" }));

        Assert.Equal("Anna", playlist.TakeSuggestion(null)?.SetBy);
        Assert.Equal("Ben", playlist.TakeSuggestion("ben")?.SetBy);
        Assert.Null(playlist.TakeSuggestion(null));
    }

    [Fact]
    public void ANewSuggestionFromTheSamePersonReplacesTheOldOne()
    {
        var playlist = new RoomPlaylist();
        playlist.Suggest(Song("one") with { SetBy = "Anna" });
        playlist.Suggest(Song("two") with { SetBy = "anna" });

        Assert.Single(playlist.Suggestions);
        Assert.Equal("https://soundcloud.com/a/two", playlist.Suggestions[0].Url);
    }

    [Fact]
    public void TheSuggestionListHoldsAtMostTenPeople()
    {
        var playlist = new RoomPlaylist();

        for (var i = 0; i < RoomPlaylist.MaxSuggestions; i++) {
            Assert.True(playlist.Suggest(Song($"s{i}") with { SetBy = $"User{i}" }));
        }

        Assert.False(playlist.Suggest(Song("extra") with { SetBy = "Late" }));
        Assert.True(playlist.Suggest(Song("again") with { SetBy = "User3" }));
    }

    [Fact]
    public void ClearingSuggestionsLeavesTheLineAlone()
    {
        var playlist = new RoomPlaylist();
        playlist.Play(Song("now"), Start);
        playlist.Add(Song("next"), Start, out _);
        playlist.Suggest(Song("idea") with { SetBy = "Anna" });

        playlist.ClearSuggestions();

        Assert.Empty(playlist.Suggestions);
        Assert.Equal(1, playlist.Queued);
    }
}
