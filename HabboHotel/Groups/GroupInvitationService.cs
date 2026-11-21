using Plus.HabboHotel.GameClients;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Groups;

[Singleton]
public interface IGroupInvitationService
{
    /// <summary>Invites a user who is not a member: stores the invitation and shows it when the user is online.</summary>
    bool Invite(Group group, int userId, int inviterId, uint roomId);

    /// <summary>Deletes the pending invitation of the user, if there is one.</summary>
    bool Cancel(int groupId, int userId);

    Task Reply(GameClient session, int groupId, int answer);
}

public sealed class GroupInvitationService(IGroupInvitationStore store, IGroupParticipationService participation, IGameClientManager clients,
    TimeProvider time) : IGroupInvitationService
{
    public bool Invite(Group group, int userId, int inviterId, uint roomId) => throw new NotImplementedException();

    public bool Cancel(int groupId, int userId) => throw new NotImplementedException();

    public Task Reply(GameClient session, int groupId, int answer) => throw new NotImplementedException();
}
