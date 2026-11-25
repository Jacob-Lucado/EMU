using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class StealCommand : ITargetChatCommand
{
    public string Key => "steal";

    public string Parameters => "%target%";

    public string Description => "Try to snatch the hand item of a user next to you.";

    public bool MustBeInSameRoom => true;

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        if (!RoomContact.TryResolve(session, room, target, "steal from", out var self, out var other)) {
            return Task.CompletedTask;
        }

        if (other.CarryItemId <= 0) {
            session.SendWhisper($"{target.Username} is not holding anything.");

            return Task.CompletedTask;
        }

        var success = RoomSocial.StealSucceeds(Random.Shared);

        if (success) {
            var item = other.CarryItemId;
            other.CarryItem(0);
            self.CarryItem(item);
        }

        room.SendChat(receiver => new ChatComposer(self.VirtualId, RoomSocial.Steal(target.Username, success), 0, self.LastBubble, receiver));

        return Task.CompletedTask;
    }
}
