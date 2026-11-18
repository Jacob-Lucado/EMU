using Plus.HabboHotel.Catalog.Marketplace;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class ConvertCreditsCommand : IChatCommand
{
    private readonly IItemRedemptionStore _store;
    public string Key => "convertcredits";

    public string Parameters => "";

    public string Description => "Convert your exchangeable furniture into actual credits.";

    public ConvertCreditsCommand(IItemRedemptionStore store)
    {
        _store = store;
    }

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var totalValue = 0;

        try {
            var habbo = session.GetHabbo();

            // The exchanged items are read from and removed out of the loaded inventory, so nothing is converted without one.
            if (habbo.Inventory is not { } inventory) {
                return;
            }

            lock (habbo.InventoryMutationSync)
                lock (habbo.WalletSync) {
                    if (habbo.WalletClosed || habbo.Credits == int.MaxValue) {
                        return;
                    }

                    var items = inventory.Furniture.AllItems.ToArray();

                    if (items.Length == 0) {
                        session.SendWhisper("You currently have no items in your inventory!");

                        return;
                    }

                    var reserved = new List<InventoryItem>();

                    try {
                        foreach (var item in items) {
                            if (item.Definition.RedeemableForCredits && item.Definition.BehaviourData > 0 && item.TryReserve()) {
                                reserved.Add(item);
                            }
                        }

                        var candidates = reserved.ToDictionary(item => item.Id, item => new ExchangeRedemption(item.Id, item.Definition.BehaviourData));
                        var redeemed = candidates.Count == 0 ? [] : MarketplaceCommit.Guard(habbo, () =>
                            _store.RedeemInventoryExchanges(habbo.Id, habbo.Credits, candidates.Values.ToArray()));
                        totalValue = redeemed.Sum(id => candidates[id].Credits);
                        habbo.Credits = checked(habbo.Credits + totalValue);

                        foreach (var id in redeemed) {
                            inventory.Furniture.RemoveItem(id);
                        }

                        foreach (var id in redeemed) {
                            session.Send(new FurniListRemoveComposer(id));
                        }

                        if (totalValue > 0) {
                            session.Send(new CreditBalanceComposer(habbo.Credits));
                        }
                    }
                    finally {
                        foreach (var item in reserved) {
                            item.ReleaseReservation();
                        }
                    }
                }

            if (totalValue > 0) {
                session.SendNotification($"All credits have successfully been converted!\r\r(Total value: {totalValue} credits!");
            }
            else {
                session.SendNotification("It appears you don't have any exchangeable items!");
            }
        }
        catch {
            session.SendNotification("Oops, an error occoured whilst converting your credits!");
        }
    }
}
