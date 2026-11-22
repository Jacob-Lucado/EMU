using System.Collections.Immutable;

namespace Plus.HabboHotel.Groups;

/// <summary>
/// The badge editor names parts by editor id, while badge codes name them by badge code id.
/// </summary>
public sealed class GroupBadgePartIds
{
    private readonly Dictionary<int, int> _baseCodeIds;
    private readonly Dictionary<int, int> _symbolCodeIds;
    private readonly Dictionary<int, int> _baseEditorIds;
    private readonly Dictionary<int, int> _symbolEditorIds;

    public GroupBadgePartIds(IEnumerable<GroupBadgeParts> bases, IEnumerable<GroupBadgeParts> symbols)
    {
        _baseCodeIds = bases.ToDictionary(part => part.EditorId, part => part.Id);
        _symbolCodeIds = symbols.ToDictionary(part => part.EditorId, part => part.Id);
        _baseEditorIds = _baseCodeIds.ToDictionary(ids => ids.Value, ids => ids.Key);
        _symbolEditorIds = _symbolCodeIds.ToDictionary(ids => ids.Value, ids => ids.Key);
    }

    public bool TryGetCodeId(bool isBase, int editorId, out int codeId)
    {
        if (!isBase && editorId == 0) {
            codeId = 0;

            return true;
        }

        return (isBase ? _baseCodeIds : _symbolCodeIds).TryGetValue(editorId, out codeId);
    }

    public int GetEditorId(bool isBase, int codeId) => codeId == 0
        ? 0
        : (isBase ? _baseEditorIds : _symbolEditorIds).GetValueOrDefault(codeId);

    public ImmutableArray<GroupBadgePiece> ToEditorIds(ImmutableArray<GroupBadgePiece> pieces) =>
        [.. pieces.Select((piece, index) => piece with { Symbol = GetEditorId(index == 0, piece.Symbol) })];
}
