using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class CoinCommand : IChatCommand
{
    public string Key => "coin";

    public string Parameters => "";

    public string Description => "Flip a coin for everyone in the room.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Username);

        if (user == null) {
            return;
        }

        var message = RoomDice.Flip(Random.Shared);
        room.SendChat(receiver => new ChatComposer(user.VirtualId, message, 0, user.LastBubble, receiver));
    }
}
