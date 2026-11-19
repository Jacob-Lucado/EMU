using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms.PathFinding;

// Pure rules authorize the step; accepted execution keeps the legacy gate animation/timer.
internal sealed class GuildGateExecution(Room room, NavGrid grid)
{
    internal void Leave(uint itemId)
    {
        var gate = room.GetRoomItemHandler().GetItem(itemId);

        if (gate?.Definition.InteractionType == InteractionType.GuildGate && gate.InteractingUser != 0) {
            GateTransitionService.ToggleState(gate, state => state == "1" ? "0" : null, GateCloseReason.Walk, persist: false);
        }
    }

    internal void Accept(RoomUser actor, ActorProfile profile, int tile, StepPurpose purpose)
    {
        if ((grid.Flags[tile] & NavFlags.GuildGate) == 0 || profile.LegacyOverride
            || profile.IgnoreUsers || purpose == StepPurpose.Interaction) {
            return;
        }

        var habbo = actor.GetClient()?.GetHabbo();
        var gate = room.GetRoomItemHandler().GetItem(grid.SupportItem[tile]);

        if (habbo == null || gate == null) {
            return;
        }

        GateTransitionService.ToggleState(gate, state =>
        {
            if (state != "1") {
                return "1";
            }

            TrackPassage(gate);

            return null;
        }, GateCloseReason.Walk, persist: false, afterWrite: TrackPassage);

        void TrackPassage(Item opened)
        {
            opened.InteractingUser = habbo.Id;
            opened.RequestUpdate(4, true);
        }
    }
}
