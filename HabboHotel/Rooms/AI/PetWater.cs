using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms.AI;

/// <summary>Water bowls and the horse trough. Their furni state is the water left, from 5 (full) to 0.</summary>
public static class PetWater
{
    public const int Full = 5;

    public static bool Is(ItemDefinition definition) => throw new NotImplementedException();

    /// <summary>One drink at <paramref name="now"/>: the water that nobody drank for a while is full again first.</summary>
    public static void Drink(Item water, DateTimeOffset now) => throw new NotImplementedException();
}
