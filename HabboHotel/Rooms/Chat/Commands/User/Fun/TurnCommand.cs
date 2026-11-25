using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class TurnCommand : ITargetChatCommand
{
    public string Key => "turn";

    public string Parameters => "%target%";

    public string Description => "Turn a user next to you around to face away from you.";

    public bool MustBeInSameRoom => true;

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        if (!RoomContact.TryResolve(session, room, target, "turn", out var self, out var other)) {
            return Task.CompletedTask;
        }

        if (other.IsWalking) {
            session.SendWhisper($"{target.Username} is walking, wait until they stop.");

            return Task.CompletedTask;
        }

        if (other.X == self.X && other.Y == self.Y) {
            session.SendWhisper("Step away from them first.");

            return Task.CompletedTask;
        }

        other.SetRot(Plus.HabboHotel.Rooms.PathFinding.Rotation.Calculate(self.X, self.Y, other.X, other.Y), false);
        other.UpdateNeeded = true;
        room.SendChat(receiver => new ChatComposer(self.VirtualId, $"*turns {target.Username} around*", 0, self.LastBubble, receiver));

        return Task.CompletedTask;
    }
}
