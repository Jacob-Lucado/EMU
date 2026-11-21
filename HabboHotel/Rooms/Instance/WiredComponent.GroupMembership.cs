using Plus.HabboHotel.Groups;

namespace Plus.HabboHotel.Rooms.Instance;

public partial class WiredComponent
{
    /// <summary>What the group boxes may do with groups; null when the room runs without the group services.</summary>
    internal IWiredGroupMembership? GroupMembership { get; set; }
}
