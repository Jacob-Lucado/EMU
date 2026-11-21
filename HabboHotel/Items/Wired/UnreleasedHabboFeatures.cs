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
}

/// <summary>The answer of a <c>GroupInvitationReply</c>.</summary>
public enum GroupInvitationAnswer
{
    Accept = 0,
    Reject = 1,
    DecideLater = 2
}
