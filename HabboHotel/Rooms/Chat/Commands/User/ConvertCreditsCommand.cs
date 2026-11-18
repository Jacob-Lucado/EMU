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
            InventoryItem[] deletedItems;

            // The exchanged items are read from and removed out of the loaded inventory, so nothing is converted without one.
            if (habbo.Inventory is not { } inventory) {
                return;
            }

            lock (habbo.WalletSync) {
                var items = inventory.Furniture.AllItems.ToArray();

                if (items.Length == 0) {
                    session.SendWhisper("You currently have no items in your inventory!");

                    return;
                }

                var exchangeItems = items.Where(item => item.Definition.RedeemableForCredits).ToDictionary(item => item.Id);
                var redeemed = _store.RedeemInventoryExchanges(habbo.Id,
                    exchangeItems.Values.Select(item => new ExchangeRedemption(item.Id, Math.Max(item.Definition.BehaviourData, 0))).ToArray());
                deletedItems = redeemed.Select(id => exchangeItems[id]).ToArray();

                foreach (var item in deletedItems) {
                    var value = item.Definition.BehaviourData;
                    totalValue += value;

                    if (value > 0) {
                        habbo.Credits += value;
                    }
                }
            }

            foreach (var item in deletedItems) {
                inventory.Furniture.RemoveItem(item.Id);
                session.Send(new FurniListRemoveComposer(item.Id));
            }

            if (totalValue > 0) {
                session.Send(new CreditBalanceComposer(habbo.Credits));
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
