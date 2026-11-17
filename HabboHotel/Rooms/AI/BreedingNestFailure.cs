namespace Plus.HabboHotel.Rooms.AI;

/// <summary>Why a pet cannot go to a breeding box, as the reason ids of Habbo's GoToBreedingNestFailure message.</summary>
public enum BreedingNestFailure
{
    NoNest = 0,
    NoSuitableNest = 1,
    NoPath = 5
}
