using Plus.HabboHotel.Permissions;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms;

/// <summary>Who the room's idle autokick never removes.</summary>
internal static class IdleKickExemption
{
    private const uint ExemptRoomId = 1649919;

    public static bool Applies(Room room, Habbo habbo) =>
        habbo.Access.Can(PermissionKeys.ModerationTool) || room.OwnerId == habbo.Id || room.Id == ExemptRoomId;
}
