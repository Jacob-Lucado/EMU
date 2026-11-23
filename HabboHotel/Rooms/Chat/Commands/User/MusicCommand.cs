using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Music;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class MusicCommand(IRoomStreamLog log) : IChatCommand
{
    public string Key => "music";

    public string Parameters => "%link% | add %link% | skip | queue | clear | stop | suggest %link% | approve | decline | suggestions | top | history";

    public string Description => "Play a web audio stream, a SoundCloud link or a YouTube video for everyone in the room. Use :music add to line songs up, :music skip for the next one and :music stop to turn it off. Anyone can use :music suggest to offer a song and :music top or :music history to see what has played.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var text = parameters.Length > 0 ? parameters[0] : "";
        var playlist = room.Playlist;
        var now = DateTimeOffset.UtcNow;

        if (text.Equals("suggest", StringComparison.OrdinalIgnoreCase)) {
            Suggest(session, room, parameters.Length > 1 ? parameters[1] : "");

            return;
        }

        if (text.Equals("top", StringComparison.OrdinalIgnoreCase)) {
            ShowTop(session);

            return;
        }

        if (text.Equals("history", StringComparison.OrdinalIgnoreCase)) {
            ShowHistory(session, room);

            return;
        }

        if (!room.CheckRights(session)) {
            session.SendWhisper("Only people with rights in this room can change the music. You can offer a song with :music suggest <link>.");

            return;
        }

        switch (text.ToLowerInvariant()) {
            case "stop" or "off":
                playlist.Stop();
                RoomMusic.Broadcast(room);
                Announce(session, room, $"{Describe(session, room)} turned the room music off.");

                return;
            case "skip":
                if (playlist.Current == null) {
                    session.SendWhisper("No music is playing.");

                    return;
                }

                playlist.Skip(now);
                RecordCurrent(room);
                RoomMusic.Broadcast(room);
                Announce(session, room, playlist.Current == null
                    ? $"{Describe(session, room)} skipped the last song, so the music is off."
                    : $"{Describe(session, room)} skipped to the next song.");

                return;
            case "clear":
                playlist.ClearQueue();
                RoomMusic.Broadcast(room);
                session.SendWhisper("The song line is empty. The current song keeps playing.");

                return;
            case "queue" or "list":
                ShowQueue(session, playlist);

                return;
            case "suggestions":
                ShowSuggestions(session, playlist);

                return;
            case "approve":
                Approve(session, room, parameters.Length > 1 ? parameters[1] : "", now);

                return;
            case "decline":
                Decline(session, playlist, parameters.Length > 1 ? parameters[1] : "");

                return;
            case "add" or "queue-add":
                Add(session, room, parameters.Length > 1 ? parameters[1] : "", now);

                return;
        }

        var stream = RoomStream.Parse(text, session.GetHabbo().Username);

        if (stream == null) {
            session.SendWhisper("Paste a link that starts with https://, like :music https://soundcloud.com/artist/song or a YouTube link. Use :music add to line songs up and :music stop to turn it off.");

            return;
        }

        playlist.Play(stream, now);
        log.Record(room.RoomId, stream);
        RoomMusic.Broadcast(room);
        Announce(session, room, $"{Describe(session, room)} started {Kind(stream)} for the room.");
    }

    private void ShowHistory(GameClient session, Room room)
    {
        var recent = log.Recent(room.RoomId, 5);

        if (recent.Count == 0) {
            session.SendWhisper("No songs have been played in this room yet.");

            return;
        }

        session.SendWhisper("Played in this room, newest first:");

        foreach (var song in recent) {
            session.SendWhisper($"{song.Url} (by {song.SetBy})");
        }
    }

    private void ShowTop(GameClient session)
    {
        var top = log.Top(5);

        if (top.Count == 0) {
            session.SendWhisper("No songs have been played this week.");

            return;
        }

        session.SendWhisper("Top songs this week:");

        for (var i = 0; i < top.Count; i++) {
            session.SendWhisper($"{i + 1}. {top[i].Url} ({top[i].Plays} plays)");
        }
    }

    private static void Suggest(GameClient session, Room room, string text)
    {
        var stream = RoomStream.Parse(text, session.GetHabbo().Username);

        if (stream == null) {
            session.SendWhisper("Paste a link that starts with https://, like :music suggest https://soundcloud.com/artist/song");

            return;
        }

        if (room.CheckRights(session)) {
            session.SendWhisper("You have rights here, so add it yourself with :music add <link>.");

            return;
        }

        if (!room.Playlist.Suggest(stream)) {
            session.SendWhisper("There are too many suggestions waiting. Try again in a bit.");

            return;
        }

        session.SendWhisper("Your song was sent to the people who can change the music here.");
        NotifyRights(session, room, $"{session.GetHabbo().Username} suggests {Kind(stream)}. Type :music approve {session.GetHabbo().Username} to play it or :music decline {session.GetHabbo().Username} to skip it.");
    }

    private void Approve(GameClient session, Room room, string name, DateTimeOffset now)
    {
        var stream = room.Playlist.TakeSuggestion(name);

        if (stream == null) {
            session.SendWhisper(string.IsNullOrWhiteSpace(name) ? "No suggestions are waiting." : $"{name} has no suggestion waiting.");

            return;
        }

        if (!room.Playlist.Add(stream, now, out var startedNow)) {
            room.Playlist.Suggest(stream);
            session.SendWhisper($"The song line is full ({RoomPlaylist.MaxQueued} songs). Use :music skip or :music clear first.");

            return;
        }

        if (startedNow) {
            log.Record(room.RoomId, stream);
        }

        RoomMusic.Broadcast(room);
        Announce(session, room, startedNow
            ? $"{Describe(session, room)} approved {stream.SetBy}'s suggestion, and it is playing now."
            : $"{Describe(session, room)} approved {stream.SetBy}'s suggestion ({room.Playlist.Queued} waiting).");
    }

    private static void Decline(GameClient session, RoomPlaylist playlist, string name)
    {
        if (name.Equals("all", StringComparison.OrdinalIgnoreCase)) {
            playlist.ClearSuggestions();
            session.SendWhisper("All suggestions were removed.");

            return;
        }

        var stream = playlist.TakeSuggestion(name);

        session.SendWhisper(stream == null
            ? (string.IsNullOrWhiteSpace(name) ? "No suggestions are waiting." : $"{name} has no suggestion waiting.")
            : $"Removed {stream.SetBy}'s suggestion.");
    }

    private static void ShowSuggestions(GameClient session, RoomPlaylist playlist)
    {
        var suggestions = playlist.Suggestions;
        session.SendWhisper(suggestions.Count == 0
            ? "No suggestions are waiting."
            : $"Suggestions: {string.Join("  ", suggestions.Select(s => $"{s.SetBy}: {s.Url}"))}. Use :music approve <name> or :music decline <name>.");
    }

    private static void NotifyRights(GameClient from, Room room, string message)
    {
        var speaker = room.GetRoomUserManager().GetRoomUserByHabbo(from.GetHabbo().Username);

        if (speaker == null) {
            return;
        }

        foreach (var user in room.GetRoomUserManager().GetRoomUsers()) {
            if (user.IsBot || user.GetClient() is not { } client || client.GetHabbo() is not { ReceiveWhispers: true } || !room.CheckRights(client)) {
                continue;
            }

            client.Send(new WhisperComposer(speaker.VirtualId, message, 0, speaker.LastBubble, user.VirtualId));
        }
    }

    private void RecordCurrent(Room room)
    {
        if (room.Playlist.Current is { } current) {
            log.Record(room.RoomId, current);
        }
    }

    private void Add(GameClient session, Room room, string text, DateTimeOffset now)
    {
        var stream = RoomStream.Parse(text, session.GetHabbo().Username);

        if (stream == null) {
            session.SendWhisper("Paste a link that starts with https://, like :music add https://soundcloud.com/artist/song");

            return;
        }

        if (!room.Playlist.Add(stream, now, out var startedNow)) {
            session.SendWhisper($"The song line is full ({RoomPlaylist.MaxQueued} songs). Use :music skip or :music clear.");

            return;
        }

        if (startedNow) {
            log.Record(room.RoomId, stream);
        }

        RoomMusic.Broadcast(room);
        Announce(session, room, startedNow
            ? $"{Describe(session, room)} started {Kind(stream)} for the room."
            : $"{Describe(session, room)} added {Kind(stream)} to the song line ({room.Playlist.Queued} waiting).");
    }

    private static void ShowQueue(GameClient session, RoomPlaylist playlist)
    {
        if (playlist.Current == null) {
            session.SendWhisper("No music is playing.");

            return;
        }

        var upcoming = playlist.Upcoming;
        var lines = upcoming.Select((song, index) => $"{index + 1}. {song.Url} (added by {song.SetBy})");
        session.SendWhisper(upcoming.Count == 0
            ? $"Now playing: {playlist.Current.Url}. Nothing is waiting."
            : $"Now playing: {playlist.Current.Url}. Up next: {string.Join("  ", lines)}");
    }

    private static string Kind(RoomStream stream) =>
        stream.Kind switch { "soundcloud" => "a SoundCloud track", "youtube" => "a YouTube video", _ => "a music stream" };

    private static string Describe(GameClient session, Room room) =>
        $"{session.GetHabbo().Username} ({(room.CheckRights(session, true) ? "room owner" : "has room rights")})";

    private static void Announce(GameClient session, Room room, string message)
    {
        var speaker = room.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Username);

        if (speaker == null) {
            return;
        }

        foreach (var user in room.GetRoomUserManager().GetRoomUsers()) {
            if (user.IsBot || user.GetClient()?.GetHabbo() is not { ReceiveWhispers: true }) {
                continue;
            }

            user.GetClient().Send(new WhisperComposer(speaker.VirtualId, message, 0, speaker.LastBubble, user.VirtualId));
        }
    }
}
