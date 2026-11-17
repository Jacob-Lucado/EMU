using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Data.Toner;

/// <summary>The persisted companion is authoritative; item wire data mirrors its four fields.</summary>
internal static class TonerState
{
    internal static void Synchronize(Room room, Item item)
    {
        if (item.Definition.InteractionType != InteractionType.Toner || room.TonerData is not { } toner || toner.ItemId != item.Id) {
            return;
        }

        item.ExtraData = CreateData(toner);
    }

    internal static IntArrayDataFormat CreateData(TonerData toner)
    {
        if (toner.Enabled is < 0 or > 1 || toner.Hue is < 0 or > 255 || toner.Saturation is < 0 or > 255 || toner.Lightness is < 0 or > 255) {
            throw new InvalidOperationException("Invalid persisted toner companion.");
        }

        return new IntArrayDataFormat { Data = [toner.Enabled, toner.Hue, toner.Saturation, toner.Lightness] };
    }

    /// <summary>Reads the stored [state, hue, saturation, lightness], or null when the text holds no valid toner data.</summary>
    internal static IntArrayDataFormat? Load(string stored)
    {
        var parts = stored.Split('\n');
        var values = new List<int>(4);

        foreach (var part in parts) {
            if (!int.TryParse(part, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) || value > 255) {
                return null;
            }

            values.Add(value);
        }

        return values.Count == 4 && values[0] <= 1 ? new IntArrayDataFormat { Data = values } : null;
    }
}
