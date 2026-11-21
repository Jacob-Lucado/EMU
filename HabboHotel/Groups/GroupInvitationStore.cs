using Plus.Database;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Groups;

public sealed record GroupInvitation(int GroupId, int UserId, int InviterId, uint RoomId, DateTimeOffset CreatedAt);

[Singleton]
public interface IGroupInvitationStore
{
    /// <summary>Stores the invitation, or refreshes the inviter, room and time of the pending one.</summary>
    void Save(GroupInvitation invitation);
    GroupInvitation? Find(int groupId, int userId);
    bool Delete(int groupId, int userId);

    /// <summary>Turns a pending invitation into a membership in one transaction; without one nothing changes.</summary>
    GroupJoinOutcome Accept(int userId, int groupId, int membershipLimit);
}

public sealed class GroupInvitationStore(IDatabase database) : IGroupInvitationStore
{
    public void Save(GroupInvitation invitation) => throw new NotImplementedException();

    public GroupInvitation? Find(int groupId, int userId) => throw new NotImplementedException();

    public bool Delete(int groupId, int userId) => throw new NotImplementedException();

    public GroupJoinOutcome Accept(int userId, int groupId, int membershipLimit) => throw new NotImplementedException();
}
