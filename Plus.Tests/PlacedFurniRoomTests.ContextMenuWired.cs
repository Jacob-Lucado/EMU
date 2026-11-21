using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Plus.Communication.Flash;
using Plus.Communication.Packets;
using Plus.Communication.Packets.Incoming;
using Plus.Communication.Packets.Incoming.Rooms.Furni.Wired;
using Plus.Communication.Packets.Outgoing;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Items.Wired.Configuration;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.Speech;
using Xunit;

namespace Plus.Tests;

// The context menu wired boxes, saved through the editor's raw save packets: the Context Menu Item global add-on fills the menu a user
// opens on a room unit (GetWiredContextMenu), and a click on a visible, enabled item (WiredContextMenuClick) fires the User Clicks
// Context Menu trigger, with the clicked unit as the clicked user.
public partial class PlacedFurniRoomTests
{
    private const uint MenuItemA = 981, MenuItemB = 982, MenuItemC = 983, MenuTrigger = 984, MenuMessage = 985, MenuPlaceholder = 986;

    private static int[] MenuInts(int userOn = 1, int userScope = 1, int petOn = 0, int petScope = 0, int botOn = 0, int visibility = 0, int enable = 0,
        int[]? conditions = null)
    {
        int[] ints = [0, userOn, userScope, petOn, petScope, botOn, visibility, enable, 6, 0, 6, 0, 6, 0, 6, 0, 6, 0, 6, 0];
        conditions?.CopyTo(ints, 8);

        return ints;
    }

    [Fact]
    public void ContextMenuBoxesAndPacketsUseTheUnreleasedFeatureTable()
    {
        Assert.Equal((10300u, 10301u, 10300u), (UnreleasedHabboFeatures.GetWiredContextMenuEvent, UnreleasedHabboFeatures.WiredContextMenuClickEvent,
            UnreleasedHabboFeatures.WiredContextMenuComposer));
        Assert.Equal(UnreleasedHabboFeatures.GetWiredContextMenuEvent, ClientPacketHeader.GetWiredContextMenuEvent);
        Assert.Equal(UnreleasedHabboFeatures.WiredContextMenuClickEvent, ClientPacketHeader.WiredContextMenuClickEvent);
        Assert.Equal(UnreleasedHabboFeatures.WiredContextMenuComposer, ServerPacketHeader.WiredContextMenuComposer);
        Assert.NotNull(CustomPacketRanges.Find(PacketRangeDirection.ClientToServer, 10300));
        Assert.NotNull(CustomPacketRanges.Find(PacketRangeDirection.ServerToClient, 10300));
        using var manager = new PacketManager([new GetWiredContextMenuEvent(), new WiredContextMenuClickEvent()], NullLogger<PacketManager>.Instance);
        Assert.True(manager.IsRegistered(10300));
        Assert.True(manager.IsRegistered(10301));
        Assert.Equal(900, UnreleasedHabboFeatures.ContextMenuEditorContext);

        foreach (var (name, category, code, defaults) in new[]
                 {
                     ("wf_xtra_context_menu_item", WiredBoxCategory.Addon, 9000, new[] { 0, 1, 1, 0, 0, 0, 0, 0, 6, 0, 6, 0, 6, 0, 6, 0, 6, 0, 6, 0 }),
                     ("wf_trg_click_context_menu", WiredBoxCategory.Trigger, 9001, new[] { 0 })
                 }) {
            Assert.True(WiredBoxRegistry.TryGet(name, out var descriptor), name);
            Assert.Equal((category, code), (descriptor.Category, descriptor.EditorCode));
            Assert.True(WiredNativeEditorProjection.Supports(name));
            Assert.Equal(defaults, WiredNativeEditorProjection.DefaultNative(descriptor).OwnedIntParams.ToArray());
            Assert.NotNull(WiredNativeEditorProjection.DefaultRuntime(1, descriptor));
        }
    }

    [Theory]
    [InlineData("wave\tWave hello", 0, 0, "saved")]
    [InlineData("", 0, 0, "saved")]
    [InlineData("wave_2\t" + "1234567890123456789012345678901234567890123456789012345678901234", 0, 0, "saved")]
    [InlineData("wave\t" + "12345678901234567890123456789012345678901234567890123456789012345", 0, 0, "dropped")]
    [InlineData("bad id\tWave", 0, 0, "dropped")]
    [InlineData("123456789012345678901234567890123\tWave", 0, 0, "dropped")]
    [InlineData("wave\t", 0, 0, "dropped")]
    [InlineData("wave\tWave\tagain", 0, 0, "dropped")]
    [InlineData("wave\tWave", 1, 0, "dropped")]
    [InlineData("wave\tWave", 0, 4, "dropped")]
    public async Task TheMenuItemSavesAButtonIdAndTextWithinTheirLimits(string text, int targetKind, int visibility, string answer)
    {
        PrepareTwoUsers();
        var ints = MenuInts(visibility: visibility);
        ints[0] = targetKind;

        Assert.Equal(answer, Answer(await SaveMenuItem(MenuItemA, 1, text, ints)));
    }

    [Theory]
    [InlineData("wave", 0, "saved")]
    [InlineData("", 0, "saved")]
    [InlineData("wave", 1, "dropped")]
    [InlineData("bad-id", 0, "dropped")]
    [InlineData("123456789012345678901234567890123", 0, "dropped")]
    public async Task TheClickTriggerSavesAButtonIdForUsers(string buttonId, int targetKind, string answer)
    {
        PrepareTwoUsers();

        Assert.Equal(answer, Answer(await SaveMenuTrigger(buttonId, targetKind)));
    }

    [Fact]
    public async Task SavedContextMenuBoxesLoadBackFromTheirStoredRows()
    {
        PrepareTwoUsers();
        await SaveMenuItem(MenuItemA, 1, "wave\tWave", MenuInts(userScope: 0));
        await SaveMenuTrigger("wave");

        foreach (var (id, name) in new[] { (MenuItemA, "wf_xtra_context_menu_item"), (MenuTrigger, "wf_trg_click_context_menu") }) {
            Assert.True(_room.GetWired().TryGet(id, out var box));
            var stored = (IWiredConfiguredItem)box!;
            var row = Assert.IsType<WiredConfiguration>(WiredConfigurationStore.DecodeRow(id, name, JsonSerializer.Serialize(stored.Configuration.Origin!.Native)));

            Assert.Equal(stored.Configuration.IntParams.ToArray(), row.IntParams.ToArray());
            Assert.Equal(stored.Configuration.Text, row.Text);
        }
    }

    [Fact]
    public async Task TheMenuListsTheItemsInAddOnOrderWithTheirPlaceholdersFilled()
    {
        var (_, _, visitor, _, _) = PrepareTwoUsers();
        Assert.Equal("saved", await SaveMenuItem(MenuItemB, 2, "second\tSecond", MenuInts(userScope: 0)));
        Assert.Equal("saved", await SaveMenuItem(MenuItemA, 1, "first\tHello $(who)", MenuInts(userScope: 0)));
        Assert.Equal("saved", await SaveMenuItem(MenuItemC, 3, "", MenuInts(userScope: 0)));
        PlaceSavedBox(MenuPlaceholder, "wf_xtra_global_placeholder", 3, 3, 0);
        _client.Packets.Clear();
        await new SaveWiredAddonConfigEvent(MenuSaveService()).Parse(_client, ClientPacket((int)MenuPlaceholder, 3, 0, 0, 0, "who\tfriend", 0, 0, 0, 0, 0));
        Assert.Equal("saved", SaveAnswer());

        var menu = await OpenMenu(_client, 0, visitor.VirtualId);

        Assert.Equal((0, visitor.VirtualId), (menu.TargetType, menu.TargetId));
        Assert.Equal(new[] { ("first", "Hello friend", true), ("second", "Second", true) }, menu.Items);
    }

    [Theory]
    [InlineData(1, 0, true, true)]
    [InlineData(1, 1, true, false)]
    [InlineData(1, 2, false, true)]
    [InlineData(0, 0, false, false)]
    public async Task TheUserScopeDecidesOnWhichAvatarsTheItemShows(int userOn, int userScope, bool onOther, bool onSelf)
    {
        var (_, owner, visitor, _, _) = PrepareTwoUsers();
        await SaveMenuItem(MenuItemA, 1, "wave\tWave", MenuInts(userOn, userScope));

        Assert.Equal(onOther, (await OpenMenu(_client, 0, visitor.VirtualId)).Items.Length == 1);
        Assert.Equal(onSelf, (await OpenMenu(_client, 0, owner.VirtualId)).Items.Length == 1);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task ABotShowsTheItemOnlyWithBotsSwitchedOn(int botOn, bool shown)
    {
        PrepareTwoUsers();
        var speech = new List<RandomSpeech>();
        var bot = _room.GetRoomUserManager().DeployBot(new(1, RoomId, "generic", "stand", "Robbie", "", "hr-100", 2, 2, 0, 6, 0, 0, 0, 0, ref speech, "M", 0,
            7, false, 7, false, 0), null!);
        await SaveMenuItem(MenuItemA, 1, "wave\tWave", MenuInts(userOn: 1, userScope: 0, botOn: botOn));

        Assert.Equal(shown, (await OpenMenu(_client, 0, bot.VirtualId)).Items.Length == 1);
    }

    [Theory]
    [InlineData(0, 0, 7, false)]
    [InlineData(1, 0, 9, true)]
    [InlineData(1, 1, 7, true)]
    [InlineData(1, 1, 9, false)]
    public async Task APetShowsTheItemWithPetsSwitchedOnAndTheScopeMatched(int petOn, int petScope, int petOwner, bool shown)
    {
        PrepareTwoUsers();
        var speech = new List<RandomSpeech>();
        var pet = _room.GetRoomUserManager().DeployBot(new(80, RoomId, "pet", "freeroam", "Rex", "", "hr-100", 2, 2, 0, 0, 0, 0, 0, 0, ref speech, "M", 0,
            petOwner, false, 7, false, 0), new Pet(80, petOwner, RoomId, "Rex", 0, "0", "ffffff", 0, 100, 100, 0, DateTimeOffset.UnixEpoch, 2, 2, 0, 0, 0, 0, 0, "", "owner"));
        Assert.True(pet.IsPet);
        await SaveMenuItem(MenuItemA, 1, "wave\tWave", MenuInts(userOn: 1, userScope: 0, petOn: petOn, petScope: petScope));

        Assert.Equal(shown, (await OpenMenu(_client, 0, pet.VirtualId)).Items.Length == 1);
    }

    [Fact]
    public async Task FurniTargetsAndUnknownUnitsAnswerAnEmptyMenu()
    {
        var (_, _, visitor, _, _) = PrepareTwoUsers();
        await SaveMenuItem(MenuItemA, 1, "wave\tWave", MenuInts(userScope: 0));

        Assert.Empty((await OpenMenu(_client, 1, visitor.VirtualId)).Items);
        Assert.Empty((await OpenMenu(_client, 0, 999)).Items);
    }

    [Fact]
    public async Task VisibilityAndEnableConditionsReadTheViewersUserVariables()
    {
        var actor = EditedVariableRoom();
        var wireB = BuildersClubItemIds.Assign(HeldFurniB);

        try {
            GiveEditedVariables(actor, wireB);
            var user = $"user:{EditedUser}";
            var noValue = $"user:{EditedNoValue}";
            // Shown when the viewer's variable is 7; enabled while the viewer holds the second variable, disabled while it must not.
            Assert.Equal("saved", await SaveMenuItem(MenuItemA, 1, "equal\tEqual", MenuInts(userScope: 0, visibility: 1, enable: 1, conditions: [1, 7, 6, 0, 6, 0, 6, 0]),
                [user, "0", "0", noValue, "0", "0"]));
            Assert.Equal("saved", await SaveMenuItem(MenuItemB, 2, "other\tOther", MenuInts(userScope: 0, visibility: 1, enable: 1, conditions: [1, 8, 6, 0, 6, 0, 7, 0]),
                [user, "0", "0", noValue, "0", "0"]));
            Assert.Equal("saved", await SaveMenuItem(MenuItemC, 3, "gone\tGone", MenuInts(userScope: 0, visibility: 2, enable: 1, conditions: [5, 7, 2, 6, 6, 0, 7, 0]),
                [user, user, "0", noValue, "0", "0"]));

            var menu = await OpenMenu(_client, 0, actor.VirtualId);

            Assert.Equal(new[] { ("equal", "Equal", true), ("gone", "Gone", false) }, menu.Items);
        }
        finally {
            BuildersClubItemIds.Release(HeldFurniB);
        }
    }

    [Fact]
    public async Task AConditionNeedsAUserVariableOfTheRoom()
    {
        EditedVariableRoom();

        Assert.Equal(UnreleasedHabboFeatures.ContextMenuVariableError,
            await SaveMenuItem(MenuItemA, 1, "wave\tWave", MenuInts(userScope: 0, visibility: 1), ["0", "0", "0", "0", "0", "0"]));
        Assert.Equal(UnreleasedHabboFeatures.ContextMenuVariableError,
            await SaveMenuItem(MenuItemB, 2, "wave\tWave", MenuInts(userScope: 0, enable: 1), ["0", "0", "0", $"furni:{EditedFurni}", "0", "0"]));
        Assert.Equal("saved", await SaveMenuItem(MenuItemC, 3, "wave\tWave", MenuInts(userScope: 0, visibility: 1), [$"user:{EditedUser}", "0", "0", "0", "0", "0"]));
    }

    [Fact]
    public async Task ClickingAVisibleEnabledItemFiresTheTriggerOnTheClickedUser()
    {
        var (_, _, visitor, visitorClient, _) = PrepareTwoUsers();
        await SaveMenuItem(MenuItemA, 1, "wave\tWave", MenuInts(userScope: 0));
        await SaveMenuTrigger("wave");

        await Click(_client, 0, visitor.VirtualId, "wave");

        Assert.Contains("clicked", MenuMessages(visitorClient));
        Assert.DoesNotContain("clicked", MenuMessages(_client));
    }

    [Theory]
    [InlineData("other", 0, 1, 0)]
    [InlineData("wave", 2, 1, 0)]
    [InlineData("wave", 0, 1, 1)]
    [InlineData("wave", 0, 0, 0)]
    public async Task AClickIsIgnoredUnlessTheItemIsVisibleAndEnabledForTheClicker(string button, int userScope, int userOn, int targetType)
    {
        var (_, _, visitor, visitorClient, _) = PrepareTwoUsers();
        await SaveMenuItem(MenuItemA, 1, "wave\tWave", MenuInts(userOn, userScope));
        await SaveMenuItem(MenuItemB, 2, "other\tOther", MenuInts(userScope: 0));
        await SaveMenuTrigger("wave");

        await Click(_client, targetType, visitor.VirtualId, button);

        Assert.DoesNotContain("clicked", MenuMessages(visitorClient));
    }

    [Fact]
    public async Task ADisabledItemIgnoresAClick()
    {
        var actor = EditedVariableRoom();
        var wireB = BuildersClubItemIds.Assign(HeldFurniB);

        try {
            GiveEditedVariables(actor, wireB);
            Assert.Equal("saved", await SaveMenuItem(MenuItemA, 1, "wave\tWave", MenuInts(userScope: 0, enable: 1, conditions: [6, 0, 6, 0, 6, 0, 7, 0]),
                ["0", "0", "0", $"user:{EditedNoValue}", "0", "0"]));
            Assert.Equal("saved", await SaveMenuTrigger("wave"));

            await Click(_client, 0, actor.VirtualId, "wave");

            Assert.DoesNotContain("clicked", MenuMessages(_client));
        }
        finally {
            BuildersClubItemIds.Release(HeldFurniB);
        }
    }

    [Fact]
    public async Task TheTriggerEditorListsTheRoomsButtonIdsOnce()
    {
        PrepareTwoUsers();
        await SaveMenuItem(MenuItemB, 2, "wave\tWave", MenuInts(userScope: 0));
        await SaveMenuItem(MenuItemA, 1, "bow\tBow", MenuInts(userScope: 0));
        await SaveMenuItem(MenuItemC, 3, "wave\tWave again", MenuInts(userScope: 0));
        await SaveMenuTrigger("wave");

        _client.Packets.Clear();
        await new OpenWiredEvent().Parse(_room, _client, ClientPacket((int)MenuTrigger));
        var frame = new FlashIncomingPacket { Buffer = Assert.Single(_client.Packets, packet => packet.Header == ServerPacketHeader.WiredTriggeRconfigComposer).Body };

        Assert.Equal(new[] { "bow", "wave" }, TriggerEditorButtons(frame));
    }

    // The trigger editor frame up to its context entries, then the context menu entry: type 900, count, the button ids.
    private static string[] TriggerEditorButtons(FlashIncomingPacket frame)
    {
        frame.ReadInt();
        SkipInts(frame);
        SkipInts(frame);
        frame.ReadInt();
        frame.ReadUInt();
        frame.ReadString();
        SkipInts(frame);

        for (var count = frame.ReadInt(); count > 0; count--) {
            frame.ReadString();
        }

        SkipInts(frame);
        SkipInts(frame);
        frame.ReadInt();
        frame.ReadBool();

        for (var groups = 0; groups < 2; groups++) {
            for (var count = frame.ReadInt(); count > 0; count--) {
                SkipInts(frame);
            }
        }

        SkipInts(frame);
        SkipInts(frame);
        frame.ReadBool();
        Assert.Equal(1, frame.ReadInt());
        Assert.Equal(900, frame.ReadInt());
        var buttons = new string[frame.ReadInt()];

        for (var index = 0; index < buttons.Length; index++) {
            buttons[index] = frame.ReadString();
        }

        SkipInts(frame);
        Assert.False(frame.HasDataRemaining());

        return buttons;
    }

    private sealed record OpenedMenu(int TargetType, int TargetId, (string ButtonId, string Text, bool Enabled)[] Items);

    private async Task<OpenedMenu> OpenMenu(TestClient client, int targetType, int targetId)
    {
        client.Packets.Clear();
        await new GetWiredContextMenuEvent().Parse(client, ClientPacket(targetType, targetId));
        var frame = new FlashIncomingPacket { Buffer = Assert.Single(client.Packets, packet => packet.Header == ServerPacketHeader.WiredContextMenuComposer).Body };
        var (type, id) = (frame.ReadInt(), frame.ReadInt());
        var items = new (string, string, bool)[frame.ReadInt()];

        for (var index = 0; index < items.Length; index++) {
            items[index] = (frame.ReadString(), frame.ReadString(), frame.ReadBool());
        }

        Assert.False(frame.HasDataRemaining());

        return new(type, id, items);
    }

    private async Task Click(TestClient client, int targetType, int targetId, string button)
    {
        await new WiredContextMenuClickEvent().Parse(client, ClientPacket(targetType, targetId, button));
        _room.GetWired().OnFastCycle();
        _room.GetWired().OnCycle();
    }

    private static string[] MenuMessages(TestClient client) =>
        [.. ChatTexts(client, ServerPacketHeader.ChatComposer), .. ChatTexts(client, ServerPacketHeader.WhisperComposer)];

    private static WiredConfigurationService MenuSaveService() =>
        new(TestWiredConfigurationStore.Instance, null!, new FakeWordFilter(), TestLogging.For<WiredConfigurationService>());

    private static string Answer(string answer) => answer is "saved" or "dropped" ? answer : "dropped";

    // The add-on envelope: ints, text, no picks, no sources, the six variable slots and no second pick list.
    private async Task<string> SaveMenuItem(uint id, double height, string text, int[] ints, string[]? variables = null)
    {
        if (!_room.GetWired().TryGet(id, out _)) {
            PlaceSavedBox(id, "wf_xtra_context_menu_item", 3, 3, height);
        }

        variables ??= ["0", "0", "0", "0", "0", "0"];
        _client.Packets.Clear();
        await new SaveWiredAddonConfigEvent(MenuSaveService()).Parse(_client,
            ClientPacket([(int)id, ints.Length, .. ints.Cast<object>(), text, 0, 0, 0, variables.Length, .. variables.Cast<object>(), 0]));

        return SaveAnswer();
    }

    private async Task<string> SaveMenuTrigger(string buttonId, int targetKind = 0)
    {
        if (!_room.GetWired().TryGet(MenuTrigger, out _)) {
            PlaceSavedBox(MenuTrigger, "wf_trg_click_context_menu", 0, 3, 0);
            PlaceSavedBox(MenuMessage, "wf_act_show_message", 0, 3, 1);
            _client.Packets.Clear();
            // Show Message to user source 11: the clicked user.
            await new SaveWiredEffectConfigEvent(MenuSaveService()).Parse(_client, ClientPacket((int)MenuMessage, 3, 0, 202, -1, "clicked", 0, 0, 0, 1, 11, 0, 0));
            Assert.Equal("saved", SaveAnswer());
        }

        _client.Packets.Clear();
        await new SaveWiredTriggerConfigEvent(MenuSaveService()).Parse(_client, ClientPacket((int)MenuTrigger, 1, targetKind, buttonId, 0, 0, 0, 0, 0));

        return SaveAnswer();
    }
}
