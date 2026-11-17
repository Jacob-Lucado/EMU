namespace Plus.HabboHotel.Rooms.AI;

/// <summary>
/// The commands a pet type can learn, in learning order. A level 1 pet knows the first
/// <see cref="StartCount"/> commands and learns the next one at every level.
/// </summary>
public sealed class PetCommandSet(int[] commands, int startCount)
{
    public const int Free = 0;
    public const int Drink = 14;
    public const int Eat = 43;
    public const int Breed = 46;

    private static readonly PetCommandSet Dog = new([46, 0, 1, 14, 43, 2, 3, 4, 17, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 16, 24, 25, 26], 5);

    // Types without their own list use the dog list.
    private static readonly Dictionary<int, PetCommandSet> ByType = new()
    {
        [0] = Dog,
        [15] = new([0, 14, 43, 44, 2, 3, 6, 7, 10, 12, 11, 15, 16, 24, 25, 26, 45], 4),
        [20] = new([0, 2, 13, 3, 6, 11, 7, 17, 15, 16], 2),
        [21] = new([0, 1, 2, 3, 10, 5, 9, 7, 11, 13, 15, 16, 34, 32, 33], 2),
        [29] = new([0, 1, 14, 43, 2, 3, 4, 17, 6, 7, 8, 11, 13, 15, 16, 24, 25, 26], 4)
    };

    public IReadOnlyList<int> Commands => commands;

    public int StartCount => startCount;

    public static PetCommandSet For(int petType) => ByType.GetValueOrDefault(petType, Dog);

    public IReadOnlyList<int> Learned(int level) => commands.Take(Math.Max(0, startCount + level - 1)).ToArray();

    public bool Has(int command) => commands.Contains(command);

    public bool HasLearned(int command, int level) => Learned(level).Contains(command);
}
