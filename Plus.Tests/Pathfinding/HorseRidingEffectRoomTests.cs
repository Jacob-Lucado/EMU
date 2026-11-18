using Plus.Communication.Packets.Incoming.Rooms.AI.Pets.Horse;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.Chat.Pets.Locale;
using Plus.HabboHotel.Users.Effects;
using Xunit;

namespace Plus.Tests;

public partial class PlacedFurniRoomTests
{
    [Theory]
    [InlineData(9, 77)]
    [InlineData(10, 103)]
    public void MountedRiderWearsTheEffectOfTheSaddle(int saddle, int effect)
    {
        var rider = ExecutorActor(1, 1);
        var habbo = _client.GetHabbo();
        habbo.Effects = new EffectsComponent(TimeProvider.System);
        habbo.Effects.Init(habbo);
        var horse = ExternalLifecycleHorse(2, 1);
        Assert.IsType<Pet>(horse.PetData).Saddle = saddle;
        var items = Proxy<IItemDataManager>((method, _) => method == "get_Items"
            ? new Dictionary<uint, ItemDefinition>
            {
                [7100] = new() { Id = 7100, InteractionType = InteractionType.HorseSaddle1, CustomParams = "15 4 9 0 77" },
                [4450] = new() { Id = 4450, InteractionType = InteractionType.HorseSaddle2, CustomParams = "15 4 10 0 103" },
            }
            : throw new InvalidOperationException(method));
        ExecutorTick();

        new RideHorseEvent(new HorseRidingService(Proxy<IPetLocale>((_, _) => new[] { "horse" }), items))
            .Parse(_room, _client, ClientPacket(50, true)).GetAwaiter().GetResult();
        ExecutorTick();
        ExecutorTick();

        Assert.True(rider.RidingHorse);
        Assert.Equal(effect, habbo.Effects.CurrentEffect);
    }
}
