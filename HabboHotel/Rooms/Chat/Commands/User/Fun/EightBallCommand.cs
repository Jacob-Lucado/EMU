using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class EightBallCommand : IChatCommand
{
    public string Key => "8ball";

    public string Parameters => "";

    public string Description => "Shake the magic 8 ball. Think of a question first.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Id);

        if (user == null) {
            return;
        }

        var message = RoomSocial.EightBall(Random.Shared);
        room.SendChat(receiver => new ChatComposer(user.VirtualId, message, 0, user.LastBubble, receiver));
    }
}
