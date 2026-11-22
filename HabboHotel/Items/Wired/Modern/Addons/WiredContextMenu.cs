using System.Collections.Immutable;
using Plus.HabboHotel.Items.Wired.Configuration;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired.Modern.Addons;

/// <summary>One item of a room unit's context menu as one viewer sees it; the text has its placeholders filled.</summary>
public sealed record WiredContextMenuEntry(string ButtonId, string Text, bool Enabled);

/// <summary>One condition of a Context Menu Item, read on the viewer's user variable.</summary>
public readonly record struct WiredContextMenuCondition(int Comparison, int Value, string VariableId);

/// <summary>
/// The settings of a Context Menu Item add-on. Ints: [target kind, user on, user scope, pet on, pet scope, bot on, visibility count,
/// enable count, then three visibility and three enable conditions as (comparison, value)]; variables: the six conditions' variables;
/// text: button id TAB button text.
/// </summary>
public sealed record WiredContextMenuRule(string ButtonId, string Text, ImmutableArray<int> Ints, ImmutableArray<string> Variables)
{
    public const int IntCount = 8 + 4 * UnreleasedHabboFeatures.ContextMenuConditions;
    public const int VariableCount = 2 * UnreleasedHabboFeatures.ContextMenuConditions;

    public static bool IsButtonId(string id) => id.Length is > 0 and <= UnreleasedHabboFeatures.ContextMenuButtonIdLength
        && id.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');

    public static bool IsButtonText(string text) => text.Length is > 0 and <= UnreleasedHabboFeatures.ContextMenuButtonTextLength
        && text.IndexOfAny(['\t', '\r', '\n']) < 0;

    /// <summary>The rule of an active item; null for an item without a button id.</summary>
    public static WiredContextMenuRule? Parse(WiredConfiguration config)
    {
        var parts = config.Text.Split('\t', 2);

        return config.IntParams.Length == IntCount && config.VariableIds.Length == VariableCount && parts.Length == 2 && IsButtonId(parts[0])
            ? new(parts[0], parts[1], config.IntParams, config.VariableIds) : null;
    }

    public IEnumerable<WiredContextMenuCondition> Visibility => Conditions(0, Ints[6]);

    public IEnumerable<WiredContextMenuCondition> Enable => Conditions(UnreleasedHabboFeatures.ContextMenuConditions, Ints[7]);

    // 0 users is the only target kind; furni (1) is not built yet.
    public bool Shows(RoomUser viewer, RoomUser unit)
    {
        if (Ints[0] != (int)WiredContextMenuTarget.RoomUnit) {
            return false;
        }

        if (unit.IsPet) {
            return Ints[3] == 1 && (Ints[4] == UnreleasedHabboFeatures.AllPets || unit.PetData?.OwnerId == viewer.HabboId);
        }

        if (unit.IsBot) {
            return Ints[5] == 1;
        }

        var self = ReferenceEquals(unit, viewer);

        return Ints[1] == 1 && Ints[2] switch
        {
            UnreleasedHabboFeatures.Everyone => true,
            UnreleasedHabboFeatures.EveryoneExceptViewer => !self,
            UnreleasedHabboFeatures.OnlyViewer => self,
            _ => false
        };
    }

    /// <summary>Habbo's comparisons 0 &lt;, 1 =, 2 &gt;, 3 &lt;=, 4 !=, 5 &gt;=, then 6 exists and 7 does not exist.</summary>
    public static bool Holds(int comparison, long? value, int literal) => comparison switch
    {
        UnreleasedHabboFeatures.ContextMenuExists => value != null,
        UnreleasedHabboFeatures.ContextMenuNotExists => value == null,
        _ => value is { } held && comparison switch
        {
            0 => held < literal,
            1 => held == literal,
            2 => held > literal,
            3 => held <= literal,
            4 => held != literal,
            5 => held >= literal,
            _ => false
        }
    };

    private IEnumerable<WiredContextMenuCondition> Conditions(int first, int count) => Enumerable.Range(first, Math.Clamp(count, 0, UnreleasedHabboFeatures.ContextMenuConditions))
        .Select(index => new WiredContextMenuCondition(Ints[8 + 2 * index], Ints[9 + 2 * index], Variables[index]));
}
