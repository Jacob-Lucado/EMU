using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Permissions;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

public static class RoomContact
{
    public const int ThrowDistance = 3;

    private static readonly (int X, int Y)[] Steps = [(0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)];

    public static (int X, int Y) Step(int rotation) => Steps[((rotation % 8) + 8) % 8];

    public static int TurnBy(int rotation, int steps) => ((rotation + steps) % 8 + 8) % 8;

    public static bool IsAdjacent(int x1, int y1, int x2, int y2) => Math.Abs(x1 - x2) <= 1 && Math.Abs(y1 - y2) <= 1;

    internal static bool TryResolve(GameClient session, Room room, Habbo target, string verb, out RoomUser self, out RoomUser other)
    {
        self = null!;
        other = null!;

        if (!room.PushEnabled && !session.GetHabbo().Access.Can(PermissionKeys.RoomOverrideCustomConfig)) {
            session.SendWhisper("The room owner has turned off pushing, so you cannot do that here.");

            return false;
        }

        if (target == session.GetHabbo()) {
            session.SendWhisper($"You cannot {verb} yourself.");

            return false;
        }

        var targetUser = room.GetRoomUserManager().GetRoomUserByHabbo(target.Id);
        var thisUser = room.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Id);

        if (targetUser == null || thisUser == null) {
            session.SendWhisper("That user is not in this room.");

            return false;
        }

        if (targetUser.TeleportEnabled) {
            session.SendWhisper("You cannot do that to a user who has teleport mode on.");

            return false;
        }

        if (!IsAdjacent(thisUser.X, thisUser.Y, targetUser.X, targetUser.Y)) {
            session.SendWhisper($"{target.Username} is not close enough.");

            return false;
        }

        self = thisUser;
        other = targetUser;

        return true;
    }
}
