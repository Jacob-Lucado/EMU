namespace Plus.HabboHotel.Permissions;

internal static class AccessMutationPolicy
{
    public static bool CanEdit(int actorId, int targetId, UserAccess actor, UserAccess target) =>
        actorId != targetId && actor.Can(PermissionKeys.HousekeepingRolesManage) && actor.Outranks(target);

    public static bool CanAssign(int actorId, int targetId, UserAccess actor, UserAccess target, AccessRole role, IEnumerable<string> registry) =>
        CanEdit(actorId, targetId, actor, target) && role.Weight <= actor.Weight && role.SecurityLevel <= actor.SecurityLevel &&
        (!role.IsStaff || actor.Roles.Any(held => held.IsStaff)) &&
        role.Limits.All(limit => limit.Value <= actor.Limit(limit.Key, AccessLimits.Defaults.GetValueOrDefault(limit.Key))) &&
        registry.Where(key => role.Permissions.Any(pattern => UserAccess.Matches(pattern, key))).All(actor.Can);

    // A role of the actor's own weight comes back off a user only when the actor gave it and outranks the user without it.
    public static bool CanRevoke(int actorId, int targetId, UserAccess actor, UserAccess target, UserAccess targetWithoutRole, AccessRole role, bool grantedByActor) =>
        role.Weight <= actor.Weight &&
        (CanEdit(actorId, targetId, actor, target) || grantedByActor && CanEdit(actorId, targetId, actor, targetWithoutRole));

    public static bool CanGrant(UserAccess actor, string pattern, IEnumerable<string> registry)
    {
        var keys = registry.Where(key => UserAccess.Matches(pattern, key)).ToArray();

        return keys.Length > 0 && keys.All(actor.Can);
    }
}
