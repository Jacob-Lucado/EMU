namespace Plus.HabboHotel.Items.Wired;

/// <summary>
/// Packet ids, wired codes, classnames and limits that Plus invented for features Habbo has shown but not shipped yet.
/// Swap them for Habbo's values when Habbo ships the features. Nothing else in the emulator repeats these values.
/// </summary>
public static class UnreleasedHabboFeatures
{
    public const string PacketRangeFeature = "unreleased_habbo_features";
    public const uint PacketRangeStart = 10300;
    public const uint PacketRangeEnd = 10399;

    // Group invitations.
    public const uint GroupInvitationReplyEvent = 10302;
    public const uint GroupInvitationComposer = 10301;

    // Group wired boxes. Codes count per category, from 9000.
    public const string InviteToGroupAction = "wf_act_invite_to_group";
    public const int InviteToGroupCode = 9000;
    public const string RemoveFromGroupAction = "wf_act_remove_from_group";
    public const int RemoveFromGroupCode = 9001;
    public const string GroupMembershipTrigger = "wf_trg_group_membership";
    public const int GroupMembershipTriggerCode = 9000;
    public const string GroupNotFoundError = "This group does not exist.";
    public const string GroupNotOwnedError = "You can only pick a group you own.";
}

/// <summary>The answer of a <c>GroupInvitationReply</c>.</summary>
public enum GroupInvitationAnswer
{
    Accept = 0,
    Reject = 1,
    DecideLater = 2
}
