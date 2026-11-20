namespace Plus.HabboHotel.Groups;

public sealed class GroupBadgePartIds
{
    public GroupBadgePartIds(IEnumerable<GroupBadgeParts> bases, IEnumerable<GroupBadgeParts> symbols)
    {
    }

    public bool TryGetCodeId(bool isBase, int editorId, out int codeId) => throw new NotImplementedException();

    public int GetEditorId(bool isBase, int codeId) => throw new NotImplementedException();
}
