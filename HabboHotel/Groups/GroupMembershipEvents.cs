using Plus.HabboHotel.GameClients;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Groups;

[Singleton]
public interface IGroupMembershipEvents
{
    /// <summary>A user joined or left a group by any path; the room the user is in runs its membership triggers.</summary>
    void Changed(int groupId, int userId, bool joined);
}

public sealed class GroupMembershipEvents(IGameClientManager clients) : IGroupMembershipEvents
{
    public void Changed(int groupId, int userId, bool joined)
    {
        if (clients.GetClientByUserId(userId)?.GetHabbo() is not { CurrentRoom: { } room }
            || room.GetRoomUserManager()?.GetRoomUserByHabbo(userId) is not { } user) {
            return;
        }

        room.GetWired()?.GroupMembershipChanged(user, groupId, joined);
    }
}
