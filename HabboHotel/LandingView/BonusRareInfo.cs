using Plus.Core.Settings;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.LandingView;

/// <summary>The landing view's bonus rare: furni class name and sprite id, credits per rare and credits still to buy.</summary>
public sealed record BonusRareInfo(string ProductType, int ProductClassId, int TotalCredits, int CreditsStillRequired)
{
    public static BonusRareInfo From(ISettingsManager settings, IItemDataManager items) => throw new NotImplementedException();
}
