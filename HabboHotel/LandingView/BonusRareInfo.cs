using System.Globalization;
using Plus.Core.Settings;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.LandingView;

/// <summary>The landing view's bonus rare: furni class name and sprite id, credits per rare and credits still to buy.</summary>
public sealed record BonusRareInfo(string ProductType, int ProductClassId, int TotalCredits, int CreditsStillRequired)
{
    /// <summary>The client hides the bonus rare for these values.</summary>
    public static BonusRareInfo None { get; } = new("", -1, -1, -1);

    public static BonusRareInfo From(ISettingsManager settings, IItemDataManager items)
    {
        var furni = items.GetItemByName(settings.GetOptionalValue("landing.bonus_rare.item_name") ?? "bonusbag26_3");

        if (furni == null || !int.TryParse(settings.GetOptionalValue("landing.bonus_rare.credits") ?? "120", NumberStyles.None,
                CultureInfo.InvariantCulture, out var credits) || credits <= 0) {
            return None;
        }

        // Credits are bought outside the hotel, so no purchase counts towards the next rare.
        return new(furni.ItemName, furni.SpriteId, credits, credits);
    }
}
