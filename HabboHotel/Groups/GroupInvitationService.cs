using System.Runtime.CompilerServices;
using Plus.Communication.Packets.Outgoing.Groups;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Wired;
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
    private readonly ConditionalWeakTable<GameClient, Queue<(int GroupId, DateTimeOffset SentAt)>> _recent = new();

    private bool AllowPrompt(GameClient client, int groupId, DateTimeOffset now)
    {
        var prompts = _recent.GetOrCreateValue(client);

        lock (prompts) {
            while (prompts.TryPeek(out var oldest) && oldest.SentAt <= now.AddSeconds(-30)) {
                prompts.Dequeue();
            }

            if (prompts.Count >= 5 || prompts.Any(prompt => prompt.GroupId == groupId)) {
                return false;
            }

            prompts.Enqueue((groupId, now));

            return true;
        }
    }

    public bool Invite(Group group, int userId, int inviterId, uint roomId)
    {
        var client = clients.GetClientByUserId(userId);
        var now = time.GetUtcNow();

        lock (group) {
            if (group.CreatorId == userId || group.IsMember(userId) || client != null && !AllowPrompt(client, group.Id, now)) {
                return false;
            }

            store.Save(new GroupInvitation(group.Id, userId, inviterId, roomId, now));
        }

        client?.Send(new GroupInvitationComposer(group.Id, group.Name, group.Badge, roomId));

        return true;
    }

    public bool Cancel(int groupId, int userId) => store.Delete(groupId, userId);

    public Task Reply(GameClient session, int groupId, int answer)
    {
        var habbo = session.GetHabbo();

        switch ((GroupInvitationAnswer)answer) {
            case GroupInvitationAnswer.Accept:
                return participation.AcceptInvitation(session, groupId);
            case GroupInvitationAnswer.Reject:
                store.Delete(groupId, habbo.Id);
                break;
        }

        return Task.CompletedTask;
    }
}
