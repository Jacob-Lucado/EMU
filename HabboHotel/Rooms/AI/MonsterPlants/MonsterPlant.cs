namespace Plus.HabboHotel.Rooms.AI.MonsterPlants;

/// <summary>
/// The life of a monster plant (pet type 16). Growth and wellbeing are clock based: the plant stores when it started
/// growing and when its wellbeing runs out, and every other value follows from the current time.
/// </summary>
public sealed class MonsterPlant(int petId, int rarity, DateTimeOffset growthStartedAt, DateTimeOffset wellbeingUntil,
    bool canBreed, bool breedingPermission)
{
    public const int PetType = 16;
    public const int AdultLevel = 7;

    /// <summary>The growth and energy bars of a plant both count up to 120.</summary>
    public const int BarMaximum = 120;

    private const int ExperiencePerLevel = BarMaximum / (AdultLevel - 1);

    public static TimeSpan GrowthTime { get; } = TimeSpan.FromHours(48);
    public static TimeSpan SeedlingMaxWellbeing { get; } = TimeSpan.FromHours(36);
    public static TimeSpan GrownMaxWellbeing { get; } = TimeSpan.FromHours(72);

    public int PetId { get; } = petId;
    public int Rarity { get; } = rarity;
    public DateTimeOffset GrowthStartedAt { get; private set; } = growthStartedAt;
    public DateTimeOffset WellbeingUntil { get; private set; } = wellbeingUntil;
    public bool CanBreed { get; private set; } = canBreed;
    public bool BreedingPermission { get; private set; } = breedingPermission;

    /// <summary>A freshly planted seed.</summary>
    public static MonsterPlant Seeded(int petId, int rarity, DateTimeOffset plantedAt) =>
        new(petId, rarity, plantedAt, plantedAt + SeedlingMaxWellbeing, true, false);

    public bool IsDead(DateTimeOffset now) => now >= WellbeingUntil;

    public bool IsGrown(DateTimeOffset now) => now >= GrowthStartedAt + GrowthTime;

    /// <summary>Growth progress from 0 to 120; every 20 is one level.</summary>
    public int Experience(DateTimeOffset now)
    {
        var grown = Math.Clamp((now - GrowthStartedAt) / GrowthTime, 0, 1);

        return (int)Math.Floor(grown * BarMaximum);
    }

    public int Level(DateTimeOffset now) => Math.Min(AdultLevel, 1 + Experience(now) / ExperiencePerLevel);

    public int ExperienceGoal(DateTimeOffset now) => Math.Min(BarMaximum, Level(now) * ExperiencePerLevel);

    public TimeSpan RemainingGrowth(DateTimeOffset now) => IsGrown(now) ? TimeSpan.Zero : GrowthStartedAt + GrowthTime - now;

    public TimeSpan MaxWellbeing(DateTimeOffset now) => IsGrown(now) ? GrownMaxWellbeing : SeedlingMaxWellbeing;

    public TimeSpan RemainingWellbeing(DateTimeOffset now) => IsDead(now) ? TimeSpan.Zero : WellbeingUntil - now;

    /// <summary>Wellbeing shown as an energy bar out of 120.</summary>
    public int Energy(DateTimeOffset now) =>
        (int)Math.Floor(Math.Min(1, RemainingWellbeing(now) / MaxWellbeing(now)) * BarMaximum);

    /// <summary>A plant can breed once it is fully grown and alive, until it has bred.</summary>
    public bool CanBreedNow(DateTimeOffset now) => CanBreed && !IsDead(now) && IsGrown(now);

    /// <summary>The room posture: <c>rip</c> when dead, <c>grw1</c> to <c>grw6</c> while growing, <c>std</c> when grown.</summary>
    public string Posture(DateTimeOffset now)
    {
        if (IsDead(now)) {
            return "rip";
        }

        var level = Level(now);

        return level < AdultLevel ? $"grw{level}" : "std";
    }

    /// <summary>Treating a living plant fills its wellbeing. A dead plant needs a revival potion instead.</summary>
    public bool Treat(DateTimeOffset now)
    {
        if (IsDead(now) || Energy(now) >= BarMaximum * 0.98) {
            return false;
        }

        WellbeingUntil = now + MaxWellbeing(now);

        return true;
    }
}
