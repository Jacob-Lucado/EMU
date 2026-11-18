using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Rooms.Avatar;

public class AvatarEffectComposer : IServerPacket
{
    private readonly int _playerId;
    private readonly int _effectId;
    private readonly int _delay;
    public uint MessageId => ServerPacketHeader.AvatarEffectComposer;

    public AvatarEffectComposer(int playerId, int effectId, int delay = 0)
    {
        _playerId = playerId;
        _effectId = effectId;
        _delay = delay;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_playerId);
        packet.WriteInteger(_effectId);
        packet.WriteInteger(_delay);
    }
}
