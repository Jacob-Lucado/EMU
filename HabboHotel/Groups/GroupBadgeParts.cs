namespace Plus.HabboHotel.Groups;

public class GroupBadgeParts
{
    /// <summary>
    /// A part without a stored editor id uses its badge code id plus this offset, far above Habbo's editor ids.
    /// </summary>
    public const int CustomEditorIdOffset = 1000;

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
