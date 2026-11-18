using System.Data;
using System.Globalization;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items;

/// <summary>Creates furni a user receives, with the rows its interaction needs, inside the caller's transaction.</summary>
public static class FurniDelivery
{
    public static List<Item> Create(IItemFactory factory, IDbConnection connection, IDbTransaction transaction, Habbo owner, ItemDefinition definition,
        string extraData, int amount, uint limitedEditionSells, uint limitedEditionStack)
    {
        var items = new List<Item>();

        switch (definition.InteractionType) {
            case InteractionType.GuildGate:
            case InteractionType.GuildItem:
            case InteractionType.GuildForum:
                var groupId = int.TryParse(extraData, NumberStyles.Integer, CultureInfo.InvariantCulture, out var group) ? group : 0;

                if (amount > 1) {
                    items.AddRange(factory.CreateMultipleItems(connection, transaction, definition, owner.Id, extraData, amount, groupId));
                }
                else if (factory.CreateSingleItemNullable(connection, transaction, definition, owner, extraData, groupId) is { } guildItem) {
                    items.Add(guildItem);
                }

                break;
            case InteractionType.Arrow:
            case InteractionType.Teleport:
            case InteractionType.WiredRoomLinker:
                for (var i = 0; i < amount; i++) {
                    items.AddRange(factory.CreateTeleporterItems(connection, transaction, definition, owner));
                }

                break;
            default:
                if (amount > 1) {
                    items.AddRange(factory.CreateMultipleItems(connection, transaction, definition, owner.Id, extraData, amount));
                }
                else if (factory.CreateSingleItemNullable(connection, transaction, definition, owner, extraData, 0, limitedEditionSells, limitedEditionStack) is { } item) {
                    items.Add(item);
                }

                break;
        }

        foreach (var item in items) {
            if (definition.InteractionType == InteractionType.Moodlight) {
                factory.CreateMoodlightData(connection, transaction, item);
            }
            else if (definition.InteractionType == InteractionType.Toner) {
                factory.CreateTonerData(connection, transaction, item);
            }
        }

        return items;
    }
}
