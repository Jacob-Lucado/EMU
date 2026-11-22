using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Music;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class MusicCommand : IChatCommand
{
    public string Key => "music";

    public string Parameters => "%stream, soundcloud or youtube link%";

    public string Description => "Play a web audio stream, a SoundCloud link or a YouTube video for everyone in the room. Use :music stop to turn it off.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        if (!room.CheckRights(session)) {
            session.SendWhisper("Only people with rights in this room can change the music.");

            return;
        }

        var text = parameters.Length > 0 ? parameters[0] : "";

        if (text.Equals("stop", StringComparison.OrdinalIgnoreCase) || text.Equals("off", StringComparison.OrdinalIgnoreCase)) {
            room.Stream = null;
            room.SendPacket(new RoomStreamComposer(room.RoomId, null));
            Announce(session, room, $"{Describe(session, room)} turned the room music off.");

            return;
        }

        var stream = RoomStream.Parse(text, session.GetHabbo().Username);

        if (stream == null) {
            session.SendWhisper("Paste a link that starts with https://, like :music https://soundcloud.com/artist/song or a YouTube link. Use :music stop to turn it off.");

            return;
        }

        room.Stream = stream;
        room.SendPacket(new RoomStreamComposer(room.RoomId, stream));
        Announce(session, room, $"{Describe(session, room)} started {(stream.Kind switch { "soundcloud" => "a SoundCloud track", "youtube" => "a YouTube video", _ => "a music stream" })} for the room.");
    }

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
