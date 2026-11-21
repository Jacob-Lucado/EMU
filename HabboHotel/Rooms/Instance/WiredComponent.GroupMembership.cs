using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Items.Wired.Runtime;

namespace Plus.HabboHotel.Rooms.Instance;

public partial class WiredComponent
{
    /// <summary>What the group boxes may do with groups; null when the room runs without the group services.</summary>
    internal IWiredGroupMembership? GroupMembership { get; set; }

    /// <summary>
    /// A user of this room joined or left a group. The callers hold group and account locks that a group box also takes,
    /// so the stacks run on the next Wired pass instead of now.
    /// </summary>
    public void GroupMembershipChanged(RoomUser user, int groupId, bool joined) =>
        _linkArrivals.Enqueue(new(WiredEventKind.GroupMembership) { Actor = user, Code = groupId, Action = joined ? UnreleasedHabboFeatures.JoinedGroup : UnreleasedHabboFeatures.LeftGroup });
}
