using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms.AI;

/// <summary>Pet nests: the "nests" furniline, or a classname starting with "nest".</summary>
public static class PetNest
{
    public static bool Is(ItemDefinition definition) => definition.FurniLine == "nests" || definition.ItemName.StartsWith("nest", StringComparison.Ordinal);
}
