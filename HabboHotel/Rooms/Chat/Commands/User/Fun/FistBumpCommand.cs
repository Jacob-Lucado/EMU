using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class FistBumpCommand : ITargetChatCommand
{
    public string Key => "fistbump";

    public string Parameters => "%target%";

    public string Description => "Fist bump another user in the room.";

    public bool MustBeInSameRoom => true;

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        if (target == session.GetHabbo()) {
            session.SendWhisper("Bump someone else, you cannot fist bump yourself.");

            return Task.CompletedTask;
        }

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Id);

        if (user == null) {
            return Task.CompletedTask;
        }

        var message = RoomSocial.FistBump(target.Username);
        room.SendChat(receiver => new ChatComposer(user.VirtualId, message, 0, user.LastBubble, receiver));

        return Task.CompletedTask;
    }
}
