using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Televisions;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class TvAddCommand(IDatabase database, ITelevisionManager televisions) : IChatCommand
{
    public string Key => "tvadd";

    public string Parameters => "%youtube link%";

    public string Description => "Add a YouTube video to the TV playlist without playing it.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        if (!room.CheckRights(session)) {
            session.SendWhisper("Only people with rights in this room can add to the TV playlist.");

            return;
        }

        var videoId = TvCommand.VideoIdFrom(parameters);

        if (videoId == null) {
            session.SendWhisper("Paste a YouTube link, like :tvadd https://youtu.be/dQw4w9WgXcQ");

            return;
        }

        TvCommand.AddToPlaylist(database, videoId, session.GetHabbo().Username);
        televisions.Init();
        session.SendWhisper("Done! The video is on the TV playlist.");
    }
}
