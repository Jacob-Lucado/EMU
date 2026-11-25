using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class TurnAroundCommand : IChatCommand
{
    public string Key => "turnaround";

    public string Parameters => "";

    public string Description => "Turn around to face the other way.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Id);

        if (user == null || user.IsWalking) {
            return;
        }

        user.SetRot(RoomContact.TurnBy(user.RotBody, 4), false);
        user.UpdateNeeded = true;
    }
}
