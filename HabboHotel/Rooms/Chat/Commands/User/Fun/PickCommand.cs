using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class PickCommand : IChatCommand
{
    public string Key => "pick";

    public string Parameters => "%a% | %b% | ...";

    public string Description => "Let the room pick one of several options, split with |.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Username);

        if (user == null) {
            return;
        }

        var message = RoomSocial.Pick(string.Join(' ', parameters), Random.Shared);

        if (message == null) {
            session.SendWhisper("Give me at least two options split with |, like :pick pizza | tacos.");

            return;
        }

        room.SendChat(receiver => new ChatComposer(user.VirtualId, message, 0, user.LastBubble, receiver));
    }
}
