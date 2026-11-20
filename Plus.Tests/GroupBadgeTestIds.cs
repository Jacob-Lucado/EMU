using Plus.HabboHotel.Groups;

namespace Plus.Tests;

internal static class GroupBadgeTestIds
{
    internal static GroupBadgePartIds Habbo { get; } = new(
        Parts("base", (1, 1), (5, 5), (21, 65)),
        Parts("symbol", (1, 21), (2, 22), (6, 26), (68, 173), (99, 203), (100, 204), (104, 122), (141, 209), (158, 211)));

    private static GroupBadgeParts[] Parts(string kind, params (int CodeId, int EditorId)[] ids) =>
        [.. ids.Select(id => new GroupBadgeParts(id.CodeId, $"{kind}_{id.CodeId}.gif", string.Empty, id.EditorId))];
}
