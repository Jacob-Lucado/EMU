using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Plus.Communication.Packets;
using Plus.Communication.Packets.Incoming;
using Plus.Communication.Packets.Incoming.Groups;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Authentication;
using Xunit;

namespace Plus.Tests;

/// <summary>Group invitations: the invitation packet, and accept, reject and decide later through the raw reply packet.</summary>
public class GroupInvitationTests
{
    private const int GroupId = 9;

    [Fact]
    public void InvitationPacketIdsComeFromTheUnreleasedFeatureTable()
    {
        Assert.Equal(10302u, UnreleasedHabboFeatures.GroupInvitationReplyEvent);
        Assert.Equal(10301u, UnreleasedHabboFeatures.GroupInvitationComposer);
        Assert.Equal(UnreleasedHabboFeatures.GroupInvitationReplyEvent, ClientPacketHeader.GroupInvitationReplyEvent);
        Assert.Equal(UnreleasedHabboFeatures.GroupInvitationComposer, ServerPacketHeader.GroupInvitationComposer);
        Assert.NotNull(CustomPacketRanges.Find(PacketRangeDirection.ClientToServer, ClientPacketHeader.GroupInvitationReplyEvent));
        Assert.NotNull(CustomPacketRanges.Find(PacketRangeDirection.ServerToClient, ServerPacketHeader.GroupInvitationComposer));

        using var manager = new PacketManager([new GroupInvitationReplyEvent(new Hotel(GroupType.Open).Invitations)], NullLogger<PacketManager>.Instance);
        Assert.True(manager.IsRegistered(10302));
    }

    [Fact]
    public void InviteStoresTheInvitationAndSendsTheGroupToTheOnlineUser()
    {
        var hotel = new Hotel(GroupType.Locked);
        var (invited, sent) = hotel.Online(8);

        Assert.True(hotel.Invitations.Invite(hotel.Group, 8, 7, 42));

        var stored = Assert.Single(hotel.Store.Rows.Values);
        Assert.Equal((GroupId, 8, 7, 42u), (stored.GroupId, stored.UserId, stored.InviterId, stored.RoomId));
        var (header, payload) = Assert.Single(sent);
        Assert.Equal(10301u, header);
        var reader = new PayloadReader(payload);
        Assert.Equal(GroupId, reader.Int());
        Assert.Equal("Crew", reader.String());
        Assert.Equal("b01014s02024", reader.String());
        Assert.Equal(42, reader.Int());
        Assert.True(reader.AtEnd);
        Assert.NotNull(invited);
    }

    [Fact]
    public void RepeatedInviteToTheSameClientSendsOnlyOnePrompt()
    {
        var hotel = new Hotel(GroupType.Private);
        var (_, sent) = hotel.Online(8);

        Assert.True(hotel.Invitations.Invite(hotel.Group, 8, 7, 42));
        Assert.False(hotel.Invitations.Invite(hotel.Group, 8, 7, 42));
        Assert.Single(sent);
        Assert.Single(hotel.Store.Rows);
    }

    [Fact]
    public void InviteRefreshesAPendingInvitationAndSkipsMembers()
    {
        var hotel = new Hotel(GroupType.Open);

        Assert.True(hotel.Invitations.Invite(hotel.Group, 8, 7, 42));
        Assert.True(hotel.Invitations.Invite(hotel.Group, 8, 5, 43));
        var stored = Assert.Single(hotel.Store.Rows.Values);
        Assert.Equal((5, 43u), (stored.InviterId, stored.RoomId));

        hotel.Group.PublishJoin(6);
        var (_, memberSent) = hotel.Online(6);
        Assert.False(hotel.Invitations.Invite(hotel.Group, 6, 7, 42));
        Assert.False(hotel.Store.Rows.ContainsKey((GroupId, 6)));
        Assert.Empty(memberSent);
    }

    [Theory]
    [InlineData(GroupType.Open)]
    [InlineData(GroupType.Locked)]
    [InlineData(GroupType.Private)]
    public async Task AcceptMakesTheInvitedUserAMemberWithTheSamePacketsAsANormalJoin(GroupType type)
    {
        var hotel = new Hotel(type);
        hotel.Invitations.Invite(hotel.Group, 8, 7, 42);
        var (client, sent) = hotel.Online(8);
        sent.Clear();

        await hotel.Reply(client, GroupId, 0);

        Assert.True(hotel.Group.IsMember(8));
        Assert.False(hotel.Group.HasRequest(8));
        Assert.Empty(hotel.Store.Rows);
        Assert.Equal(new object[] { 8, GroupId, 1500 }, Assert.Single(hotel.Store.Accepts));
        Assert.Equal(await NormalOpenJoinHeaders(), sent.Select(packet => packet.Header));
    }

    [Fact]
    public async Task AcceptAlsoSettlesAPendingRequestOfTheInvitedUser()
    {
        var hotel = new Hotel(GroupType.Locked);
        hotel.Group.PublishJoin(8);
        Assert.True(hotel.Group.HasRequest(8));
        hotel.Invitations.Invite(hotel.Group, 8, 7, 42);
        var (client, _) = hotel.Online(8);

        await hotel.Reply(client, GroupId, 0);

        Assert.True(hotel.Group.IsMember(8));
        Assert.False(hotel.Group.HasRequest(8));
    }

    [Fact]
    public async Task AcceptWithoutAPendingInvitationChangesNothing()
    {
        var hotel = new Hotel(GroupType.Private);
        var (client, sent) = hotel.Online(8);

        await hotel.Reply(client, GroupId, 0);
        await hotel.Reply(client, 77, 0);

        Assert.False(hotel.Group.IsMember(8));
        Assert.Empty(sent);
    }

    [Fact]
    public async Task AcceptAtTheMembershipLimitSendsTheNoticeAndKeepsTheInvitation()
    {
        var hotel = new Hotel(GroupType.Open) { AcceptOutcome = GroupJoinOutcome.LimitReached };
        hotel.Invitations.Invite(hotel.Group, 8, 7, 42);
        var (client, sent) = hotel.Online(8);
        sent.Clear();

        await hotel.Reply(client, GroupId, 0);

        Assert.False(hotel.Group.IsMember(8));
        Assert.Single(hotel.Store.Rows);
        Assert.Equal(new[] { ServerPacketHeader.BroadcastMessageAlertComposer }, sent.Select(packet => packet.Header));
    }

    [Fact]
    public async Task RejectDeletesTheInvitationAndSendsNothing()
    {
        var hotel = new Hotel(GroupType.Open);
        hotel.Invitations.Invite(hotel.Group, 8, 7, 42);
        var (client, sent) = hotel.Online(8);
        sent.Clear();

        await hotel.Reply(client, GroupId, 1);

        Assert.Empty(hotel.Store.Rows);
        Assert.False(hotel.Group.IsMember(8));
        Assert.Empty(sent);

        await hotel.Reply(client, GroupId, 0);
        Assert.False(hotel.Group.IsMember(8));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(-1)]
    public async Task DecideLaterOrAnUnknownAnswerKeepsTheInvitation(int answer)
    {
        var hotel = new Hotel(GroupType.Open);
        hotel.Invitations.Invite(hotel.Group, 8, 7, 42);
        var (client, sent) = hotel.Online(8);
        sent.Clear();

        await hotel.Reply(client, GroupId, answer);

        Assert.Single(hotel.Store.Rows);
        Assert.False(hotel.Group.IsMember(8));
        Assert.Empty(sent);

        await hotel.Reply(client, GroupId, 0);
        Assert.True(hotel.Group.IsMember(8));
    }

    [Fact]
    public void CancelDeletesOnlyThatUsersInvitation()
    {
        var hotel = new Hotel(GroupType.Open);
        hotel.Invitations.Invite(hotel.Group, 8, 7, 42);
        hotel.Invitations.Invite(hotel.Group, 6, 7, 42);

        Assert.True(hotel.Invitations.Cancel(GroupId, 8));
        Assert.False(hotel.Invitations.Cancel(GroupId, 8));

        Assert.Equal((GroupId, 6), Assert.Single(hotel.Store.Rows.Keys));
    }

    // The packets an open group's normal join sends, so an accepted invitation can be compared with them.
    private static async Task<IEnumerable<uint>> NormalOpenJoinHeaders()
    {
        var hotel = new Hotel(GroupType.Open);
        var (client, sent) = hotel.Online(8);
        await hotel.Participation.Join(client, GroupId);
        Assert.True(hotel.Group.IsMember(8));

        return sent.Select(packet => packet.Header).ToArray();
    }

    private sealed class Hotel
    {
        private readonly List<GameClient> _clients = new();

        public Hotel(GroupType type)
        {
            Group = new Group(GroupId, "Crew", "desc", "b01014s02024", 42, 7, DateTimeOffset.FromUnixTimeSeconds(1_700_000_000), (int)type, 3, 4, 0,
                false, GroupMembershipSnapshot.Empty);
            Store = new FakeInvitationStore(this);
            var manager = CatalogSnapshotTestSupport.Proxy<IGroupManager>((method, args) => method switch
            {
                "TryGetGroup" => Lookup(args),
                "GetGroupsForUser" => new List<Group>(),
                "GetColourCode" => "",
                _ => throw new NotSupportedException(method),
            });
            var snapshots = CatalogSnapshotTestSupport.Proxy<IGroupInfoSnapshotService>((_, args) =>
            {
                var group = (Group)args[0]!;

                return new GroupInfoSnapshot(group.Id, group.Type, group.Name, group.Description, group.Badge, group.RoomId, "HQ",
                    group.MemberCount, "1-1-2023", "Owner", false, false, false, false, group.RequestCount, false, group.ForumEnabled);
            });
            var clients = CatalogSnapshotTestSupport.Proxy<IGameClientManager>((method, args) => method switch
            {
                "get_GetClients" => _clients,
                "GetClientByUserId" => _clients.FirstOrDefault(client => client.GetHabbo().Id == (int)args[0]!),
                _ => throw new NotSupportedException(method),
            });
            Participation = new GroupParticipationService(manager, snapshots, clients, new JoinStore(), new AccountSessionGate(), Store);
            Invitations = new GroupInvitationService(Store, Participation, clients, TimeProvider.System);
        }

        public Group Group { get; }
        public FakeInvitationStore Store { get; }
        public GroupParticipationService Participation { get; }
        public GroupInvitationService Invitations { get; }
        public GroupJoinOutcome AcceptOutcome { get; init; } = GroupJoinOutcome.Inserted;

        public (GameClient Client, List<(uint Header, byte[] Payload)> Sent) Online(int userId)
        {
            var (client, sent) = HabbiconTestSupport.Client(new Habbo { Id = userId, Username = "user" + userId, Look = "hr-1" });
            _clients.Add(client);

            return (client, sent);
        }

        public Task Reply(GameClient client, int groupId, int answer) =>
            new GroupInvitationReplyEvent(Invitations).Parse(client, HabbiconTestSupport.Incoming(groupId, answer));

        private bool Lookup(object?[] args)
        {
            var found = (int)args[0]! == Group.Id;
            args[1] = found ? Group : null;

            return found;
        }
    }

    private sealed class FakeInvitationStore(Hotel hotel) : IGroupInvitationStore
    {
        public Dictionary<(int GroupId, int UserId), GroupInvitation> Rows { get; } = new();
        public List<object[]> Accepts { get; } = new();

        public void Save(GroupInvitation invitation) => Rows[(invitation.GroupId, invitation.UserId)] = invitation;

        public GroupInvitation? Find(int groupId, int userId) => Rows.GetValueOrDefault((groupId, userId));

        public bool Delete(int groupId, int userId) => Rows.Remove((groupId, userId));

        public GroupJoinOutcome Accept(int userId, int groupId, int membershipLimit)
        {
            Accepts.Add(new object[] { userId, groupId, membershipLimit });

            if (!Rows.ContainsKey((groupId, userId))) {
                return GroupJoinOutcome.Refused;
            }

            if (hotel.AcceptOutcome == GroupJoinOutcome.Inserted) {
                Rows.Remove((groupId, userId));
            }

            return hotel.AcceptOutcome;
        }
    }

    private sealed class JoinStore : IGroupParticipationStore
    {
        public GroupJoinOutcome Join(int userId, int groupId, bool request, int membershipLimit) => GroupJoinOutcome.Inserted;

        public bool SaveFavourite(int userId, int groupId) => true;
    }

    private sealed class PayloadReader(byte[] payload)
    {
        private int _at;

        public bool AtEnd => _at == payload.Length;

        public int Int()
        {
            var value = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(_at, 4));
            _at += 4;

            return value;
        }

        public string String()
        {
            var length = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(_at, 2));
            var value = Encoding.UTF8.GetString(payload, _at + 2, length);
            _at += 2 + length;

            return value;
        }
    }
}
