using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Music;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class MusicCommand : IChatCommand
{
    public string Key => "music";

    public string Parameters => "%link% | add %link% | skip | queue | clear | stop";

    public string Description => "Play a web audio stream, a SoundCloud link or a YouTube video for everyone in the room. Use :music add to line songs up, :music skip for the next one and :music stop to turn it off.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        if (!room.CheckRights(session)) {
            session.SendWhisper("Only people with rights in this room can change the music.");

            return;
        }

        var text = parameters.Length > 0 ? parameters[0] : "";
        var playlist = room.Playlist;
        var now = DateTimeOffset.UtcNow;

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
        RoomMusic.Broadcast(room);
        Announce(session, room, $"{Describe(session, room)} started {Kind(stream)} for the room.");
    }

    private static void Add(GameClient session, Room room, string text, DateTimeOffset now)
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
