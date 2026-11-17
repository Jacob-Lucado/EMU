using Plus.HabboHotel.Rooms.Games.Teams;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Fact]
    public void AFreezeGameStartsEachPlayerWithFortyPoints()
    {
        FreezeTile(10, 1, 1);
        FreezePlayer(Viewer(1, 1), Team.Red);
        FreezePlayer(SecondFreezeUser(3, 3), Team.Blue);

        _room.GetFreeze().StartGame();

        Assert.Equal((40, 40), (_room.GetGameManager().Points[(int)Team.Red], _room.GetGameManager().Points[(int)Team.Blue]));
    }
}
