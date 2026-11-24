using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class AfkCommand(IRoomAvatarActionService actions) : IChatCommand
{
    public string Key => "afk";

    public string Parameters => "";

    public string Description => "Show that you are away from the keyboard. Moving or chatting wakes you up.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        actions.PerformAction(room, session, 5);
    }
}
