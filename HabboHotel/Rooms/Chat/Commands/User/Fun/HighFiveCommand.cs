using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class HighFiveCommand : ITargetChatCommand
{
    public string Key => "highfive";

    public string Parameters => "%target%";

    public string Description => "Give another user in the room a high five.";

    public bool MustBeInSameRoom => true;

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        if (target == session.GetHabbo()) {
            session.SendWhisper("High five someone else, you cannot clap with yourself.");

            return Task.CompletedTask;
        }

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Id);

        if (user == null) {
            return Task.CompletedTask;
        }

        var message = RoomSocial.HighFive(target.Username);
        room.SendChat(receiver => new ChatComposer(user.VirtualId, message, 0, user.LastBubble, receiver));

        return Task.CompletedTask;
    }
}
