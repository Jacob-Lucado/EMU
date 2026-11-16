namespace Plus.HabboHotel.Rooms;

/// <summary>The room settings for the door tile, idle sleep, idle autokick and pet speech.</summary>
public sealed record RoomBehaviorSettings(bool LeaveOnDoorTile, bool IdleSleepEnabled, int IdleSleepTimeoutSeconds,
    bool IdleAutokickEnabled, int IdleAutokickTimeoutSeconds, bool MuteAllPets)
{
    public static RoomBehaviorSettings Default { get; } = new(true, true, 300, true, 900, false);

    /// <summary>The room ticks an avatar stays idle before it falls asleep, or null when idle sleep is off.</summary>
    public int? IdleSleepTicks => throw new NotImplementedException();

    /// <summary>The room ticks an avatar stays idle before it is kicked, or null when idle autokick is off.</summary>
    public int? IdleAutokickTicks => throw new NotImplementedException();

    /// <summary>The settings a save stores, or the name of the field that makes the whole save fail.</summary>
    public static RoomBehaviorSettingsChange Change(RoomBehaviorSettings stored, RoomBehaviorSettings requested, bool club) =>
        throw new NotImplementedException();
}

public sealed record RoomBehaviorSettingsChange(RoomBehaviorSettings Settings, string? InvalidField);
