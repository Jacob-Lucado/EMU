using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Styles;

public static class ChatStyleAccess
{
    public static IReadOnlyList<int> GetAllowedStyleIds(this IChatStyleManager styles, Habbo habbo) => styles.GetAllowedStyleIds(habbo.Access)
        .Concat(habbo.PurchasedChatStyles.Keys.Where(id => styles.TryGetStyle(id, out var style) && style.CanUse(habbo)))
        .Distinct().Order().ToArray();
}
