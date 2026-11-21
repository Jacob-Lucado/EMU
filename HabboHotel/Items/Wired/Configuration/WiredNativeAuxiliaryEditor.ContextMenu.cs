using Plus.HabboHotel.Items.Wired.Modern.Addons;

namespace Plus.HabboHotel.Items.Wired.Configuration;

/// <summary>The context menu boxes. Plus made these editors up from Habbo's previews, so the grammar is ours (see UnreleasedHabboFeatures).</summary>
internal static partial class WiredNativeAuxiliaryEditor
{
    private static IEnumerable<KeyValuePair<string, Spec>> ContextMenu()
    {
        // See WiredContextMenuRule for the 20 ints, the six variables and the text. An empty button id is the inactive item.
        yield return new(UnreleasedHabboFeatures.ContextMenuItemAddon, new(Meta(0, 0,
            [0, 1, 1, 0, 0, 0, 0, 0, .. Enumerable.Repeat(new[] { 6, 0 }, 2 * UnreleasedHabboFeatures.ContextMenuConditions).SelectMany(pair => pair)],
            WiredContextMenuRule.VariableCount), n =>
        {
            var p = n.OwnedIntParams;
            var parts = n.Text.Split('\t', 2);

            return Shape(n, WiredContextMenuRule.IntCount, 0, 0, WiredContextMenuRule.VariableCount) && p[0] == (int)WiredContextMenuTarget.RoomUnit
                && p[1] is 0 or 1 && In(p[2], 0, 2) && p[3] is 0 or 1 && p[4] is 0 or 1 && p[5] is 0 or 1
                && In(p[6], 0, UnreleasedHabboFeatures.ContextMenuConditions) && In(p[7], 0, UnreleasedHabboFeatures.ContextMenuConditions)
                && Enumerable.Range(0, WiredContextMenuRule.VariableCount).All(index => In(p[8 + 2 * index], 0, UnreleasedHabboFeatures.ContextMenuNotExists))
                && (n.Text.Length == 0 || parts.Length == 2 && WiredContextMenuRule.IsButtonId(parts[0]) && WiredContextMenuRule.IsButtonText(parts[1]))
                    ? Draft(n, p, n.Text) : null;
        }));
        // [target kind (0 users)]; the button id.
        yield return new(UnreleasedHabboFeatures.ClickContextMenuTrigger, new(Meta(0, 0, [0]), n =>
            Shape(n, 1, 0, 0, 0) && n.OwnedIntParams[0] == (int)WiredContextMenuTarget.RoomUnit
            && (n.Text.Length == 0 || WiredContextMenuRule.IsButtonId(n.Text)) ? Draft(n, n.OwnedIntParams, n.Text) : null));
    }
}
