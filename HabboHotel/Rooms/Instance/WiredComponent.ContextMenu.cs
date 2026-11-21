using System.Collections.Immutable;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Items.Wired.Configuration;
using Plus.HabboHotel.Items.Wired.Modern.Addons;
using Plus.HabboHotel.Items.Wired.Runtime;
using Plus.HabboHotel.Items.Wired.Variables;

namespace Plus.HabboHotel.Rooms.Instance;

/// <summary>The room's Context Menu Item add-ons: the menu each viewer sees on a room unit, and the clicks that fire User Clicks Context Menu.</summary>
public partial class WiredComponent
{
    /// <summary>The items a viewer sees on a unit, in add-on order (stack height, then item id); disabled items are kept so the client greys them out.</summary>
    public IReadOnlyList<WiredContextMenuEntry> ContextMenu(RoomUser viewer, RoomUser unit) =>
        [.. MenuItems(viewer, unit, out var context).Select(item => new WiredContextMenuEntry(item.Rule.ButtonId, MenuText(context!, item.Box, item.Rule.Text), item.Enabled))];

    /// <summary>A click counts only when an item with the button id is visible and enabled for the clicker on that unit now.</summary>
    public bool ClickContextMenu(RoomUser clicker, RoomUser unit, string buttonId) =>
        MenuItems(clicker, unit, out _).Any(item => item.Enabled && item.Rule.ButtonId == buttonId)
        && Dispatch(new(WiredEventKind.ContextMenu) { Actor = clicker, TargetUser = unit, Message = buttonId });

    /// <summary>The button ids of the room's Context Menu Items, sorted and once each: editor context 900 of the click trigger.</summary>
    internal ImmutableArray<string> ContextMenuButtonIds() => [.. MenuBoxes().Select(box => WiredContextMenuRule.Parse(box.Configuration)?.ButtonId)
        .OfType<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    private IEnumerable<IWiredConfiguredItem> MenuBoxes() =>
        ConfiguredBoxes(UnreleasedHabboFeatures.ContextMenuItemAddon).OrderBy(box => box.Item.GetZ).ThenBy(box => box.Item.Id);

    private List<(IWiredConfiguredItem Box, WiredContextMenuRule Rule, bool Enabled)> MenuItems(RoomUser viewer, RoomUser unit, out WiredRuntimeContext? context)
    {
        var items = new List<(IWiredConfiguredItem, WiredContextMenuRule, bool)>();
        context = null;
        var candidates = MenuBoxes().Select(box => (Box: box, Rule: WiredContextMenuRule.Parse(box.Configuration)))
            .Where(pair => pair.Rule?.Shows(viewer, unit) == true).ToArray();

        if (candidates.Length == 0) {
            return items;
        }

        // The viewer is the user of the conditions and the placeholders.
        context = new WiredRuntimeContext(_room, new(WiredEventKind.ContextMenu) { Actor = viewer, TargetUser = unit }, _targets, this)
        { NowMilliseconds = _engine.NowMilliseconds, Triggering = new([], [viewer.VirtualId]) };
        using var queries = new WiredVariableQueries(Variables.Module, WiredVariableRuntimeFrames.Create(context, context.VariableFrame));

        foreach (var (box, rule) in candidates) {
            bool Holds(WiredContextMenuCondition condition) => WiredContextMenuRule.Holds(condition.Comparison,
                queries.ReadOperand(WiredVariableTarget.User, condition.VariableId, WiredSources.Trigger, WiredSources.Trigger, box.Configuration),
                condition.Value);

            if (rule!.Visibility.All(Holds)) {
                items.Add((box, rule, rule.Enable.All(Holds)));
            }
        }

        return items;
    }

    // The text output add-ons stacked with the item fill their placeholders for the viewer, then the room's global placeholders.
    private string MenuText(WiredRuntimeContext context, IWiredConfiguredItem box, string text)
    {
        if (!text.Contains("$(", StringComparison.Ordinal)) {
            return text;
        }

        try {
            foreach (var output in (_room.GetGameMap()?.GetCoordinatedItems(new(box.Item.GetX, box.Item.GetY)) ?? [])
                         .OrderBy(item => item.GetZ).ThenBy(item => item.Id)
                         .Select(item => TryGet(item.Id, out var found) ? found : null).OfType<IWiredConfiguredItem>()
                         .Where(found => found.Descriptor.CanonicalName.StartsWith("wf_xtra_text_output_", StringComparison.Ordinal))
                         .OfType<IWiredContextualAddon>()) {
                output.Apply(context);
            }

            return context.Policy.FormatText(context, text);
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException or KeyNotFoundException) {
            return WiredTextPlaceholders.FillTriggerText(context, text);
        }
    }
}
