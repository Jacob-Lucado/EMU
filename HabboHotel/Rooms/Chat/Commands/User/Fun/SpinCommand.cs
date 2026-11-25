using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class SpinCommand : IChatCommand
{
    public string Key => "spin";

    public string Parameters => "";

    public string Description => "Turn a quarter turn to the right.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Id);

        if (user == null || user.IsWalking) {
            return;
        }

        user.SetRot(RoomContact.TurnBy(user.RotBody, 2), false);
        user.UpdateNeeded = true;
    }
}
