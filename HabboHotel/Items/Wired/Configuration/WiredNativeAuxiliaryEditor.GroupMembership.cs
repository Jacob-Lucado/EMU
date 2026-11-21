namespace Plus.HabboHotel.Items.Wired.Configuration;

/// <summary>The group boxes. Plus made these editors up from Habbo's previews, so the grammar is ours (see UnreleasedHabboFeatures).</summary>
internal static partial class WiredNativeAuxiliaryEditor
{
    private static IEnumerable<KeyValuePair<string, Spec>> GroupMembership()
    {
        // [group id]; the users to invite, the triggering user by default.
        yield return new(UnreleasedHabboFeatures.InviteToGroupAction, new(Sources([], [TargetUsers], [0]), n =>
            Shape(n, 1, 0, 1, 0) && n.Text.Length == 0 && n.OwnedIntParams[0] >= 0
                ? Roles(Draft(n, n.OwnedIntParams), [], [("users", n.UserSourceTypes[0])]) : null));
        // [group id, cancel a pending invitation 0/1].
        yield return new(UnreleasedHabboFeatures.RemoveFromGroupAction, new(Sources([], [TargetUsers], [0, 1]), n =>
            Shape(n, 2, 0, 1, 0) && n.Text.Length == 0 && n.OwnedIntParams[0] >= 0 && n.OwnedIntParams[1] is 0 or 1
                ? Roles(Draft(n, n.OwnedIntParams), [], [("users", n.UserSourceTypes[0])]) : null));
        // [group mode (0 the room's group, 1 a picked group), group id, change (0 joins, 1 leaves)].
        yield return new(UnreleasedHabboFeatures.GroupMembershipTrigger, new(Meta(0, 0, [0, 0, 0]), n =>
        {
            var p = n.OwnedIntParams;

            return Shape(n, 3, 0, 0, 0) && n.Text.Length == 0 && p[0] is 0 or 1 && p[1] >= 0 && p[2] is 0 or 1 ? Draft(n, p) : null;
        }));
    }
}
