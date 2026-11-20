using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Plus.Communication.Packets.Incoming.Handshake;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;
using Xunit;

namespace Plus.Tests;

public sealed class GameClientConnectionCheckTests
{
    [Fact]
    public void A_logged_in_client_gets_a_ping_every_interval()
    {
        var world = new World();
        var client = world.LoggedIn(1);

        world.Advance(TimeSpan.FromSeconds(29));
        Assert.Equal(0, client.Pings);

        world.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(1, client.Pings);

        world.Advance(TimeSpan.FromSeconds(15));
        Assert.Equal(1, client.Pings);

        world.Advance(TimeSpan.FromSeconds(15));
        Assert.Equal(2, client.Pings);
    }

    [Fact]
    public void A_client_with_three_unanswered_pings_is_disconnected_and_an_answering_client_stays()
    {
        var world = new World();
        var silent = world.LoggedIn(1);
        var answering = world.LoggedIn(2);

        for (var check = 0; check < 3; check++) {
            world.Advance(TimeSpan.FromSeconds(30));
            answering.Pong();
        }

        Assert.Equal(3, silent.Pings);
        Assert.False(silent.Disconnected);

        world.Advance(TimeSpan.FromSeconds(30));

        Assert.True(silent.Disconnected);
        Assert.Equal(3, silent.Pings);
        Assert.False(answering.Disconnected);
        Assert.Equal(4, answering.Pings);
    }

    [Fact]
    public void A_late_pong_keeps_the_client_and_starts_the_count_again()
    {
        var world = new World();
        var client = world.LoggedIn(1);

        for (var check = 0; check < 3; check++) {
            world.Advance(TimeSpan.FromSeconds(30));
        }

        world.Advance(TimeSpan.FromSeconds(29));
        client.Pong();
        world.Advance(TimeSpan.FromSeconds(1));

        Assert.False(client.Disconnected);
        Assert.Equal(4, client.Pings);

        world.Advance(TimeSpan.FromSeconds(30));
        world.Advance(TimeSpan.FromSeconds(30));
        Assert.False(client.Disconnected);

        world.Advance(TimeSpan.FromSeconds(30));
        Assert.True(client.Disconnected);
        Assert.Equal(6, client.Pings);
    }

    [Fact]
    public void A_client_that_always_answers_is_never_disconnected()
    {
        var world = new World();
        var client = world.LoggedIn(1);

        for (var check = 0; check < 20; check++) {
            world.Advance(TimeSpan.FromSeconds(30));
            client.Pong();
        }

        Assert.Equal(20, client.Pings);
        Assert.False(client.Disconnected);
    }

    [Fact]
    public void The_interval_and_the_limit_come_from_the_configuration()
    {
        var world = new World(new ConnectionCheckConfiguration { PingIntervalSeconds = 10, MaxUnansweredPings = 1 });
        var client = world.LoggedIn(1);

        world.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(1, client.Pings);
        Assert.False(client.Disconnected);

        world.Advance(TimeSpan.FromSeconds(10));
        Assert.True(client.Disconnected);
    }

    [Fact]
    public void A_connection_that_has_not_logged_in_is_not_pinged_or_disconnected()
    {
        var world = new World();
        var loading = new Probe(new Habbo { Id = 7, Username = "loading" });

        world.Advance(TimeSpan.FromMinutes(10));

        Assert.Equal(0, loading.Pings);
        Assert.False(loading.Disconnected);
    }

    [Fact]
    public void A_client_that_cannot_be_sent_to_does_not_stop_the_check_for_others()
    {
        var world = new World();
        var broken = world.LoggedIn(1);
        broken.Client.Revision = null;
        var client = world.LoggedIn(2);

        world.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(1, client.Pings);
    }

    private sealed class World
    {
        private readonly FakeTimeProvider _clock = new(DateTimeOffset.Parse("2026-11-20T12:00:00Z"));
        private readonly GameClientManager _manager;

        public World(ConnectionCheckConfiguration? configuration = null)
        {
            _manager = new GameClientManager(null!, NullLogger<GameClientManager>.Instance, Options.Create(configuration ?? new ConnectionCheckConfiguration()), _clock);
            _manager.OnCycle();
        }

        public Probe LoggedIn(int id)
        {
            var probe = new Probe(new Habbo { Id = id, Username = $"user{id}" });
            probe.Client.Id = Guid.NewGuid();
            _manager.RegisterClient(probe.Client, id, $"user{id}");

            return probe;
        }

        public void Advance(TimeSpan time)
        {
            _clock.Advance(time);
            _manager.OnCycle();
        }
    }

    private sealed class Probe
    {
        private readonly List<(uint Header, byte[] Payload)> _sent;

        public Probe(Habbo habbo)
        {
            (Client, _sent) = HabbiconTestSupport.Client(habbo);
            Client.DisconnectRequested = () => Disconnected = true;
        }

        public Plus.Communication.Flash.FlashGameClient Client { get; }

        public bool Disconnected { get; private set; }

        public int Pings => _sent.Count(packet => packet.Header == ServerPacketHeader.PingComposer);

        public void Pong() => new PongEvent().Parse(Client, null!).GetAwaiter().GetResult();
    }
}
