using System.Globalization;

namespace Plus.HabboHotel.Groups;

public static class BadgePartUtility
{
    public static string WorkBadgeParts(bool isBase, string partId, string colour, string position)
    {
        var id = int.Parse(partId);
        // Badge codes keep two digits per id: symbols from 100 use the "t" prefix with id - 100.
        var parts = isBase ? "b" : id >= 100 ? "t" : "s";
        partId = (!isBase && id >= 100 ? id - 100 : id).ToString("00", CultureInfo.InvariantCulture);
        colour = int.Parse(colour) < 10 ? $"0{colour}" : colour;
        parts += partId + colour + position;

        if (!isBase && (parts == "s00000" || parts == "s000000")) {
            return string.Empty;
        }

        return parts;
    }
}
