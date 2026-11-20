namespace Plus.HabboHotel.Groups;

public class GroupBadgeParts
{
    public GroupBadgeParts(int id, string assetOne, string assetTwo, int editorId)
    {
        Id = id;
        AssetOne = assetOne;
        AssetTwo = assetTwo;
        EditorId = editorId;
    }

    public int Id { get; }
    public int EditorId { get; }
    public string AssetOne { get; }
    public string AssetTwo { get; }
}
