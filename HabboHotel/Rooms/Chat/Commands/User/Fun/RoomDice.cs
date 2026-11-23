namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

public static class RoomDice
{
    public const int DefaultSides = 6;
    public const int MaxSides = 1000;

    public static bool TryParseSides(string? text, out int sides)
    {
        sides = DefaultSides;

        if (string.IsNullOrWhiteSpace(text)) {
            return true;
        }

        var value = text.Trim();

        if (value.StartsWith('d') || value.StartsWith('D')) {
            value = value[1..];
        }

        return int.TryParse(value, out sides) && sides is >= 2 and <= MaxSides;
    }

    public static string Roll(int sides, Random random) => $"*rolls a d{sides} and gets {random.Next(1, sides + 1)}*";

    public static string Flip(Random random) => random.Next(2) == 0 ? "*flips a coin: heads*" : "*flips a coin: tails*";
}
