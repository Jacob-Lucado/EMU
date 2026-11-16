namespace Plus.HabboHotel.Rooms;

/// <summary>The room settings for the door tile, idle sleep, idle autokick and pet speech.</summary>
public sealed record RoomBehaviorSettings(bool LeaveOnDoorTile, bool IdleSleepEnabled, int IdleSleepTimeoutSeconds,
    bool IdleAutokickEnabled, int IdleAutokickTimeoutSeconds, bool MuteAllPets)
{
    public const string InvalidIdleSleepTimeout = "idleSleepTimeoutSeconds";
    public const string InvalidIdleAutokickTimeout = "idleAutokickTimeoutSeconds";

    public static RoomBehaviorSettings Default { get; } = new(true, true, 300, true, 900, false);

    /// <summary>The room ticks an avatar stays idle before it falls asleep, or null when idle sleep is off.</summary>
    public int? IdleSleepTicks => IdleSleepEnabled ? Ticks(IdleSleepTimeoutSeconds) : null;

    /// <summary>The room ticks an avatar stays idle before it is kicked, or null when idle autokick is off.</summary>
    public int? IdleAutokickTicks => IdleAutokickEnabled ? Ticks(IdleAutokickTimeoutSeconds) : null;

    /// <summary>The settings a save stores, or the name of the field that makes the whole save fail.</summary>
    /// <remarks>
    /// Without Habbo Club only <see cref="MuteAllPets"/> changes. A disabled timer keeps its stored timeout, and only enabled timers
    /// are checked: sleep 30 to 3600 s, autokick 60 to 36000 s and, with both on, at least 30 s after sleep.
    /// </remarks>
    public static RoomBehaviorSettingsChange Change(RoomBehaviorSettings stored, RoomBehaviorSettings requested, bool club)
    {
        if (!club) {
            return new(stored with { MuteAllPets = requested.MuteAllPets }, null);
        }

        var sleep = requested.IdleSleepEnabled ? requested.IdleSleepTimeoutSeconds : stored.IdleSleepTimeoutSeconds;
        var kick = requested.IdleAutokickEnabled ? requested.IdleAutokickTimeoutSeconds : stored.IdleAutokickTimeoutSeconds;
        var saved = requested with { IdleSleepTimeoutSeconds = sleep, IdleAutokickTimeoutSeconds = kick };

        if (saved.IdleSleepEnabled && sleep is < 30 or > 3600) {
            return new(stored, InvalidIdleSleepTimeout);
        }

        if (saved.IdleAutokickEnabled && (kick is < 60 or > 36000 || saved.IdleSleepEnabled && kick < sleep + 30)) {
            return new(stored, InvalidIdleAutokickTimeout);
        }

        return new(saved, null);
    }

    private static int Ticks(int seconds) => (int)(TimeSpan.FromSeconds(seconds) / RoomCycle.Interval);
}

public sealed record RoomBehaviorSettingsChange(RoomBehaviorSettings Settings, string? InvalidField);
