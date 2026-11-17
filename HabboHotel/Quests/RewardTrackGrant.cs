using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Quests;

/// <summary>
/// Habbo's product type ids, as the client's product icon widget reads them. A prize's reward type holds the id or code
/// of the product: the furni sprite id, the badge code, the currency type or the habbicon id.
/// </summary>
public static class ProductItemType
{
    public const short WallItem = 0;
    public const short FloorItem = 1;
    public const short Badge = 4;
    public const short Currency = 8;
    public const short Habbicon = 12;

    /// <summary>The currency type of credits. Every other currency type is an activity point type.</summary>
    public const int Credits = -1;
}

/// <summary>What claiming a prize gives.</summary>
public abstract record RewardTrackGrant;

/// <summary><paramref name="Type"/> is <see cref="ProductItemType.Credits"/> or an activity point type.</summary>
public sealed record RewardTrackCurrencyGrant(int Type, int Amount) : RewardTrackGrant;

public sealed record RewardTrackBadgeGrant(string Code) : RewardTrackGrant;

public sealed record RewardTrackFurniGrant(ItemType Type, int SpriteId, int Amount) : RewardTrackGrant;

public sealed record RewardTrackHabbiconGrant(int Id) : RewardTrackGrant;
