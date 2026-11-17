# Furni-Matic

Furni-Matic uses the native recycler window and Ecotron boxes. Its configuration is stored in `recycler_settings`, `recycler_levels` and `recycler_prizes`. Migration 150 gives a hotel that never set it up Habbo's machine: 8 slots, 10 Duckets, 10 seconds between exchanges and Habbo's prize table. A hotel with its own setup, or without every prize furni, is left unchanged.

## Configuration

- `slots`: 1 to 12 eligible furniture instances per exchange.
- `cooldown_seconds`: 0 to 86,400 seconds between exchanges by the same user.
- `cost_duckets`: 0 to 1,000,000 Duckets per exchange. Existing installations keep a cost of zero.
- Levels: up to six distinct levels, numbered 1 to 6. Level 1 must have `chance=1` and receives the remaining probability. Each other level has an absolute probability of `1/chance`. Their sum must be less than one.
- Each level needs at least one prize. Prizes within a level have equal probability. A prize has either an `item_id` or a `chat_style_id`, never both.

For six levels with chances `1, 4, 10, 40, 400, 5000`, the probabilities are 62.23%, 25%, 10%, 2.5%, 0.25% and 0.02%. Invalid or overflowing distributions close the recycler instead of dropping prizes.

Set the client's `recycler.number_of_slots` and `recycler.ducket_cost` properties to the same slot count and cost, and `recycler.timeout_seconds` to `cooldown_seconds`. These values control the display; the server always validates its own configuration and balance. Client revisions without purchased chat-style messages cannot recycle a distribution containing chat styles.

A paid exchange is checked against the session's Duckets. Before a paid exchange, the server saves the session's credits and currencies.

Chat-style prizes must be enabled, require ownership, and have no HC or permission requirement. They need a client that handles the purchased chat-style messages 470 and 916 and lists owned styles in its chat-style selector. Do not add chat-style prizes while players use a client without this support. Acquired styles belong to the user who opens the box, and the allowed chat-style list (9340) then includes them. Opening another box with the same style keeps one permanent right, records another opening and awards no substitute currency or furniture.

## Transactions and recovery

An exchange consumes eligible owned inventory items, charges Duckets, chooses the box contents, updates the cooldown and records progress in one transaction. Limited items, borrowed Builders Club items, active music objects and unopened containers cannot be recycled. The reward is fixed when the box is created.

An opening consumes the placed box and grants its contents in one transaction. A box can be opened once, including after transfer to another owner. Old boxes without receipts remain usable, but do not receive an invented rarity level.

`recycler_operations`, `recycler_openings` and `recycler_revisions` retain immutable receipts. Database triggers block every update and delete on them, so receipts are never pruned. Migration 135 creates these triggers: the database user needs the `TRIGGER` privilege, and with binary logging on also `SUPER` or `log_bin_trust_function_creators=1`.

A lost commit response is resolved against the receipt on a new connection. If recovery is unavailable, the session closes. After a paid exchange its balances are then excluded from logout persistence, because they were saved just before the exchange. After an opening or a free exchange the wallet is saved as usual. Login reloads inventory and ownership from storage and reconciles the five recycler achievement totals.

Achievements:

- `ACH_RecycledItems` counts recycled furniture and `ACH_RecycleBoxOpener` counts opened boxes.
- `ACH_RecycleEcoprizes` counts recycled furniture that was won from a box with a receipt.
- `ACH_RecyclerRarePrize` counts opened boxes of level 3 or higher, and `ACH_RecyclerGrandPrize` counts opened boxes of level 6. A hotel with fewer than six levels never progresses the grand prize achievement.

Migrations preserve existing prize rows, cooldowns, pending boxes and chat-style preferences. Only migration 150 enables a reward table, and only on an untouched setup. Removing this feature requires disabling new exchanges; keep its receipts and ownership tables.
