using System.Collections.Frozen;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;

namespace Plus.HabboHotel.Rooms.PathFinding;

// Immutable geometry. Footprints are copied; no compiler reads a live Item.
public sealed record NavItemRecord(uint ItemId, long Version, double Z, double Height,
    bool Walkable, bool Seat, InteractionType Interaction, string State, int GroupId,
    int Rotation, bool Removed, IReadOnlyList<int> Footprint,
    int X = 0, int Y = 0, int Length = 1, int Width = 1, WiredBoxType WiredType = WiredBoxType.None)
{
    private static readonly FrozenSet<string> HorseJumps = new[] { "obstacle_oxer", "obstacle_oxer2", "obstacle_oxer3" }.ToFrozenSet();

    public double Top => Z + Height;
    public IReadOnlyList<int> BlockedTiles { get; init; } = [];
    public bool WalkableAt(int tile) => Walkable && !BlockedTiles.Contains(tile);

    // The bar runs across the jump's length, so a rider jumps it moving along that length.
    public NavFlags JumpAt(int tile) => !Walkable || !BlockedTiles.Contains(tile) ? NavFlags.None
        : (Rotation % 2 == 1 ? (Rotation + 1) % 8 : Rotation) is 2 or 6 ? NavFlags.JumpAlongX : NavFlags.JumpAlongY;
    internal static bool StateRelevant(ItemDefinition definition) => definition.InteractionType is InteractionType.Gate or InteractionType.GuildGate
        || definition.AdjustableHeights is { Count: > 1 };

    internal static NavItemRecord Capture(Item item, long version, int width, int height, NavItemRecord? previous = null)
    {
        var definition = item.Definition;
        var currentHeight = item.CurrentHeight;

        // Only gate state changes standability; adjustable state is represented by Height.
        var state = definition.InteractionType is InteractionType.Gate or InteractionType.GuildGate ? item.LegacyDataString : "";
        var group = definition.InteractionType == InteractionType.GuildGate ? item.GroupId : 0;
        // Nothing can stand on the bar of a horse jump, the middle row across its length.
        IReadOnlyList<int> blocked = HorseJumps.Contains(definition.ItemName)
            ? [.. Tiles(definition.Length / 2 + 1).Except(Tiles(definition.Length / 2))] : [];
        var sameFootprint = previous != null && previous.X == item.GetX && previous.Y == item.GetY
            && previous.Rotation == item.Rotation && previous.Length == definition.Length && previous.Width == definition.Width;

        if (sameFootprint && !previous!.Removed && previous.Z == item.GetZ && previous.Height == currentHeight
            && previous.Walkable == definition.Walkable && previous.Seat == definition.IsSeat
            && previous.Interaction == definition.InteractionType && previous.State == state && previous.GroupId == group
            && previous.WiredType == definition.WiredType && previous.BlockedTiles.SequenceEqual(blocked)) {
            return previous;
        }

        var footprint = sameFootprint ? previous!.Footprint : Tiles(definition.Length);

        return new(item.Id, version, item.GetZ, currentHeight,
            definition.Walkable, definition.IsSeat, definition.InteractionType,
            state, group, item.Rotation, false, footprint, item.GetX, item.GetY, definition.Length, definition.Width, definition.WiredType)
        {
            BlockedTiles = blocked
        };

        // The footprint tiles inside the room, with the item cut to the given length.
        IReadOnlyList<int> Tiles(int length) => Array.AsReadOnly(Gamemap.GetAffectedTiles(length, definition.Width,
            item.GetX, item.GetY, item.Rotation).Values
            .Select(p => (p.X, p.Y)).Append((item.GetX, item.GetY))
            .Where(p => p.Item1 >= 0 && p.Item1 < width && p.Item2 >= 0 && p.Item2 < height)
            .Select(p => p.Item2 * width + p.Item1).Distinct().Order().ToArray());
    }
}
