using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Catalog;

public class PresentDeliverErrorComposer : IServerPacket
{
    private readonly bool _creditError;
    private readonly bool _ducketError;
    private readonly int? _activityPointType;

    public uint MessageId => ServerPacketHeader.PresentDeliverErrorComposer;

    /// <summary>The client reads the activity point type only when it is sent.</summary>
    public PresentDeliverErrorComposer(bool creditError, bool ducketError, int? activityPointType = null)
    {
        _creditError = creditError;
        _ducketError = ducketError;
        _activityPointType = activityPointType;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteBoolean(_creditError);
        packet.WriteBoolean(_ducketError);

        if (_activityPointType is { } type) {
            packet.WriteInteger(type);
        }
    }
}
