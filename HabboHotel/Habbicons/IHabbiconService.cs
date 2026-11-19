using System.Data;
using Plus.HabboHotel.Users;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Habbicons;

[Singleton]
public interface IHabbiconService
{
    HabbiconSnapshot Load(int userId);
    HabbiconChange Change(Habbo habbo, HabbiconAction action, int id);
    HabbiconChange BuyCatalog(Habbo habbo, int id, int credits, int points, int pointsType);
    /// <summary>Gives the user the icon inside the caller's transaction, so it only lands when the caller commits.</summary>
    HabbiconChange Grant(IDbConnection connection, IDbTransaction transaction, int userId, int id);
    bool Use(int userId, int id);
    void ClearUnseen(int userId, IReadOnlyList<int> ids);
}
