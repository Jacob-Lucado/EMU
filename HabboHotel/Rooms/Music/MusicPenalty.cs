using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Music;

internal static class MusicPenalty
{
    // Adds a strike and tells the user, including when it was the one that ends their :music rights.
    public static StrikeResult Apply(IRoomStreamModeration moderation, int userId, GameClient? client)
    {
        var result = moderation.Strike(userId);

        client?.SendNotification(result.Restricted
            ? $"That was strike {result.Strikes} for playing banned music. You can no longer use :music until a moderator lifts it."
            : $"Banned music was played with your :music command. That is strike {result.Strikes} of {RoomStreamModeration.StrikeLimit}. At {RoomStreamModeration.StrikeLimit} strikes you lose :music.");

        return result;
    }
}
