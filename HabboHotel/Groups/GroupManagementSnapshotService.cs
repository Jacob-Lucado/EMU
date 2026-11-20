using System.Collections.Immutable;
using Plus.Communication.Packets.Outgoing.Groups;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Permissions;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Groups;

public readonly record struct GroupBadgePiece(int Symbol, int Colour, int Position);
public sealed record GroupManagementSnapshot(
    bool HasRoom, uint RoomId, string RoomName, int GroupId, string Name, string Description,
    int Colour1, int Colour2, int Type, int AdminOnlyDeco, ImmutableArray<GroupBadgePiece> BadgePieces,
    string Badge, int MemberCount, bool ForumEnabled);

public interface IGroupManagementSnapshotService
{
    void Send(GameClient session, int groupId);
}

public sealed class GroupManagementSnapshotService(IGroupManager groups, IRoomDataLoader rooms) : IGroupManagementSnapshotService
{
    public void Send(GameClient session, int groupId)
    {
        if (!groups.TryGetGroup(groupId, out var group)) {
            return;
        }

        var habbo = session.GetHabbo();

        if (group.CreatorId != habbo.Id && !habbo.Access.Can(PermissionKeys.GroupManagementOverride)) {
            return;
        }

        if (!TryParseBadge(group.Badge, out var pieces)) {
            return;
        }

        var roomName = group.RoomId != 0 && rooms.TryGetData(group.RoomId, out var roomData) ? roomData.Name : string.Empty;
        var snapshot = new GroupManagementSnapshot(
            group.RoomId != 0, group.RoomId, roomName, group.Id, group.Name, group.Description,
            group.Colour1, group.Colour2, group.Type switch { GroupType.Open => 0, GroupType.Locked => 1, _ => 2 },
            group.AdminOnlyDeco, groups.BadgePartIds.ToEditorIds(pieces), group.Badge, group.MemberCount, group.ForumEnabled);
        session.Send(new ManageGroupComposer(snapshot));
    }

    internal static bool TryParseBadge(string? badge, out ImmutableArray<GroupBadgePiece> pieces)
    {
        var parsed = new List<GroupBadgePiece>(5);
        var remaining = badge.AsSpan();
        pieces = [];

        while (!remaining.IsEmpty) {
            var prefix = remaining[0];

            if (prefix is 'b' or 's' or 't') {
                remaining = remaining[1..];
            }

            var next = remaining.IndexOfAny('s', 't');
            var symbol = next < 0 ? remaining : remaining[..next];
            remaining = next < 0 ? [] : remaining[next..];

            if (parsed.Count == 5 || symbol.Length is < 4 or > 6 || symbol.ContainsAnyExceptInRange('0', '9')) {
                return false;
            }

            var symbolLength = symbol.Length >= 6 ? 3 : 2;
            var position = 0;

            if (!int.TryParse(symbol[..symbolLength], out var symbolId) ||
                !int.TryParse(symbol.Slice(symbolLength, 2), out var colour) ||
                (symbol.Length >= 5 && !int.TryParse(symbol[^1..], out position))) {
                return false;
            }

            parsed.Add(new(prefix == 't' ? symbolId + 100 : symbolId, colour, position));
        }

        while (parsed.Count < 5) {
            parsed.Add(default);
        }

        pieces = [.. parsed];

        return true;
    }
}
