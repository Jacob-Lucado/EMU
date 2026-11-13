using System.Diagnostics.CodeAnalysis;

namespace Plus.HabboHotel.Items.Televisions;

public interface ITelevisionManager
{
    ICollection<TelevisionItem> TelevisionList { get; }
    Dictionary<int, TelevisionItem> Televisions { get; }

    /// <summary>The video chosen for a TV with :tv, by the TV's item id, until the server restarts.</summary>
    System.Collections.Concurrent.ConcurrentDictionary<int, string> NowShowing { get; }
    void Init();
    bool TryGet(int itemId, [NotNullWhen(true)] out TelevisionItem? televisionItem);
}
