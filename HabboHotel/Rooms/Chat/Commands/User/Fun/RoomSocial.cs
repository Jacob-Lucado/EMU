namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

public static class RoomSocial
{
    public static readonly string[] EightBallAnswers =
    [
        "It is certain", "It is decidedly so", "Without a doubt", "Yes, definitely", "You may rely on it",
        "As I see it, yes", "Most likely", "Outlook good", "Yes", "Signs point to yes",
        "Reply hazy, try again", "Ask again later", "Better not tell you now", "Cannot predict now", "Concentrate and ask again",
        "Don't count on it", "My reply is no", "My sources say no", "Outlook not so good", "Very doubtful"
    ];

    public static string EightBall(Random random) => $"*shakes the magic 8 ball: {EightBallAnswers[random.Next(EightBallAnswers.Length)]}*";

    public static string Hug(string username) => $"*hugs {username}*";

    public static string HighFive(string username) => $"*high fives {username}*";

    public const int MaxOptionLength = 40;

    public static string? Pick(string text, Random random)
    {
        var options = text.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(option => option.Length > MaxOptionLength ? option[..MaxOptionLength] : option)
            .ToArray();

        return options.Length < 2 ? null : $"*picks: {options[random.Next(options.Length)]}*";
    }

    public static string Poke(string username) => $"*pokes {username}*";
}
