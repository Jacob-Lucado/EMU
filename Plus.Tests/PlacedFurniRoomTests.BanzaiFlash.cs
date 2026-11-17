using System.Buffers.Binary;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.Games.Teams;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Fact]
    public void PickedUpWinnerTileStopsBlinking()
    {
        var (tile, _, _, _) = EndedBanzai();
        _room.GetBanzai().RemoveTile(tile.Id);
        _room.GetBanzai().OnCycle();

        Assert.Equal("5", tile.LegacyDataString);
    }

    [Fact]
    public void TheWinningBanzaiTeamsLockedTilesBlinkFiveTimesAndStayLit()
    {
        var (red1, red2, blue, free) = EndedBanzai();
        var states = new List<(string, string, string, string)>();

        for (var tick = 0; tick < 12; tick++) {
            _room.GetBanzai().OnCycle();
            states.Add((red1.LegacyDataString, red2.LegacyDataString, blue.LegacyDataString, free.LegacyDataString));
        }

        Assert.Equal([.. Enumerable.Range(0, 10).Select(tick => tick % 2 == 0 ? ("0", "0", "11", "0") : ("5", "5", "11", "0")), ("5", "5", "11", "0"),
            ("5", "5", "11", "0")], states);
    }

    [Fact]
    public void EachBanzaiWinnerBlinkGoesOutAsOneObjectsDataUpdate()
    {
        EndedBanzai();
        _client.Packets.Clear();

        for (var tick = 0; tick < 12; tick++) {
            _room.GetBanzai().OnCycle();
        }

        var updates = _client.Packets.Where(packet => packet.Header is ServerPacketHeader.ObjectsDataUpdateComposer or ServerPacketHeader.ObjectDataUpdateComposer
            or ServerPacketHeader.ObjectUpdateComposer).ToList();
        Assert.Equal(10, updates.Count);
        Assert.All(updates, update => Assert.Equal((ServerPacketHeader.ObjectsDataUpdateComposer, 2),
            (update.Header, BinaryPrimitives.ReadInt32BigEndian(update.Body))));
    }

    [Fact]
    public void TheEndOfABanzaiGameTurnsOffItsUnclaimedTilesInOneUpdate()
    {
        var tiles = new[] { InteractionItem(100, 1, 1, InteractionType.Banzaifloor), InteractionItem(101, 2, 1, InteractionType.Banzaifloor),
            InteractionItem(102, 1, 2, InteractionType.Banzaifloor) };
        Viewer(3, 3);
        var banzai = _room.GetBanzai();
        banzai.BanzaiStart();
        _client.Packets.Clear();

        banzai.BanzaiEnd();

        var update = Assert.Single(_client.Packets, packet => packet.Header is ServerPacketHeader.ObjectsDataUpdateComposer
            or ServerPacketHeader.ObjectDataUpdateComposer or ServerPacketHeader.ObjectUpdateComposer);
        Assert.Equal((ServerPacketHeader.ObjectsDataUpdateComposer, 3), (update.Header, BinaryPrimitives.ReadInt32BigEndian(update.Body)));
        Assert.All(tiles, tile => Assert.Equal("0", tile.LegacyDataString));
    }

    private (Item Red1, Item Red2, Item Blue, Item Free) EndedBanzai()
    {
        var red1 = InteractionItem(100, 1, 1, InteractionType.Banzaifloor);
        var red2 = InteractionItem(101, 2, 1, InteractionType.Banzaifloor);
        var blue = InteractionItem(102, 1, 2, InteractionType.Banzaifloor);
        var free = InteractionItem(103, 2, 2, InteractionType.Banzaifloor);
        Viewer(3, 3);
        var banzai = _room.GetBanzai();
        banzai.BanzaiStart();
        Lock(red1, Team.Red, "5");
        Lock(red2, Team.Red, "5");
        Lock(blue, Team.Blue, "11");
        _room.GetGameManager().AddPointToTeam(Team.Red, 2);
        _room.GetGameManager().AddPointToTeam(Team.Blue, 1);
        banzai.BanzaiEnd();

        return (red1, red2, blue, free);
    }

    private static void Lock(Item tile, Team team, string state)
    {
        tile.Team = team;
        tile.Value = 3;
        tile.LegacyDataString = state;
    }
}
