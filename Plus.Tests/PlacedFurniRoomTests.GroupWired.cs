using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Plus.Communication.Packets.Incoming.Groups;
using Plus.Communication.Packets.Incoming.Rooms.Furni.Wired;
using Plus.Communication.Packets.Outgoing;
using Plus.Core.Settings;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Items.Wired.Configuration;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Authentication;
using Xunit;

namespace Plus.Tests;

// The group wired boxes, saved through the editor's raw save packets and run by the real room cycle: Invite to Group, Remove from
// Group and the Group Membership trigger, which fires on every way a user joins or leaves a group.
public partial class PlacedFurniRoomTests
{
    private const uint GroupSpeech = 951, GroupBoxId = 952, GroupMessage = 953, WiredLeaveSpeech = 954, WiredLeaveBox = 955;
    private const int OwnGroup = 9, OtherGroup = 10;

    [Fact]
    public void GroupBoxesUseTheCodesOfTheUnreleasedFeatureTable()
    {
        Assert.Equal((9000, 9001, 9000), (UnreleasedHabboFeatures.InviteToGroupCode, UnreleasedHabboFeatures.RemoveFromGroupCode,
            UnreleasedHabboFeatures.GroupMembershipTriggerCode));

        foreach (var (name, category, code, defaults) in new[]
                 {
                     ("wf_act_invite_to_group", WiredBoxCategory.Action, 9000, new[] { 0 }),
                     ("wf_act_remove_from_group", WiredBoxCategory.Action, 9001, new[] { 0, 1 }),
                     ("wf_trg_group_membership", WiredBoxCategory.Trigger, 9000, new[] { 0, 0, 0 })
                 }) {
            Assert.True(WiredBoxRegistry.TryGet(name, out var descriptor), name);
            Assert.Equal((category, code), (descriptor.Category, descriptor.EditorCode));
            Assert.True(WiredNativeEditorProjection.Supports(name));
            Assert.Equal(defaults, WiredNativeEditorProjection.DefaultNative(descriptor).OwnedIntParams.ToArray());
            Assert.NotNull(WiredNativeEditorProjection.DefaultRuntime(1, descriptor));
        }
    }

    [Theory]
    [InlineData("wf_act_invite_to_group", OwnGroup, "saved")]
    [InlineData("wf_act_invite_to_group", 0, "saved")]
    [InlineData("wf_act_invite_to_group", OtherGroup, UnreleasedHabboFeatures.GroupNotOwnedError)]
    [InlineData("wf_act_invite_to_group", 77, UnreleasedHabboFeatures.GroupNotFoundError)]
    [InlineData("wf_act_remove_from_group", OwnGroup, "saved")]
    [InlineData("wf_act_remove_from_group", OtherGroup, UnreleasedHabboFeatures.GroupNotOwnedError)]
    [InlineData("wf_act_remove_from_group", 77, UnreleasedHabboFeatures.GroupNotFoundError)]
    public async Task AGroupEffectSavesOnlyAGroupTheEditorOwns(string box, int groupId, string answer)
    {
        GroupHotel();

        Assert.Equal(answer, await SaveGroupEffect(box, groupId));
    }

    [Theory]
    [InlineData(1, OtherGroup, "saved")]
    [InlineData(1, OwnGroup, "saved")]
    [InlineData(0, 0, "saved")]
    [InlineData(1, 77, UnreleasedHabboFeatures.GroupNotFoundError)]
    public async Task TheMembershipTriggerSavesAnyExistingGroup(int mode, int groupId, string answer)
    {
        GroupHotel();

        Assert.Equal(answer, await SaveMembershipTrigger(mode, groupId, 0));
    }

    [Fact]
    public async Task SavedGroupBoxesLoadBackFromTheirStoredRows()
    {
        GroupHotel();
        await SaveGroupEffect("wf_act_remove_from_group", OwnGroup, cancel: 0);
        var stored = (IWiredConfiguredItem)GetBox(GroupBoxId);

        var row = WiredConfigurationStore.DecodeRow(GroupBoxId, "wf_act_remove_from_group", JsonSerializer.Serialize(stored.Configuration.Origin!.Native));

        Assert.Equal(new[] { OwnGroup, 0 }, Assert.IsType<WiredConfiguration>(row).IntParams.ToArray());
    }

    [Fact]
    public async Task InviteToGroupInvitesTheTriggeringUserAndShowsTheInvitation()
    {
        var hotel = GroupHotel();
        await SaveGroupEffect("wf_act_invite_to_group", OwnGroup);

        Say(hotel.Visitor, "go");

        var invitation = Assert.Single(hotel.VisitorClient.Packets, packet => packet.Header == ServerPacketHeader.GroupInvitationComposer);
        var reader = new GroupWireReader(invitation.Body);
        Assert.Equal((OwnGroup, "Crew", "b01014s02024", (int)RoomId), (reader.Int(), reader.String(), reader.String(), reader.Int()));
        var stored = Assert.Single(hotel.Invitations.Values);
        Assert.Equal((OwnGroup, 8, 7, RoomId), (stored.GroupId, stored.UserId, stored.InviterId, stored.RoomId));
    }

    [Fact]
    public async Task InviteToGroupSkipsAMemberAndAnInactiveBox()
    {
        var hotel = GroupHotel();
        hotel.Groups[OwnGroup].PublishJoin(8);
        await SaveGroupEffect("wf_act_invite_to_group", OwnGroup);

        Say(hotel.Visitor, "go");
        hotel.Groups[OwnGroup].DeleteMember(8);
        Assert.Equal("saved", await SaveGroupEffectOn(GroupBoxId, "wf_act_invite_to_group", 0, 1));
        Say(hotel.Visitor, "go");

        Assert.DoesNotContain(hotel.VisitorClient.Packets, packet => packet.Header == ServerPacketHeader.GroupInvitationComposer);
        Assert.Empty(hotel.Invitations);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(0, true)]
    public async Task RemoveFromGroupMakesTheMemberLeaveAndCancelsTheInvitationWhenAsked(int cancel, bool invitationKept)
    {
        var hotel = GroupHotel();
        hotel.Groups[OwnGroup].PublishJoin(8);
        hotel.Invitations[(OwnGroup, 8)] = new(OwnGroup, 8, 7, RoomId, DateTimeOffset.UnixEpoch);
        await SaveGroupEffect("wf_act_remove_from_group", OwnGroup, cancel);

        Say(hotel.Visitor, "go");

        Assert.False(hotel.Groups[OwnGroup].IsMember(8));
        Assert.Equal(new object[] { OwnGroup, 8 }, Assert.Single(hotel.Removals));
        Assert.Contains(hotel.VisitorClient.Packets, packet => packet.Header == ServerPacketHeader.GroupInfoComposer);
        Assert.Equal(invitationKept, hotel.Invitations.ContainsKey((OwnGroup, 8)));
    }

    [Fact]
    public async Task RemoveFromGroupNeverRemovesTheOwnerButStillCancelsAnInvitationOfANonMember()
    {
        var hotel = GroupHotel();
        hotel.Invitations[(OwnGroup, 8)] = new(OwnGroup, 8, 7, RoomId, DateTimeOffset.UnixEpoch);
        await SaveGroupEffect("wf_act_remove_from_group", OwnGroup);

        Say(hotel.Owner, "go");
        Say(hotel.Visitor, "go");

        Assert.Empty(hotel.Removals);
        Assert.False(hotel.Invitations.ContainsKey((OwnGroup, 8)));
    }

    [Theory]
    [InlineData(1, OwnGroup, 0, true)]
    [InlineData(1, OtherGroup, 0, false)]
    [InlineData(1, OwnGroup, 1, false)]
    [InlineData(0, 0, 0, true)]
    public async Task TheMembershipTriggerFiresWhenTheUserJoinsTheMatchingGroup(int mode, int groupId, int change, bool fires)
    {
        var hotel = GroupHotel();
        _room.Group = hotel.Groups[OwnGroup];
        await SaveMembershipTrigger(mode, groupId, change);

        await new JoinGroupEvent(hotel.Participation).Parse(hotel.VisitorClient, ClientPacket(OwnGroup));
        RunGroupCycle();

        Assert.True(hotel.Groups[OwnGroup].IsMember(8));
        Assert.Equal(fires, GroupMessages(hotel.VisitorClient).Contains("changed"));
    }

    [Fact]
    public async Task TheMembershipTriggerFiresOnAnAcceptedRequestAndAnAcceptedInvitation()
    {
        var hotel = GroupHotel(GroupType.Locked);
        await SaveMembershipTrigger(1, OwnGroup, 0);

        await new JoinGroupEvent(hotel.Participation).Parse(hotel.VisitorClient, ClientPacket(OwnGroup));
        RunGroupCycle();
        Assert.DoesNotContain("changed", GroupMessages(hotel.VisitorClient));

        await new AcceptGroupMembershipEvent(hotel.Mutation).Parse(_client, ClientPacket(OwnGroup, 8));
        RunGroupCycle();
        Assert.Single(GroupMessages(hotel.VisitorClient), "changed");

        hotel.Groups[OwnGroup].DeleteMember(8);
        hotel.VisitorClient.Packets.Clear();
        hotel.Invitations[(OwnGroup, 8)] = new(OwnGroup, 8, 7, RoomId, DateTimeOffset.UnixEpoch);
        await new GroupInvitationReplyEvent(hotel.InvitationService).Parse(hotel.VisitorClient, ClientPacket(OwnGroup, 0));
        RunGroupCycle();
        Assert.True(hotel.Groups[OwnGroup].IsMember(8));
        Assert.Single(GroupMessages(hotel.VisitorClient), "changed");
    }

    [Fact]
    public async Task TheMembershipTriggerFiresWhenTheUserLeavesIsKickedOrIsRemovedByWired()
    {
        var hotel = GroupHotel();
        await SaveMembershipTrigger(1, OwnGroup, 1);

        hotel.Groups[OwnGroup].PublishJoin(8);
        await new RemoveGroupMemberEvent(hotel.Removal).Parse(hotel.VisitorClient, ClientPacket(OwnGroup, 8));
        RunGroupCycle();
        Assert.Single(GroupMessages(hotel.VisitorClient), "changed");

        hotel.Groups[OwnGroup].PublishJoin(8);
        hotel.VisitorClient.Packets.Clear();
        await new RemoveGroupMemberEvent(hotel.Removal).Parse(_client, ClientPacket(OwnGroup, 8));
        RunGroupCycle();
        Assert.Single(GroupMessages(hotel.VisitorClient), "changed");

        hotel.Groups[OwnGroup].PublishJoin(8);
        hotel.VisitorClient.Packets.Clear();
        PlaceSavedBox(WiredLeaveBox, "wf_act_remove_from_group", 2, 2, 1);
        AddSpeechBox(WiredLeaveSpeech, "wf_trg_says_something", new() { IntParams = [1, 1, 0], Text = "go" }, 0, 2, 2);
        Assert.Equal("saved", await SaveGroupEffectOn(WiredLeaveBox, "wf_act_remove_from_group", OwnGroup, 1));
        Say(hotel.Visitor, "go");
        RunGroupCycle();
        Assert.False(hotel.Groups[OwnGroup].IsMember(8));
        Assert.Single(GroupMessages(hotel.VisitorClient), "changed");
    }

    private void RunGroupCycle()
    {
        _room.GetWired().OnFastCycle();
        _room.GetWired().OnCycle();
    }

    private void Say(RoomUser user, string text)
    {
        user.OnChat(0, text, false);
        RunGroupCycle();
    }

    private static string[] GroupMessages(TestClient client) =>
        [.. ChatTexts(client, ServerPacketHeader.ChatComposer), .. ChatTexts(client, ServerPacketHeader.WhisperComposer)];

    private async Task<string> SaveGroupEffect(string box, int groupId, int cancel = 1)
    {
        PlaceSavedBox(GroupBoxId, box, 1, 1, 1);
        AddSpeechBox(GroupSpeech, "wf_trg_says_something", new() { IntParams = [1, 1, 0], Text = "go" }, 0);

        return await SaveGroupEffectOn(GroupBoxId, box, groupId, cancel);
    }

    private async Task<string> SaveGroupEffectOn(uint id, string box, int groupId, int cancel)
    {
        object[] ints = box == "wf_act_remove_from_group" ? [2, groupId, cancel] : [1, groupId];
        _client.Packets.Clear();
        await new SaveWiredEffectConfigEvent(GroupSaveService()).Parse(_client, ClientPacket([(int)id, .. ints, "", 0, 0, 0, 1, 0, 0, 0]));

        return SaveAnswer();
    }

    private async Task<string> SaveMembershipTrigger(int mode, int groupId, int change)
    {
        PlaceSavedBox(GroupSpeech, "wf_trg_group_membership", 1, 1, 0);
        AddSpeechBox(GroupMessage, "wf_act_show_message", new() { IntParams = [0, 1, 34, -1], Text = "changed" }, 1);
        _client.Packets.Clear();
        await new SaveWiredTriggerConfigEvent(GroupSaveService()).Parse(_client, ClientPacket((int)GroupSpeech, 3, mode, groupId, change, "", 0, 0, 0, 0, 0));

        return SaveAnswer();
    }

    private static WiredConfigurationService GroupSaveService() =>
        new(TestWiredConfigurationStore.Instance, null!, new FakeWordFilter(), TestLogging.For<WiredConfigurationService>());

    private Plus.HabboHotel.Items.Wired.IWiredItem GetBox(uint id)
    {
        Assert.True(_room.GetWired().TryGet(id, out var box));

        return box!;
    }

    private GroupWiredHotel GroupHotel(GroupType type = GroupType.Open)
    {
        var (_, owner, visitor, visitorClient, _) = PrepareTwoUsers();
        visitorClient.GetHabbo().HabboStats = new HabboStats(0, 0, 0, 0, 0, 0, 0, 0, 0, "");
        var hotel = new GroupWiredHotel(owner, visitor, visitorClient, _client, type);
        _room.GetWired().GroupMembership = hotel.Wired;

        return hotel;
    }

    private sealed class GroupWiredHotel
    {
        public GroupWiredHotel(RoomUser owner, RoomUser visitor, TestClient visitorClient, TestClient ownerClient, GroupType type)
        {
            Owner = owner;
            Visitor = visitor;
            VisitorClient = visitorClient;
            Groups[OwnGroup] = NewGroup(OwnGroup, 7, type);
            Groups[OtherGroup] = NewGroup(OtherGroup, 5, GroupType.Open);
            var clients = Proxy<IGameClientManager>((method, args) => method switch
            {
                "GetClientByUserId" => (int)args[0]! switch { 7 => ownerClient, 8 => visitorClient, _ => null },
                "get_GetClients" => new List<GameClient> { ownerClient, visitorClient },
                _ => throw new NotSupportedException(method)
            });
            var manager = Proxy<IGroupManager>((method, args) =>
            {
                switch (method) {
                    case "TryGetGroup":
                        var found = Groups.TryGetValue((int)args[0]!, out var group);
                        args[1] = group;

                        return found;
                    case "GetGroupsForUser":
                        return new List<Group>();
                    case "GetColourCode":
                        return "";
                    default:
                        throw new NotSupportedException(method);
                }
            });
            var snapshots = Proxy<IGroupInfoSnapshotService>((_, args) =>
            {
                var group = (Group)args[0]!;

                return new GroupInfoSnapshot(group.Id, group.Type, group.Name, group.Description, group.Badge, group.RoomId, "HQ",
                    group.MemberCount, "1-1-2023", "Owner", false, false, false, false, group.RequestCount, false, group.ForumEnabled);
            });
            var rooms = Proxy<IRoomManager>((method, args) =>
            {
                args[1] = null;

                return false;
            });
            var events = new GroupMembershipEvents(clients);
            var store = new GroupWiredInvitationStore(Invitations);
            var gate = new AccountSessionGate();
            Participation = new GroupParticipationService(manager, snapshots, clients, new GroupWiredJoinStore(), gate, store, events);
            Mutation = new GroupMembershipMutationService(manager, rooms,
                Proxy<IGroupMemberIdentityLookup>((_, args) => new GroupMemberIdentity((int)args[0]!, "guest", "hr-1")),
                Proxy<IGroupMembershipMutationStore>((_, _) => true), events);
            Removal = new GroupRemovalService(manager, rooms, Proxy<ISettingsManager>((_, _) => "0"), clients, snapshots,
                Proxy<IGroupRemovalStore>((method, args) =>
                {
                    if (method == nameof(IGroupRemovalStore.RemoveMember)) {
                        Removals.Add(new object[] { args[0]!, args[1]! });
                    }

                    return true;
                }), gate, events);
            InvitationService = new GroupInvitationService(store, Participation, clients, TimeProvider.System);
            Wired = new WiredGroupMembership(manager, InvitationService, Removal);
        }

        public RoomUser Owner { get; }
        public RoomUser Visitor { get; }
        public TestClient VisitorClient { get; }
        public Dictionary<int, Group> Groups { get; } = new();
        public Dictionary<(int GroupId, int UserId), GroupInvitation> Invitations { get; } = new();
        public List<object[]> Removals { get; } = new();
        public GroupParticipationService Participation { get; }
        public GroupMembershipMutationService Mutation { get; }
        public GroupRemovalService Removal { get; }
        public GroupInvitationService InvitationService { get; }
        public WiredGroupMembership Wired { get; }

        private static Group NewGroup(int id, int owner, GroupType type) => new(id, id == OwnGroup ? "Crew" : "Others", "desc",
            "b01014s02024", 1, owner, DateTimeOffset.FromUnixTimeSeconds(1_700_000_000), (int)type, 3, 4, 0, false, GroupMembershipSnapshot.Empty);
    }

    private sealed class GroupWiredJoinStore : IGroupParticipationStore
    {
        public GroupJoinOutcome Join(int userId, int groupId, bool request, int membershipLimit) => GroupJoinOutcome.Inserted;

        public bool SaveFavourite(int userId, int groupId) => true;
    }

    private sealed class GroupWiredInvitationStore(Dictionary<(int GroupId, int UserId), GroupInvitation> rows) : IGroupInvitationStore
    {
        public void Save(GroupInvitation invitation) => rows[(invitation.GroupId, invitation.UserId)] = invitation;

        public GroupInvitation? Find(int groupId, int userId) => rows.GetValueOrDefault((groupId, userId));

        public bool Delete(int groupId, int userId) => rows.Remove((groupId, userId));

        public GroupJoinOutcome Accept(int userId, int groupId, int membershipLimit) =>
            rows.Remove((groupId, userId)) ? GroupJoinOutcome.Inserted : GroupJoinOutcome.Refused;
    }

    private sealed class GroupWireReader(byte[] body)
    {
        private int _at;

        public int Int()
        {
            var value = BinaryPrimitives.ReadInt32BigEndian(body.AsSpan(_at, 4));
            _at += 4;

            return value;
        }

        public string String()
        {
            var length = BinaryPrimitives.ReadUInt16BigEndian(body.AsSpan(_at, 2));
            var value = Encoding.UTF8.GetString(body, _at + 2, length);
            _at += 2 + length;

            return value;
        }
    }
}
