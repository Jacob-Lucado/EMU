using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class YeetCommand : ITargetChatCommand
{
    public string Key => "yeet";

    public string Parameters => "%target%";

    public string Description => "Throw a user next to you three tiles away.";

    public bool MustBeInSameRoom => true;

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        if (!RoomContact.TryResolve(session, room, target, "yeet", out var self, out var other)) {
            return Task.CompletedTask;
        }

        var (stepX, stepY) = RoomContact.Step(self.RotBody);
        var x = other.X;
        var y = other.Y;

        for (var i = 0; i < RoomContact.ThrowDistance; i++) {
            var nextX = x + stepX;
            var nextY = y + stepY;

            var map = room.GetGameMap();

            if (nextX < 0 || nextY < 0 || nextX == map.Model.DoorX && nextY == map.Model.DoorY || !map.SquareIsOpen(nextX, nextY, false) || map.SquareHasUsers(nextX, nextY)) {
                break;
            }

            x = nextX;
            y = nextY;
        }

        if (x == other.X && y == other.Y) {
            session.SendWhisper($"There is no room to throw {target.Username} that way.");

            return Task.CompletedTask;
        }

        other.MoveTo(x, y);
        room.SendChat(receiver => new ChatComposer(self.VirtualId, $"*yeets {target.Username}*", 0, self.LastBubble, receiver));

        return Task.CompletedTask;
    }
}
