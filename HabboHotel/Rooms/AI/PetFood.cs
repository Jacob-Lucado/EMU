using System.Globalization;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms.AI;

/// <summary>
/// Pet food furni: a classname starting with "petfood" or the "pet_food" furniline. Its customparams read
/// "energy,experience,happiness" and an optional list of the pet types that eat it, as in "375,15,0 8,26,30".
/// </summary>
public sealed record PetFood(int Energy, int Experience, int Happiness, IReadOnlyList<int> PetTypes)
{
    public static PetFood? Of(ItemDefinition definition)
    {
        throw new NotImplementedException();
    }

    public bool Feeds(int petType) => throw new NotImplementedException();
}
