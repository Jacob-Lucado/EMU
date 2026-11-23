namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

public static class RoomDice
{
    public const int DefaultSides = 6;
    public const int MaxSides = 1000;
    public const int MaxCount = 10;

    public static bool TryParse(string? text, out int count, out int sides)
    {
        count = 1;
        sides = DefaultSides;

        if (string.IsNullOrWhiteSpace(text)) {
            return true;
        }

        var value = text.Trim().ToLowerInvariant();
        var split = value.IndexOf('d');

        if (split >= 0) {
            if (split > 0 && (!int.TryParse(value[..split], out count) || count is < 1 or > MaxCount)) {
                return false;
            }

            if (split == 0) {
                count = 1;
            }

            value = value[(split + 1)..];
        }

        return int.TryParse(value, out sides) && sides is >= 2 and <= MaxSides;
    }

    public static string Roll(int count, int sides, Random random)
    {
        if (count == 1) {
            return $"*rolls a d{sides} and gets {random.Next(1, sides + 1)}*";
        }

        var rolls = Enumerable.Range(0, count).Select(_ => random.Next(1, sides + 1)).ToArray();

        return $"*rolls {count}d{sides} and gets {string.Join(" + ", rolls)} = {rolls.Sum()}*";
    }

    public static string Flip(Random random) => random.Next(2) == 0 ? "*flips a coin: heads*" : "*flips a coin: tails*";
}
