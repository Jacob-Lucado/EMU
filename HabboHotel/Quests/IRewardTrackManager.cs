using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Quests;

public interface IRewardTrackManager
{
    void Progress(GameClient session, string actionType, int amount = 1);

    /// <summary>
    /// Progress with a detail of the action, such as the hand item id. Reward tracks ignore the detail; daily tasks can be
    /// narrowed to it.
    /// </summary>
    void Progress(GameClient session, string actionType, int amount, string? parameter) => Progress(session, actionType, amount);

    /// <summary>The Wired progress reward track box: moves the named custom Wired task by the score, or up to it.</summary>
    void ProgressWiredTask(GameClient session, string trackId, string taskId, int score, bool addToExisting) { }

    void SendTracks(GameClient session);
    Task Claim(GameClient session, string trackId, string prizeId);
    void PurchasePremium(GameClient session, string trackId);
}
