using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Groups;

/// <summary>What the group wired boxes may do with groups.</summary>
[Singleton]
public interface IWiredGroupMembership
{
    /// <summary>The error a box save gets for this group, or null. Group 0 is the inactive box.</summary>
    string? SaveRefusal(int editorId, int groupId, bool mustOwn);

    bool Invite(int groupId, RoomUser target, uint roomId);

    bool Remove(int groupId, RoomUser target, bool cancelInvitation);
}

public sealed class WiredGroupMembership(IGroupManager groups, IGroupInvitationService invitations, IGroupRemovalService removal) : IWiredGroupMembership
{
    public string? SaveRefusal(int editorId, int groupId, bool mustOwn)
    {
        if (groupId == 0) {
            return null;
        }

        if (!groups.TryGetGroup(groupId, out var group)) {
            return UnreleasedHabboFeatures.GroupNotFoundError;
        }

        return mustOwn && group.CreatorId != editorId ? UnreleasedHabboFeatures.GroupNotOwnedError : null;
    }

    // The invitation comes from the group's owner, who is the only one that can save the box.
    public bool Invite(int groupId, RoomUser target, uint roomId) =>
        groups.TryGetGroup(groupId, out var group) && invitations.Invite(group, target.HabboId, group.CreatorId, roomId);

    // The member leaves as with the leave button, so the client gets the packets of a normal leave.
    public bool Remove(int groupId, RoomUser target, bool cancelInvitation)
    {
        if (!groups.TryGetGroup(groupId, out var group)) {
            return false;
        }

        var cancelled = cancelInvitation && invitations.Cancel(groupId, target.HabboId);

        if (target.HabboId == group.CreatorId || !group.IsMember(target.HabboId) || target.GetClient() is not { } client) {
            return cancelled;
        }

        removal.Remove(client, groupId, target.HabboId).GetAwaiter().GetResult();

        return !group.IsMember(target.HabboId) || cancelled;
    }
}
