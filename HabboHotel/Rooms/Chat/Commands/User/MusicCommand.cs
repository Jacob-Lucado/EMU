using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Music;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class MusicCommand : IChatCommand
{
    public string Key => "music";

    public string Parameters => "%stream or soundcloud link%";

    public string Description => "Play a web audio stream or a SoundCloud link for everyone in the room. Use :music stop to turn it off.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        if (!room.CheckRights(session)) {
            session.SendWhisper("Only people with rights in this room can change the music.");

            return;
        }

        var text = parameters.Length > 0 ? parameters[0] : "";

        if (text.Equals("stop", StringComparison.OrdinalIgnoreCase) || text.Equals("off", StringComparison.OrdinalIgnoreCase)) {
            room.Stream = null;
            room.SendPacket(new RoomStreamComposer(null));
            session.SendWhisper("The room music is off.");

            return;
        }

        var stream = RoomStream.Parse(text, session.GetHabbo().Username);

        if (stream == null) {
            session.SendWhisper("Paste a link that starts with https://, like :music https://soundcloud.com/artist/song. Use :music stop to turn it off.");

            return;
        }

        room.Stream = stream;
        room.SendPacket(new RoomStreamComposer(stream));
        session.SendWhisper(stream.Kind == "soundcloud" ? "Done! Playing that SoundCloud link for everyone in the room." : "Done! Playing that stream for everyone in the room.");
    }
}
