using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class RollCommand : IChatCommand
{
    public string Key => "roll";

    public string Parameters => "%sides%";

    public string Description => "Roll a dice for everyone in the room. Add a number for other dice, like :roll 20, or roll several at once, like :roll 3d6.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        if (!RoomDice.TryParse(parameters.Length > 0 ? parameters[0] : null, out var count, out var sides)) {
            session.SendWhisper($"Pick a dice between 2 and {RoomDice.MaxSides} sides, like :roll 20, or up to {RoomDice.MaxCount} dice like :roll 2d6.");

            return;
        }

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Username);

        if (user == null) {
            return;
        }

        var message = RoomDice.Roll(count, sides, Random.Shared);
        room.SendChat(receiver => new ChatComposer(user.VirtualId, message, 0, user.LastBubble, receiver));
    }
}
