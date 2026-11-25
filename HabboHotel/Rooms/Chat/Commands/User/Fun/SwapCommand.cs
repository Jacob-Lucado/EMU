using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class SwapCommand : ITargetChatCommand
{
    public string Key => "swap";

    public string Parameters => "%target%";

    public string Description => "Swap places with a user standing next to you.";

    public bool MustBeInSameRoom => true;

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        if (!RoomContact.TryResolve(session, room, target, "swap places with", out var self, out var other)) {
            return Task.CompletedTask;
        }

        if (self.IsWalking || other.IsWalking) {
            session.SendWhisper("Wait until you both stand still.");

            return Task.CompletedTask;
        }

        var (selfX, selfY, selfZ) = (self.X, self.Y, self.Z);
        self.SetPos(other.X, other.Y, other.Z);
        other.SetPos(selfX, selfY, selfZ);
        room.SendChat(receiver => new ChatComposer(self.VirtualId, $"*swaps places with {target.Username}*", 0, self.LastBubble, receiver));

        return Task.CompletedTask;
    }
}
