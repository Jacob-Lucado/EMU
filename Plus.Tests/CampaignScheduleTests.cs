using Microsoft.Extensions.Time.Testing;
using Plus.Communication.Flash;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.LandingView;
using Plus.HabboHotel.Users;
using Xunit;

namespace Plus.Tests;

public class CampaignScheduleTests
{
    private const string Schedule = "2026-11-16 12:00,first;2026-11-17 12:00,second;2026-11-18 12:00,third";

    [Theory]
    [InlineData("2026-11-16 20:52", "2026-11-16 12:00,nov26cf;2026-11-18 12:00,nov26clothcrackb;2026-11-19 12:00,nov26rm;2026-11-24 12:00,doublecredits23", "nov26cf")]
    [InlineData("2026-11-16 20:52", "2026-11-13 12:00,nov26ducket1;2026-11-18 12:00,nov26coll1;2026-11-20 11:00,nov26itanni1;2026-11-25 12:00,nov26coll1", "nov26ducket1")]
    [InlineData("2026-11-16 20:52", "2026-11-16 12:00,anc_jungle_bundle;2026-11-17 12:00,nov26crackb;2026-11-18 12:00,anc_jungle_bundle;2026-11-20 12:00,nov26r3", "anc_jungle_bundle")]
    [InlineData("2026-11-16 20:52", "2026-11-16 12:00,apr18army1;2026-11-19 12:00,nov26ducket1;2026-11-24 12:00,easter24dazzlingforest1", "apr18army1")]
    [InlineData("2026-11-16 20:52", "", "")]
    [InlineData("2026-11-17 13:00", Schedule, "second")]
    [InlineData("2026-11-17 12:00", Schedule, "second")]
    [InlineData("2026-11-17 11:59", Schedule, "first")]
    [InlineData("2026-12-01 00:00", Schedule, "third")]
    [InlineData("2026-11-16 11:59", Schedule, "")]
    [InlineData("2026-11-17 13:00", "2026-11-18 12:00,third;2026-11-17 12:00,second;2026-11-16 12:00,first", "second")]
    [InlineData("2026-11-17 13:00", "soon,first;2026-11-16 12:00,second;", "second")]
    public void CampaignAnswersWithTheCodeWhoseStartIsTheLatestPassed(string now, string schedule, string code)
    {
        var packet = Refresh(schedule, DateTimeOffset.Parse(now + "Z"), TimeZoneInfo.Utc);

        Assert.Equal(schedule, packet.ReadString());
        Assert.Equal(code, packet.ReadString());
        Assert.False(packet.HasDataRemaining());
    }

    [Fact]
    public void ScheduleTimesAreHotelLocalTimes()
    {
        var plusTwo = TimeZoneInfo.CreateCustomTimeZone("plus-two", TimeSpan.FromHours(2), "plus-two", "plus-two");

        // 10:30 UTC is 12:30 in the hotel, after the 12:00 start of "second".
        var packet = Refresh(Schedule, new DateTimeOffset(2026, 11, 17, 10, 30, 0, TimeSpan.Zero), plusTwo);

        Assert.Equal(Schedule, packet.ReadString());
        Assert.Equal("second", packet.ReadString());
    }

    private static FlashIncomingPacket Refresh(string schedule, DateTimeOffset utcNow, TimeZoneInfo zone)
    {
        var time = new FakeTimeProvider(utcNow);
        time.SetLocalTimeZone(zone);
        var (client, sent) = HabbiconTestSupport.Client(new Habbo());

        new LandingViewPresentationService(null!, time).RefreshCampaign(client, schedule);

        var (header, payload) = Assert.Single(sent);
        Assert.Equal(ServerPacketHeader.CampaignComposer, header);

        return new FlashIncomingPacket { Buffer = payload };
    }
}
