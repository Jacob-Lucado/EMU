using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class HugCommand : ITargetChatCommand
{
    public string Key => "hug";

    public string Parameters => "%target%";

    public string Description => "Give another user in the room a hug.";

    public bool MustBeInSameRoom => true;

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        if (target == session.GetHabbo()) {
            session.SendWhisper("Hug someone else, you already have yourself.");

            return Task.CompletedTask;
        }

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Id);

        if (user == null) {
            return Task.CompletedTask;
        }

        var message = RoomSocial.Hug(target.Username);
        room.SendChat(receiver => new ChatComposer(user.VirtualId, message, 0, user.LastBubble, receiver));

        return Task.CompletedTask;
    }
}
