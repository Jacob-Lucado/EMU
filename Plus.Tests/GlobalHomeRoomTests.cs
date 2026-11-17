using Plus.Core.Settings;
using Plus.HabboHotel.Navigator;
using Xunit;

namespace Plus.Tests;

public sealed class GlobalHomeRoomTests
{
    [Fact]
    public void Without_the_setting_a_user_without_a_home_room_has_none()
    {
        Assert.Equal(0u, GlobalHomeRoom.For(0, Settings()));
        Assert.Equal(0u, GlobalHomeRoom.For(0, Settings(("global_home_room", "0"))));
    }

    [Fact]
    public void A_user_without_a_home_room_gets_the_global_home_room()
    {
        Assert.Equal(42u, GlobalHomeRoom.For(0, Settings(("global_home_room", "42"))));
    }

    [Fact]
    public void A_users_own_home_room_wins_over_the_global_home_room()
    {
        Assert.Equal(7u, GlobalHomeRoom.For(7, Settings(("global_home_room", "42"))));
        Assert.Equal(7u, GlobalHomeRoom.For(7, Settings()));
    }

    [Fact]
    public void An_invalid_value_turns_the_global_home_room_off()
    {
        Assert.Equal(0u, GlobalHomeRoom.For(0, Settings(("global_home_room", "abc"))));
        Assert.Equal(0u, GlobalHomeRoom.For(0, Settings(("global_home_room", "-1"))));
    }

    private static ISettingsManager Settings(params (string Key, string Value)[] values) => new TestRoomSettings(values.ToDictionary(v => v.Key, v => v.Value));
}
