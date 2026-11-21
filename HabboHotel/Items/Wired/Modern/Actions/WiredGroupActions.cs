using Plus.HabboHotel.Items.Wired.Configuration;
using Plus.HabboHotel.Items.Wired.Runtime;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired.Modern.Actions;

/// <summary>
/// The group boxes Habbo has shown but not shipped: Invite to Group, Remove from Group and the Group Membership trigger.
/// Runtime int layouts are the ones WiredNativeAuxiliaryEditor.GroupMembership compiles.
/// </summary>
public static class WiredGroupActions
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        UnreleasedHabboFeatures.InviteToGroupAction, UnreleasedHabboFeatures.RemoveFromGroupAction
    };

    private static int P(WiredConfiguration config, int index) => config.IntParams.Length > index ? config.IntParams[index] : 0;

    // [group id]; [group id, cancel invitation]. Group 0 is the inactive box.
    public static bool Execute(string name, WiredRuntimeContext context, WiredConfiguration config, RoomUser[] users)
    {
        var groupId = P(config, 0);

        if (groupId <= 0 || context.Room.GetWired().GroupMembership is not { } groups) {
            return false;
        }

        var changed = false;

        foreach (var user in users.Where(user => !user.IsBot)) {
            changed |= name.Equals(UnreleasedHabboFeatures.InviteToGroupAction, StringComparison.OrdinalIgnoreCase)
                ? groups.Invite(groupId, user, context.Room.Id)
                : groups.Remove(groupId, user, P(config, 1) == 1);
        }

        return changed;
    }

    /// <summary>[group mode (0 the room's group, 1 the picked group), group id, change (0 joins, 1 leaves)].</summary>
    public static bool MatchesMembership(WiredRuntimeContext context, WiredConfiguration config)
    {
        var groupId = P(config, 0) == UnreleasedHabboFeatures.RoomGroup ? context.Room.Group?.Id ?? 0 : P(config, 1);

        return groupId > 0 && context.Event.Code == groupId && context.Event.Action == P(config, 2);
    }
}
