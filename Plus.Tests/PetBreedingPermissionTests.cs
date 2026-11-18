using Plus.Communication.Packets.Outgoing;
using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.Types;
using Xunit;

namespace Plus.Tests;

// The owner can allow other users to breed with a pet. The room gets a pet status update, and the pet's info and
// room entry carry the flag.
public partial class PlacedFurniRoomTests
{
    [Fact]
    public void TheOwnerAllowsBreedingAndTheRoomGetsAStatusUpdate()
    {
        LegacyRider();
        var petUser = LegacyHorse(2, 1);
        var pet = petUser.PetData!;
        var store = new RecordingBreedingStore();
        _client.Packets.Clear();

        new PetBreedingPermissionService(store).Toggle(_room, _client, pet.PetId);

        Assert.True(pet.AllowBreeding);
        Assert.Equal([(pet.PetId, true)], store.Saved);
        var status = Assert.Single(_client.Packets, packet => packet.Header == ServerPacketHeader.PetStatusUpdateComposer);
        Assert.Equal(StatusBody(petUser.VirtualId, pet.PetId, true), status.Body);
    }

    [Fact]
    public void TogglingAGrownPlantUpdatesItsOwnPermissionAndKeepsItsBreedingState()
    {
        LegacyRider();
        var user = LegacyHorse(2, 1);
        var pet = user.PetData!;
        var now = DateTimeOffset.UtcNow;
        pet.Plant = new(pet.PetId, 1, now.AddDays(-3), now.AddDays(1), true, false);
        var store = new RecordingBreedingStore();
        _client.Packets.Clear();

        new PetBreedingPermissionService(store).Toggle(_room, _client, pet.PetId);

        Assert.True(pet.Plant.BreedingPermission);
        var status = Assert.Single(_client.Packets, packet => packet.Header == ServerPacketHeader.PetStatusUpdateComposer);
        Assert.Equal(1, status.Body[8]);
        Assert.Equal(1, status.Body[11]);
    }

    [Fact]
    public void TogglingAgainTakesThePermissionBack()
    {
        LegacyRider();
        var petUser = LegacyHorse(2, 1);
        var pet = petUser.PetData!;
        var store = new RecordingBreedingStore();
        var service = new PetBreedingPermissionService(store);
        service.Toggle(_room, _client, pet.PetId);
        _client.Packets.Clear();

        service.Toggle(_room, _client, pet.PetId);

        Assert.False(pet.AllowBreeding);
        Assert.Equal((pet.PetId, false), store.Saved[^1]);
        var status = Assert.Single(_client.Packets, packet => packet.Header == ServerPacketHeader.PetStatusUpdateComposer);
        Assert.Equal(StatusBody(petUser.VirtualId, pet.PetId, false), status.Body);
    }

    [Fact]
    public void OnlyTheOwnerCanChangeBreedingPermission()
    {
        LegacyRider();
        var pet = LegacyHorse(2, 1).PetData!;
        pet.OwnerId = 99;
        var store = new RecordingBreedingStore();
        _client.Packets.Clear();

        new PetBreedingPermissionService(store).Toggle(_room, _client, pet.PetId);

        Assert.False(pet.AllowBreeding);
        Assert.Empty(store.Saved);
        Assert.DoesNotContain(_client.Packets, packet => packet.Header == ServerPacketHeader.PetStatusUpdateComposer);
    }

    [Fact]
    public void ThePermissionIsNotChangedWhenItCannotBeSaved()
    {
        LegacyRider();
        var pet = LegacyHorse(2, 1).PetData!;
        _client.Packets.Clear();

        Assert.Throws<InvalidOperationException>(() => new PetBreedingPermissionService(new RecordingBreedingStore { Fail = true }).Toggle(_room, _client, pet.PetId));

        Assert.False(pet.AllowBreeding);
        Assert.DoesNotContain(_client.Packets, packet => packet.Header == ServerPacketHeader.PetStatusUpdateComposer);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PetInfoEndsWithTheBreedingPermission(bool allowed)
    {
        var pet = LegacyHorse(2, 1).PetData!;
        pet.AllowBreeding = allowed;
        var packet = new HabbiconTestSupport.RecordingPacket();

        new PetInformationComposer(new PetInformationService(TimeProvider.System).Capture(pet)).Compose(packet);

        Assert.Equal(allowed, packet.Writes[^1]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThePetsRoomEntryCarriesTheBreedingPermission(bool allowed)
    {
        var petUser = LegacyHorse(2, 1);
        petUser.PetData!.AllowBreeding = allowed;
        var ai = new PetBot(petUser.VirtualId, new RecordingLocale(), new RecordingCommands(-1));
        ai.Init(50, petUser.VirtualId, RoomId, petUser, _room);
        petUser.BotAi = ai;
        var snapshot = new TestRoomUserSnapshots().Capture(petUser)!;
        var packet = new HabbiconTestSupport.RecordingPacket();

        new UsersComposer([snapshot]).Compose(packet);

        // Type, owner, owner name, rarity, saddle, riding, can breed, can harvest, can revive, breeding permission.
        var petFields = packet.Writes.SkipWhile(write => !Equals(write, petUser.PetData.Type)).ToArray();
        Assert.Equal(allowed, petFields[9]);
    }

    private static byte[] StatusBody(int virtualId, int petId, bool allowed)
    {
        var body = new byte[12];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(body, virtualId);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(body.AsSpan(4), petId);
        body[11] = allowed ? (byte)1 : (byte)0;

        return body;
    }

    private sealed class RecordingBreedingStore : IPetBreedingPermissionStore
    {
        public bool Fail { get; init; }
        public List<(int PetId, bool Allowed)> Saved { get; } = [];

        public void Save(Pet pet, bool allowed)
        {
            if (Fail) {
                throw new InvalidOperationException("The breeding permission was not persisted.");
            }

            Saved.Add((pet.PetId, allowed));
        }
    }
}
