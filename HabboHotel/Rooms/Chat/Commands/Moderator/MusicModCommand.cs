using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Music;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class MusicModCommand(IRoomStreamModeration moderation, IRoomManager rooms, IGameClientManager clients) : IChatCommand
{
    public string Key => "musicmod";

    public string Parameters => "ban [%link%] [reason] | unban %link% | list | strikes %username% | lift %username%";

    public string Description => "Moderate :music. Ban a song, playlist or stream (no link bans what plays in this room), unban it, see the banned list, check a user's strikes or lift their strikes and restriction.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var action = parameters.Length > 0 ? parameters[0].ToLowerInvariant() : "";
        var staff = session.GetHabbo().Username;

        switch (action) {
            case "ban":
                Ban(session, room, parameters, staff);

                return;
            case "unban":
                Unban(session, parameters);

                return;
            case "list":
                List(session);

                return;
            case "strikes" or "lift":
                Strikes(session, parameters, action == "lift");

                return;
        }

        session.SendWhisper($"Use :musicmod {Parameters}");
    }

    private void Ban(GameClient session, Room room, string[] parameters, string staff)
    {
        var stream = parameters.Length > 1 ? RoomStream.Parse(parameters[1], staff) : null;
        var reasonFrom = stream == null ? 1 : 2;

        stream ??= room.Playlist.Current;

        if (stream == null) {
            session.SendWhisper("Nothing is playing here. Use :musicmod ban <link> [reason].");

            return;
        }

        var reason = parameters.Length > reasonFrom ? string.Join(' ', parameters.Skip(reasonFrom)) : "";

        if (!moderation.Ban(stream, staff, reason)) {
            session.SendWhisper("That song is already banned.");

            return;
        }

        var key = stream.BanKeys[0];
        var now = DateTimeOffset.UtcNow;
        var removed = 0;
        var offenders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var other in rooms.GetRooms()) {
            if (other.Playlist.Current is { } current && current.BanKeys.Contains(key) && !current.SetBy.Equals(staff, StringComparison.OrdinalIgnoreCase)) {
                offenders.Add(current.SetBy);
            }

            var taken = other.Playlist.Remove(song => song.BanKeys.Contains(key), now);

            if (taken.Count > 0) {
                removed += taken.Count;
                RoomMusic.Broadcast(other);
            }
        }

        var struck = offenders.Select(name => Strike(name)).Where(text => text != null).ToList();

        session.SendWhisper($"Banned {stream.Url}. Removed from {removed} place(s).{(struck.Count > 0 ? " " + string.Join(" ", struck) : "")}");
    }

    private string? Strike(string username)
    {
        var client = clients.GetClientByUsername(username);
        var userId = client?.GetHabbo()?.Id ?? moderation.UserId(username);

        if (userId == null) {
            return null;
        }

        var result = MusicPenalty.Apply(moderation, userId.Value, client);

        return $"{username} now has {result.Strikes} strike(s){(result.Restricted ? " and lost :music" : "")}.";
    }

    private void Unban(GameClient session, string[] parameters)
    {
        var stream = parameters.Length > 1 ? RoomStream.Parse(parameters[1], "") : null;

        if (stream == null) {
            session.SendWhisper("Use :musicmod unban <link>.");

            return;
        }

        session.SendWhisper(moderation.Unban(stream) ? "That song can be played again." : "That song was not banned.");
    }

    private void List(GameClient session)
    {
        var bans = moderation.Bans(10);

        session.SendWhisper(bans.Count == 0
            ? "No songs are banned."
            : $"Banned songs, newest first: {string.Join("  ", bans.Select(ban => $"{ban.Url} (by {ban.BannedBy}{(ban.Reason.Length > 0 ? ", " + ban.Reason : "")})"))}");
    }

    private void Strikes(GameClient session, string[] parameters, bool lift)
    {
        if (parameters.Length < 2) {
            session.SendWhisper($"Use :musicmod {(lift ? "lift" : "strikes")} <username>.");

            return;
        }

        var name = parameters[1];
        var userId = clients.GetClientByUsername(name)?.GetHabbo()?.Id ?? moderation.UserId(name);

        if (userId == null) {
            session.SendWhisper($"{name} was not found.");

            return;
        }

        if (!lift) {
            var restricted = moderation.IsRestricted(userId.Value);
            session.SendWhisper($"{name} has {moderation.Strikes(userId.Value)} strike(s) and {(restricted ? "cannot" : "can")} use :music.");

            return;
        }

        if (!moderation.Lift(userId.Value)) {
            session.SendWhisper($"{name} has no strikes.");

            return;
        }

        clients.GetClientByUsername(name)?.SendNotification("A moderator lifted your music strikes. You can use :music again.");
        session.SendWhisper($"Lifted {name}'s strikes. They can use :music again.");
    }
}
