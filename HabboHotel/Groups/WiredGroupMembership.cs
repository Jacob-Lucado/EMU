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
    public string? SaveRefusal(int editorId, int groupId, bool mustOwn) => throw new NotImplementedException();

    public bool Invite(int groupId, RoomUser target, uint roomId) => throw new NotImplementedException();

    public bool Remove(int groupId, RoomUser target, bool cancelInvitation) => throw new NotImplementedException();
}
