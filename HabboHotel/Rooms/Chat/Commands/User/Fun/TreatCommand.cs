using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class TreatCommand : ITargetChatCommand
{
    public string Key => "treat";

    public string Parameters => "%target%";

    public string Description => "Hand a user next to you a drink or a snack.";

    public bool MustBeInSameRoom => true;

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        if (!RoomContact.TryResolve(session, room, target, "treat", out var self, out var other, needsPush: false)) {
            return Task.CompletedTask;
        }

        var (itemId, message) = RoomSocial.Treat(target.Username, Random.Shared);
        other.CarryItem(itemId);
        room.SendChat(receiver => new ChatComposer(self.VirtualId, message, 0, self.LastBubble, receiver));

        return Task.CompletedTask;
    }
}
