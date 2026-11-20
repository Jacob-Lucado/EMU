using Plus.Utilities;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Plus.HabboHotel.Rooms;

public sealed record RoomUserStatusSnapshot(int VirtualId, int X, int Y, string Z, int HeadRotation,
    int BodyRotation, string Status)
{
    public static ImmutableArray<RoomUserStatusSnapshot> Capture(IEnumerable<RoomUser> users) =>
        users.Select(user => Capture(user)).ToImmutableArray();

    internal static RoomUserStatusSnapshot Capture(RoomUser user, string? gesture = null)
    {
        var statusText = new StringBuilder("/");

        foreach (var status in user.Statusses.ToList()) {
            statusText.Append(status.Key);

            if (!string.IsNullOrEmpty(status.Value)) {
                statusText.Append(' ').Append(status.Value);
            }

            statusText.Append('/');
        }

        if (gesture != null) {
            statusText.Append("gst ").Append(gesture).Append('/');
        }

        statusText.Append('/');

        return new(user.VirtualId, user.X, user.Y, TextHandling.GetString(user.Z),
            user.RotHead, user.RotBody, statusText.ToString());
    }
}
