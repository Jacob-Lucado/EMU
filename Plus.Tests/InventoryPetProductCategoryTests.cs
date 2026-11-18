using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Xunit;

namespace Plus.Tests;

public sealed class InventoryPetProductCategoryTests
{
    [Theory]
    [InlineData(InteractionType.HorseBodyDye, FurniCategory.PetShampoo)]
    [InlineData(InteractionType.HorseHairstyle, FurniCategory.PetCustomPart)]
    [InlineData(InteractionType.HorseHairDye, FurniCategory.PetCustomPartShampoo)]
    [InlineData(InteractionType.HorseSaddle1, FurniCategory.PetSaddle)]
    [InlineData(InteractionType.HorseSaddle2, FurniCategory.PetSaddle)]
    public void HorseProductsReachTheInventoryAsPetProducts(InteractionType product, FurniCategory category)
    {
        var item = new InventoryItem { Id = 5, Definition = new ItemDefinition { Type = ItemType.Floor, InteractionType = product } };

        Assert.Equal(category, InventoryItemSnapshot.Capture(item).Category);
    }
}
