using Plus.Communication.Packets.Outgoing.Rooms.Furni.YouTubeTelevisions;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.VideoScreens;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class ScreenCommand(IVideoScreens screens) : IChatCommand
{
    public string Key => "screen";

    public string Parameters => "[id] music | off | %youtube link%";

    public string Description => "Choose what a Video Screen shows: the room music (:screen music), its own YouTube video (:screen <link>) or nothing (:screen off). Add the furni id from its info box to change one screen, or leave it out to change every Video Screen in the room.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        if (!room.CheckRights(session)) {
            session.SendWhisper("Only people with rights in this room can change a Video Screen.");

            return;
        }

        var found = room.GetRoomItemHandler().GetWallAndFloor.Where(item => item.Definition.ItemName == Items.VideoScreens.VideoScreens.ItemName).ToList();

        if (found.Count == 0) {
            session.SendWhisper("There is no Video Screen in this room.");

            return;
        }

        var rest = parameters.ToList();
        var chosen = found;

        if (rest.Count > 0 && uint.TryParse(rest[0], out var id)) {
            chosen = found.Where(item => item.Id == id).ToList();
            rest.RemoveAt(0);

            if (chosen.Count == 0) {
                session.SendWhisper($"No Video Screen has the id {id}. The id is in its info box.");

                return;
            }
        }

        var text = rest.Count > 0 ? rest[0] : "";
        string? value = text.ToLowerInvariant() switch {
            "music" => Items.VideoScreens.VideoScreens.FollowMusic,
            "off" => "",
            _ => TvCommand.VideoIdOf(text),
        };

        if (text.Length == 0 || value == null) {
            session.SendWhisper($"Use :screen {Parameters}. Your screens: {string.Join(", ", found.Select(item => $"{item.Id} ({Describe(screens.Get(item.Id))})"))}.");

            return;
        }

        foreach (var item in chosen) {
            screens.Set(item.Id, value);
            room.SendPacket(new GetYouTubeVideoComposer((int)item.Id, value));
        }

        session.SendWhisper($"Done! {(chosen.Count == 1 ? "That screen now shows" : $"{chosen.Count} screens now show")} {Describe(value)}.");
    }

    private static string Describe(string value) => value switch {
        Items.VideoScreens.VideoScreens.FollowMusic => "the room music",
        "" => "nothing",
        _ => "its own video",
    };
}
