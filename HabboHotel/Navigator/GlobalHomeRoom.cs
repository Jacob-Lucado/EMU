using Plus.Core.Settings;

namespace Plus.HabboHotel.Navigator;

// server_settings global_home_room sends users without their own home room to that room on login and on "go home". 0 turns it off.
public static class GlobalHomeRoom
{
    public const string Setting = "global_home_room";

    public static uint For(uint homeRoom, ISettingsManager settings) =>
        homeRoom != 0 ? homeRoom : uint.TryParse(settings.GetOptionalValue(Setting), out var room) ? room : 0;
}
