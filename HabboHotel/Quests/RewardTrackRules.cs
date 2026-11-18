using System.Globalization;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Quests;

public static class RewardTrackRules
{
    /// <summary>The bot AI types a bot preset can have; a bot prize names one in upper case, such as GENERIC.</summary>
    private static readonly string[] BotAiTypes = ["generic", "bartender"];

    public static RewardTrackStep Plan(RewardTrack track, UserRewardTrackState state, RewardTrackTask task, int amount)
    {
        var before = state.ProgressOf(task.Id);

        if (amount < 1 || (task.Premium && !state.Premium) || task.IsComplete(before)) {
            return new RewardTrackStep(false, task.Id, before, state.PeakOf(task.Id), 0, state.Points);
        }

        var after = (int)Math.Min(task.Cap, Math.Min(int.MaxValue, (long)before + amount));

        if (after == before) {
            return new RewardTrackStep(false, task.Id, before, state.PeakOf(task.Id), 0, state.Points);
        }

        var previousPeak = state.PeakOf(task.Id);
        var granted = after > previousPeak ? track.PointsFor(task, previousPeak, after, state.Premium) : 0;
        var peak = Math.Max(previousPeak, after);

        return new RewardTrackStep(true, task.Id, after, peak, granted, state.Points + granted);
    }

    public static RewardTrackStep Advance(RewardTrack track, UserRewardTrackState state, RewardTrackTask task, int amount)
    {
        var step = Plan(track, state, task, amount);

        if (step.Changed) {
            state.Apply(step);
        }

        return step;
    }

    public static RewardTrackResults PreviewClaim(RewardTrack? track, UserRewardTrackState? state, string? prizeId)
    {
        if (track == null || state == null || string.IsNullOrEmpty(prizeId)) {
            return RewardTrackResults.Unknown;
        }

        var prize = track.GetPrize(prizeId);

        if (prize == null) {
            return RewardTrackResults.Unknown;
        }

        if (state.IsClaimed(prizeId)) {
            return RewardTrackResults.AlreadyClaimed;
        }

        if (prize.Premium && !state.Premium) {
            return RewardTrackResults.PremiumRequired;
        }

        if (state.Points < prize.RequiredPoints) {
            return RewardTrackResults.NotEnoughPoints;
        }

        if (Grant(prize) == null) {
            return RewardTrackResults.Unknown;
        }

        return RewardTrackResults.Ok;
    }

    /// <summary>What the prize gives, read from Habbo's prize fields; null when the server cannot grant it.</summary>
    public static RewardTrackGrant? Grant(RewardTrackPrize prize)
    {
        if (prize.RewardAmount < 1) {
            return null;
        }

        var code = prize.RewardType.Trim();
        var id = int.TryParse(code, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number) ? number : (int?)null;
        var figure = prize.ExtraParams.Trim();

        return prize.ProductItemTypeId switch
        {
            ProductItemType.WallItem when id > 0 => new RewardTrackFurniGrant(ItemType.Wall, id.Value, prize.RewardAmount),
            ProductItemType.FloorItem when id > 0 => new RewardTrackFurniGrant(ItemType.Floor, id.Value, prize.RewardAmount),
            ProductItemType.Badge when code.Length > 0 => new RewardTrackBadgeGrant(code),
            ProductItemType.Currency when id is { } type && (type == ProductItemType.Credits || ActivityPointType.IsValid(type)) =>
                new RewardTrackCurrencyGrant(type, prize.RewardAmount),
            ProductItemType.Pet when id >= 0 && figure.Split(' ')[0] == code => new RewardTrackPetGrant(id.Value, figure, prize.RewardAmount),
            ProductItemType.Bot when BotAiTypes.Contains(code.ToLowerInvariant()) && figure.Length > 0 =>
                new RewardTrackBotGrant(code.ToLowerInvariant(), figure, prize.RewardAmount),
            ProductItemType.Habbicon when id > 0 => new RewardTrackHabbiconGrant(id.Value),
            _ => null
        };
    }

    public static PremiumQuote Quote(RewardTrack? track, UserRewardTrackState? state, int credits, int diamonds)
    {
        if (track == null || state == null || !track.HasPremium) {
            return new PremiumQuote(RewardTrackResults.Unknown, credits, diamonds, state?.Points ?? 0);
        }

        if (state.Premium) {
            return new PremiumQuote(RewardTrackResults.AlreadyPremium, credits, diamonds, state.Points);
        }

        var costCredits = Math.Max(0, track.PremiumCostCredits);
        var costDiamonds = Math.Max(0, track.PremiumCostDiamonds);

        if (credits < costCredits || diamonds < costDiamonds) {
            return new PremiumQuote(RewardTrackResults.NotEnoughCurrency, credits, diamonds, state.Points);
        }

        return new PremiumQuote(RewardTrackResults.Ok, credits - costCredits, diamonds - costDiamonds, state.Points + Math.Max(0, track.PremiumInstantPoints));
    }
}
