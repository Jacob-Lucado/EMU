using Plus.Communication.Packets.Outgoing.Inventory.Pets;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Users;
using Xunit;

namespace Plus.Tests;

public sealed class PetLookTests
{
    [Theory]
    [InlineData(1, "0", "ABABAB", "1 0 ABABAB")]
    [InlineData(21, "0", "ffffff", "21 0 ffffff")]
    [InlineData(34, "8", "ffffff", "34 8 ffffff")]
    public void APetLookHasNoCustomPartsLikeHabbo(int type, string race, string colour, string look) =>
        Assert.Equal(look, Pet(type, race, colour).Look);

    [Fact]
    public void AHorseKeepsItsHairAndSaddleParts()
    {
        Assert.Equal("15 2 ffffff 2 2 -1 1 3 -1 1", Pet(15, "2", "ffffff").Look);
        Assert.Equal("15 2 ffffff 3 2 -1 1 3 -1 1 4 9 0", Pet(15, "2", "ffffff", saddle: 9).Look);
    }

    [Fact]
    public void ADressedPetKeepsItsClothing() =>
        Assert.Equal("26 3 ffffff 5 0 -1 0 1 102 13 3 301 4 4 401 5 2 201 3", Pet(26, "3", "ffffff", clothing: "5 0 -1 0 1 102 13 3 301 4 4 401 5 2 201 3").Look);

    [Fact]
    public void TheInventoryListsAPetWithoutPartsAsZeroParts()
    {
        var (client, packets) = HabbiconTestSupport.Client(new Habbo { Id = 7 });

        client.Send(new PetInventoryComposer(PetAppearanceSnapshots.Inventory([Pet(1, "0", "ABABAB"), Pet(15, "2", "ffffff")])));

        Assert.Equal(HabbiconTestSupport.Incoming(1, 0, 2,
                81, "pixel", 1, 0, "ABABAB", 0, 0, 1, -1,
                81, "pixel", 15, 2, "ffffff", 0, 2, 2, -1, 1, 3, -1, 1, 1, -1).Buffer.ToArray(),
            Assert.Single(packets).Payload);
    }

    [Fact]
    public void ATreatedPetWithoutPartsSendsAZeroCountBeforeItsLevel()
    {
        var packet = new HabbiconTestSupport.RecordingPacket();
        var pet = Pet(16, "0", "FFFFFF");
        new Plus.Communication.Packets.Outgoing.Pets.PetTreatedComposer(2, 7, 81, "pixel", pet.Type, 0, pet.Color, pet.CustomParts, 7, 4).Compose(packet);

        Assert.Equal(new object[] { 2, 7, 81, "pixel", 16, 0, "ffffff", 0, 0, 7, 4 }, packet.Writes);
    }

    private static Pet Pet(int type, string race, string colour, int saddle = 0, string clothing = "-1") =>
        new(81, 7, 0, "pixel", type, race, colour, 0, 100, 100, 0, DateTimeOffset.FromUnixTimeMilliseconds(1609459200123),
            0, 0, 0, saddle, 0, 0, -1, clothing, "owner");
}
