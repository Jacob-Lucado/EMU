using System.Collections.Immutable;
using System.Globalization;
using Plus.Communication.Packets.Outgoing.LandingView;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.LandingView.Promotions;

namespace Plus.HabboHotel.LandingView;

public sealed record LandingPromotionSnapshot(
    int Id, string? Title, string? Text, string? ButtonText, int ButtonType, string? ButtonLink, string? ImageLink)
{
    public static LandingPromotionSnapshot Capture(Promotion promotion) => new(promotion.Id, promotion.Title,
        promotion.Text, promotion.ButtonText, promotion.ButtonType, promotion.ButtonLink, promotion.ImageLink);
}

public interface ILandingViewPresentationService
{
    void ShowArticles(GameClient session);
    void RefreshCampaign(GameClient session, string campaigns);
}

public sealed class LandingViewPresentationService(ILandingViewManager landingView, TimeProvider time) : ILandingViewPresentationService
{
    public void RefreshCampaign(GameClient session, string campaigns)
    {
        if (campaigns.Contains("gamesmaker")) {
            return;
        }

        var now = TimeZoneInfo.ConvertTime(time.GetUtcNow(), time.LocalTimeZone).DateTime;
        var name = "";
        var latestStart = DateTime.MinValue;

        // Entries are "yyyy-MM-dd HH:mm,code" in hotel time. The code with the latest start that has passed is current.
        foreach (var value in campaigns.Split(';')) {
            if (string.IsNullOrEmpty(value) || value.EndsWith(",")) {
                continue;
            }

            var fields = value.Split(',');

            if (fields.Length < 2) {
                return;
            }

            if (DateTime.TryParseExact(fields[0], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
                && start <= now && start >= latestStart) {
                latestStart = start;
                name = fields[1];
            }
        }

        try {
            session.Send(new CampaignComposer(campaigns, name));
        }
        catch { /* Campaign refresh has always ignored publication failures. */ }
    }

    public void ShowArticles(GameClient session) =>
        session.Send(new PromoArticlesComposer(landingView.GetPromotionItems().Select(LandingPromotionSnapshot.Capture).ToImmutableArray()));
}
