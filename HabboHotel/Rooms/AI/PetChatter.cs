namespace Plus.HabboHotel.Rooms.AI;

/// <summary>
/// What a pet type says on its own and how often on average. A type without an entry stays silent.
/// </summary>
public sealed record PetChatter(TimeSpan Interval, string[] Lines)
{
    private static readonly string[] Lonely =
    [
        "*Come out, come out, wherever you are!*",
        "*I miss you.*",
        "*I'm lonely without you.*",
        "*Where did everybody go again?*",
        "*Where's my owner?*"
    ];

    private static readonly Dictionary<int, PetChatter> ByType = new()
    {
        [0] = new(TimeSpan.FromSeconds(29),
        [
            "*I want something to drink*",
            "*Thirsty*",
            "*feeling playful*",
            "*getting exhausted*",
            "*looking for a drink*",
            "*looking for a nest to sleep in*",
            "*tired*",
            "*wants to play*",
            "Do you want to play?",
            "I can't even move",
            "I need to sleep for a whole week",
            "It's not like we're in the desert: Water!!"
        ]),
        [1] = new(TimeSpan.FromSeconds(28),
        [
            "*Sardines here I come*",
            "*Sleepy kitty*",
            "*is hunting a bird*",
            "*is hunting a rat*",
            "*ready to drop*",
            "*thirsty*",
            "*wants to play*",
            "*water please*",
            "Food, please",
            "I can't even move",
            "I could eat a whale",
            "I need to sleep for a whole week",
            "It's not like we're in the desert: Water!!",
            "My stomach is growling",
            "So, will you play with me?"
        ]),
        [21] = new(TimeSpan.FromSeconds(122),
        [
            "A superior man is modest in his speech, but exceeds in his actions.",
            "Be true to your ancestors in all things.",
            "Do not impose on others what you yourself do not desire.",
            "If you come to a fork in the road, take it.",
            "Is it possible to know the good without knowing anything of evil?",
            "Real knowledge is to know the extent of one's ignorance.",
            "There's no such thing as \"free\" Credits. Everything has a price, and \"Free\" items often cost far too much...",
            "To go beyond is as wrong as to fall short.",
            "Water takes the easiest path downward."
        ]),
        [22] = new(TimeSpan.FromSeconds(122),
        [
            "A lie told often enough becomes the truth.",
            "He who overcomes by force has only overcome half his foe",
            "I would prefer a hard liberty to a soft servility",
            "It's not a lie if both people believe it.",
            "Psst...I know how to get free Credits... Just give me your account.... And your soul...",
            "The world is yours, if you have the strength of will to reach out and take it!",
            "Who is he who says ?No??",
            "You know, I wouldn't scam if the world was perfect...",
            "You see something that you want? You deserve it, by virtue alone of desiring it."
        ]),
        [28] = new(TimeSpan.FromSeconds(9), Lonely),
        [30] = new(TimeSpan.FromSeconds(9), Lonely)
    };

    public static PetChatter? For(int petType) => ByType.GetValueOrDefault(petType);

    /// <summary>The chance to say a line in one room tick, so a line comes once per <see cref="Interval"/> on average.</summary>
    public double ChancePerTick => RoomCycle.Interval / Interval;
}
