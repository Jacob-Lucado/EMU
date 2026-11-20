namespace Plus.HabboHotel.GameClients;

public class ConnectionCheckConfiguration
{
    public int PingIntervalSeconds { get; set; } = 30;
    public int MaxUnansweredPings { get; set; } = 3;
}
