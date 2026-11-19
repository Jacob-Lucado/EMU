using Plus.Utilities;
using System.Globalization;
using Dapper;
using Plus.Database;
using Plus.HabboHotel.Badges.Rarity;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Rooms.AI;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Rooms;

public sealed record RoomUserSnapshot(int Id, string Name, string Motto, string Look, int VirtualId, int X, int Y,
    string Z, int Rotation, int Kind, string Gender, int GroupId, string GroupName, int AchievementPoints,
    bool IsPet, int PetType, int OwnerId, string OwnerName, bool HasSaddle, bool IsRidden, int PetLevel = 0, IReadOnlyList<short>? BotSkills = null,
    int BadgesRank = -1, bool HasBreedingPermission = false, RoomUserPlantState? Plant = null);

/// <summary>The monster plant part of a pet's Users entry.</summary>
public sealed record RoomUserPlantState(int Rarity, bool CanBreed, bool CanRevive, bool BreedingPermission, string Posture);

[Scoped]
public interface IRoomUserSnapshotService
{
    RoomUserSnapshot? Capture(RoomUser user);
    IReadOnlyList<RoomUserSnapshot> Capture(IEnumerable<RoomUser> users);
}

public sealed class RoomUserSnapshotService(IGroupManager groups, IGameClientManager clients, ICacheManager cache,
    IDatabase database, IBadgeRarityManager badgeRarity, TimeProvider? clock = null) : IRoomUserSnapshotService
{
    /// <summary>A generic bot's Users entry lists skills 1-5 and 19-23.</summary>
    private static readonly short[] GenericBotSkills = [1, 2, 3, 4, 5, 19, 20, 21, 22, 23];

    public IReadOnlyList<RoomUserSnapshot> Capture(IEnumerable<RoomUser> users) =>
        users.Select(Capture).Where(snapshot => snapshot != null).Select(snapshot => snapshot!).ToArray();

    public RoomUserSnapshot? Capture(RoomUser user)
    {
        if (user.Stress is { } stress) {
            return new(user.HabboId, stress.Name, "", stress.Look, user.VirtualId, user.X, user.Y,
                TextHandling.GetString(user.Z), user.RotBody, 1, stress.Gender.ToLowerInvariant(), 0, "", 0, false, 0, 0, "", false, false);
        }

        if (!user.IsBot) {
            var habbo = clients.GetClientByUserId(user.HabboId)?.GetHabbo();

            if (habbo == null) {
                return null;
            }

            Group? group = null;
            var favouriteGroupId = habbo.HabboStats?.FavouriteGroupId ?? 0;

            if (favouriteGroupId > 0) {
                groups.TryGetGroup(favouriteGroupId, out group);
            }

            return new(habbo.Id, habbo.Username, habbo.Motto, user.LookFor(habbo), user.VirtualId, user.X, user.Y,
                TextHandling.GetString(user.Z), user.RotBody, 1, habbo.Gender.ToLowerInvariant(),
                group?.Id ?? 0, group?.Name ?? "", habbo.HabboStats?.AchievementPoints ?? 0,
                false, 0, 0, "", false, false, BadgesRank: badgeRarity.Snapshot.BadgesRank(habbo.Id));
        }

        var kind = user.BotData.AiType == BotAiType.Pet ? 2 : 4;
        // A placed bot always has its AI, and a placed pet its pet data.
        var ai = user.BotAi ?? throw new InvalidOperationException("A placed bot has no AI.");

        if (user.IsPet) {
            var pet = user.PetData ?? throw new InvalidOperationException("A placed pet has no pet data.");

            var now = (clock ?? TimeProvider.System).GetUtcNow();
            var plant = pet.Plant is { } p
                ? new RoomUserPlantState(p.Rarity, p.CanBreedNow(now), p.IsDead(now), p.BreedingPermission, p.Posture(now))
                : null;

            return new(ai.BaseId, user.BotData.Name, user.BotData.Motto, pet.Look.ToLowerInvariant(),
                user.VirtualId, user.X, user.Y, TextHandling.GetString(user.Z), 0, kind, "", 0, "", 0,
                true, pet.Type, pet.OwnerId, pet.OwnerName, pet.Saddle > 0,
                user.RidingHorse, pet.Plant?.Level(now) ?? pet.Level, HasBreedingPermission: pet.AllowBreeding, Plant: plant);
        }

        // A bot's entry carries the direction it faces.
        return new(ai.BaseId, user.BotData.Name, user.BotData.Motto, user.BotData.Look.ToLowerInvariant(),
            user.VirtualId, user.X, user.Y, TextHandling.GetString(user.Z), user.RotBody, kind,
            user.BotData.Gender.ToLowerInvariant(), 0, "", 0, false, 0, user.BotData.OwnerId,
            ResolveUsername(user.BotData.OwnerId), false, false, BotSkills: user.BotData.AiType == BotAiType.Generic ? GenericBotSkills : null);
    }

    private string ResolveUsername(int userId)
    {
        var online = clients.GetClientByUserId(userId)?.GetHabbo();

        if (online != null) {
            return online.Username;
        }

        var cached = cache.GenerateUser(userId);

        if (cached != null) {
            return cached.Username;
        }

        using var connection = database.Connection();
        var username = connection.QuerySingleOrDefault<string>("SELECT username FROM users WHERE id = @userId LIMIT 1", new { userId });

        return string.IsNullOrEmpty(username) ? "Unknown User" : username;
    }
}
