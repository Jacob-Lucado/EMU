namespace Plus.Communication.Packets.Incoming;

public static class ClientPacketHeader
{
    // Handshake
    public const uint InitDiffieHandshakeEvent = 384;
    public const uint CompleteDiffieHandshakeEvent = 2389;
    public const uint UniqueIdEvent = 37;
    public const uint SSOTicketEvent = 2684;
    public const uint InfoRetrieveEvent = 2519;

    // Avatar
    public const uint CheckUserNameEvent = 3721;
    public const uint ChangeUserNameEvent = 1028;
    public const uint GetWardrobeEvent = 3089;
    public const uint SaveWardrobeOutfitEvent = 3015;
    public const uint GetUserNftWardrobeEvent = 19;
    public const uint GetSelectedNftWardrobeOutfitEvent = 1680;
    public const uint SaveUserNftWardrobeEvent = 539;

    // Collectibles hub
    public const uint GetCollectibleWalletAddressesEvent = 1534;
    public const uint GetCollectorScoreEvent = 1496;
    public const uint GetNftCollectionsEvent = 1479;
    public const uint ClaimNftCollectionBonusEvent = 2572;
    public const uint ClaimNftCollectionRewardEvent = 1730;
    public const uint GetNftStoreOffersEvent = 3226;
    public const uint PurchaseNftStoreOfferEvent = 784;
    public const uint GetCollectibleMintableItemTypesEvent = 3993;
    public const uint GetCollectibleMintingEnabledEvent = 3301;
    public const uint MintCollectibleEvent = 672;
    public const uint GetNftClaimsEvent = 3707;
    public const uint ClaimNftRewardsEvent = 133;

    //Preferences
    public const uint SetChatStylePreferenceEvent = 559;
    public const uint SetChatDisplayPreferencesEvent = 1222;
    public const uint SetOnlineIndicatorPreferenceEvent = 2990;
    public const uint SetChatPreferenceEvent = 65476;

    // Catalog
    public const uint GetCatalogPageEvent = 2185;
    public const uint GetProductOfferEvent = 3775;
    public const uint GetKickbackInfoEvent = 2018;
    public const uint GetClubGiftInfoEvent = 3912;
    public const uint PurchaseFromCatalogEvent = 29;
    public const uint PurchaseFromCatalogAsGiftEvent = 2554;
    public const uint GetBundleDiscountRulesetEvent = 2307;

    // Polls
    public const uint PollStartEvent = 1413;
    public const uint PollRejectEvent = 849;
    public const uint PollAnswerEvent = 3458;

    // Navigator

    // Messenger
    public const uint GetFriendRequestsEvent = 3833;

    // Quests
    public const uint GetQuestListEvent = 1310;
    public const uint StartQuestEvent = 2381;
    public const uint GetCurrentQuestEvent = 937;
    public const uint CancelQuestEvent = 285;

    // Room Avatar
    public const uint ActionEvent = 1954;
    public const uint ApplySignEvent = 207;
    public const uint DanceEvent = 1366;
    public const uint SitEvent = 1117;
    public const uint ChangeMottoEvent = 2719;
    public const uint LookToEvent = 2574;
    public const uint DropHandItemEvent = 810;

    // Room Connection
    public const uint OpenFlatConnectionEvent = 1267;
    public const uint GoToFlatEvent = 65442;

    // Room Chat
    public const uint ChatEvent = 3463;
    public const uint ShoutEvent = 3079;
    public const uint WhisperEvent = 3523;

    // Room Engine

    // Room Settings

    // Room Action

    // Users
    public const uint GetIgnoredUsersEvent = 1415;

    // Moderation
    public const uint OpenHelpToolEvent = 3158;
    public const uint CallForHelpPendingCallsDeletedEvent = 2635;
    public const uint ModeratorActionEvent = 768;
    public const uint ModerationMsgEvent = 3797;
    public const uint ModerationMuteEvent = 3521;
    public const uint ModerationTradeLockEvent = 3205;
    public const uint GetModeratorUserRoomVisitsEvent = 596;
    public const uint ModerationKickEvent = 3858;
    public const uint GetModeratorRoomInfoEvent = 2979;
    public const uint GetModeratorUserInfoEvent = 1240;
    public const uint GetModeratorRoomChatlogEvent = 276;
    public const uint ModerateRoomEvent = 2394;
    public const uint GetModeratorUserChatlogEvent = 805;
    public const uint GetModeratorTicketChatlogsEvent = 2328;
    public const uint ModerationCautionEvent = 194;
    public const uint ModerationBanEvent = 710;
    public const uint SubmitNewTicketEvent = 1301;
    public const uint CloseIssueDefaultActionEvent = 3237;

    // Inventory
    public const uint GetCreditsInfoEvent = 893;
    public const uint GetAchievementsEvent = 3231;
    public const uint GetBadgesEvent = 2687;
    public const uint RequestFurniInventoryEvent = 3120;
    public const uint SetActivatedBadgesEvent = 2190;
    public const uint AvatarEffectActivatedEvent = 723;
    public const uint AvatarEffectSelectedEvent = 3316;

    public const uint InitTradeEvent = 819;
    public const uint TradingCancelConfirmEvent = 3789;
    public const uint TradingModifyEvent = 3880;
    public const uint TradingOfferItemEvent = 1681;
    public const uint TradingCancelEvent = 754;
    public const uint TradingConfirmEvent = 2360;
    public const uint TradingOfferItemsEvent = 3764;
    public const uint TradingRemoveItemEvent = 1308;
    public const uint TradingAcceptEvent = 1127;

    // Register
    public const uint UpdateFigureDataEvent = 1038;

    // Groups
    public const uint GetBadgeEditorPartsEvent = 1284;
    public const uint GetGroupCreationWindowEvent = 2996;
    public const uint GetGroupFurniSettingsEvent = 2943;
    public const uint DeclineGroupMembershipEvent = 1599;
    public const uint JoinGroupEvent = 3339;
    public const uint UpdateGroupColoursEvent = 776;
    public const uint SetGroupFavouriteEvent = 940;
    public const uint GetGroupMembersEvent = 3354;

    // Group Forums
    public const uint PostGroupContentEvent = 3387;
    public const uint GetForumStatsEvent = 2803;
    public const uint GetForumThreadEvent = 845;
    public const uint UpdateForumReadMarkersEvent = 2577;
    public const uint GetForumsUnreadCountEvent = 1154;

    // Sound


    // Ambassador

    public const uint AmbassadorAlertEvent = 990;

    public const uint RemoveMyRightsEvent = 2703;
    public const uint GiveHandItemEvent = 3129;
    public const uint GoToHotelViewEvent = 3802;
    public const uint GetRoomFilterListEvent = 3297;
    public const uint GetPromoArticlesEvent = 314;
    public const uint ModifyWhoCanRideHorseEvent = 879;
    public const uint RemoveFriendEvent = 3675;
    public const uint RefreshCampaignEvent = 2617;
    public const uint AcceptFriendEvent = 1558;
    public const uint YouTubeVideoInformationEvent = 65401; // The client has no composer for this id; 2488 is TryPhoneNumber
    public const uint FollowFriendEvent = 452;
    public const uint SaveBotActionEvent = 3209;
    public const uint LetUserInEvent = 2555;
    public const uint GetMarketplaceItemStatsEvent = 3659;
    public const uint GetSellablePetPalettesEvent = 1329;
    public const uint OpenCampaignCalendarDoorAsStaffEvent = 301;
    public const uint SetUIFlagsEvent = 2121;
    public const uint DeleteRoomEvent = 2429;
    public const uint SetSoundSettingsEvent = 1593;
    public const uint GetSoundSettingsEvent = 2886;
    public const uint InitializeGameCenterEvent = 65468;
    public const uint RedeemOfferCreditsEvent = 3570;
    public const uint FriendListUpdateEvent = 1825;
    public const uint FriendFurniConfirmLockEvent = 2935;
    public const uint UseHabboWheelEvent = 1985;
    public const uint SaveRoomSettingsEvent = 566;
    public const uint ToggleMoodlightEvent = 815;
    public const uint GetDailyQuestEvent = 1114;
    public const uint SetMannequinNameEvent = 3429;
    public const uint OneWayGateEvent = 1048;
    public const uint EventTrackerEvent = 65429;
    public const uint GetOccupiedTilesEvent = 2926;
    public const uint PickUpPetEvent = 3440;
    public const uint GetPetInventoryEvent = 2546;
    public const uint GetRoomEntryTileEvent = 2071;
    public const uint GetOwnOffersEvent = 1591;
    public const uint CheckPetNameEvent = 3230;
    public const uint SetUserFocusPreferenceEvent = 925;
    public const uint SubmitBullyReportEvent = 2999;
    public const uint RemoveRightsEvent = 1915;
    public const uint MakeOfferEvent = 3144;
    public const uint KickUserEvent = 1393;
    public const uint GetRoomSettingsEvent = 2340;
    public const uint GetThreadsListDataEvent = 3501;
    public const uint GetForumUserProfileEvent = 746;
    public const uint OpenWiredEvent = 1901;
    public const uint WiredMonitorRequestEvent = 10021;
    public const uint WiredRoomLogsPageEvent = 3696;
    public const uint WiredRoomSettingsRequestEvent = 10022;
    public const uint WiredRoomSettingsSaveEvent = 10023;
    public const uint WiredGetRoomSettingsEvent = 3895;
    public const uint WiredSetRoomSettingsEvent = 2842;
    public const uint WiredSetPreferencesEvent = 1046;
    public const uint WiredMenuPermissionsSaveEvent = 65465;
    public const uint WiredRoomUpdateEvent = 2454;
    public const uint ClickFurniEvent = 3967;
    public const uint ClickUserEvent = 10020;
    public const uint WiredUserVariablesRequestEvent = 10024;
    public const uint WiredUserVariableUpdateEvent = 10025;
    public const uint WiredUserVariableManageEvent = 10026;
    public const uint WiredVariableHoldersPageEvent = 65307;
    public const uint WiredVariableHoldersRequestEvent = 65441;
    public const uint WiredVariableHashesEvent = 2650;
    public const uint WiredAllVariablesRequestEvent = 859;
    public const uint SaveWiredEffectConfigEvent = 3318;
    public const uint ApplySnapshotEvent = 229;
    public const uint SaveWiredSelectorConfigEvent = 752;
    public const uint SaveWiredAddonConfigEvent = 2067;
    public const uint SaveWiredVariableConfigEvent = 1491;
    public const uint GetRoomEntryDataEvent = 65460;
    public const uint JoinQueueEvent = 65471;
    public const uint CanCreateRoomEvent = 141;
    public const uint SetTonerEvent = 3335;
    public const uint RecolorFurnitureEvent = 3879;
    public const uint SaveWiredTriggerConfigEvent = 2921;
    public const uint PlaceBotEvent = 7;
    public const uint MoveEntityInFlatEvent = 833;
    public const uint GetRelationshipsEvent = 3048;
    public const uint SetMessengerInviteStatusEvent = 2499;
    public const uint UseFurnitureEvent = 3753;
    public const uint GetUserFlatCatsEvent = 253;
    public const uint AssignRightsEvent = 1436;
    public const uint GetRoomBannedUsersEvent = 3112;
    public const uint ReleaseTicketEvent = 3003;
    public const uint OpenPlayerProfileEvent = 1058;
    public const uint GetSanctionStatusEvent = 65447;
    public const uint CreditFurniRedeemEvent = 3275;
    public const uint DisconnectEvent = 673;
    public const uint PickupObjectEvent = 3859;
    public const uint FindRandomFriendingRoomEvent = 541;
    public const uint UseSellableClothingEvent = 2032;
    public const uint MoveObjectEvent = 1358;
    public const uint GetFurnitureAliasesEvent = 2421;
    public const uint TakeAdminRightsEvent = 3424;
    public const uint ModifyRoomFilterListEvent = 1488;
    public const uint MoodlightUpdateEvent = 2075;
    public const uint GetPetTrainingPanelEvent = 24;
    public const uint GetSongInfoEvent = 3336;
    public const uint UseWallItemEvent = 3377;
    public const uint GetTalentTrackEvent = 3011;
    public const uint GiveAdminRightsEvent = 687;
    public const uint GetCatalogIndexEvent = 3586;
    public const uint BuildersClubPlaceRoomItemEvent = 1476;
    public const uint BuildersClubPlaceWallItemEvent = 869;
    public const uint GetGuideReportingStatusEvent = 1944;
    public const uint CancelOfferEvent = 1008;
    public const uint SaveWiredConditionConfigEvent = 1828;
    public const uint RedeemVoucherEvent = 272;
    public const uint ThrowDiceEvent = 3102;
    public const uint CraftSecretEvent = 970;
    public const uint GetGameListEvent = 65391;
    public const uint SetRelationshipEvent = 3667;
    public const uint RequestFriendEvent = 956;
    public const uint MemoryPerformanceEvent = 770;
    public const uint ToggleYouTubeVideoEvent = 140;
    public const uint SetMannequinFigureEvent = 2092;
    public const uint SetClothingChangeDataEvent = 1202;
    public const uint GetEventCategoriesEvent = 3422;
    public const uint DeleteGroupThreadEvent = 3847;
    public const uint PurchaseGroupEvent = 1920;
    public const uint MessengerInitEvent = 2213;
    public const uint CancelTypingEvent = 733;
    public const uint GetMoodlightConfigEvent = 244;
    public const uint GetGroupInfoEvent = 671;
    public const uint CreateFlatEvent = 3992;
    public const uint LatencyPingRequestEvent = 2391;
    public const uint GetSelectedBadgesEvent = 972;
    public const uint AddStickyNoteEvent = 3921;
    public const uint RideHorseEvent = 3325;
    public const uint InitializeNewNavigatorEvent = 2362;
    public const uint GetForumsListDataEvent = 3246;
    public const uint ToggleMuteToolEvent = 3580;
    public const uint UpdateGroupIdentityEvent = 1153;
    public const uint UpdateStickyNoteEvent = 3293;
    public const uint UnbanUserFromRoomEvent = 2513;
    public const uint UnignoreUserEvent = 1778;
    public const uint OpenGiftEvent = 3104;
    public const uint ApplyDecorationEvent = 2348;
    public const uint ScrGetUserInfoEvent = 2479;
    public const uint RemoveGroupMemberEvent = 858;
    public const uint ConfirmRemoveGroupMemberEvent = 2272;
    public const uint DiceOffEvent = 2597;
    public const uint YouTubeGetNextVideo = 2958;
    public const uint RemoveFavouriteRoomEvent = 1517;
    public const uint RespectUserEvent = 1597;
    public const uint AddFavouriteRoomEvent = 1257;
    public const uint DeclineFriendEvent = 2529;
    public const uint StartTypingEvent = 1542;
    public const uint GetGroupFurniConfigEvent = 749;
    public const uint SendRoomInviteEvent = 3287;
    public const uint RemoveAllRightsEvent = 1523;
    public const uint GetYouTubeTelevisionEvent = 3687;
    public const uint FindNewFriendsEvent = 3978;
    public const uint GetPromotableRoomsEvent = 2316;
    public const uint GetBotInventoryEvent = 1738;
    public const uint GetRentableSpaceEvent = 953;
    public const uint OpenBotActionEvent = 702;
    public const uint OpenCampaignCalendarDoorEvent = 3800;
    public const uint DeleteGroupPostEvent = 1072;
    public const uint UpdateGroupBadgeEvent = 1953;
    public const uint PlaceObjectEvent = 263;
    public const uint RemoveGroupFavouriteEvent = 3470;
    public const uint UpdateNavigatorSettingsEvent = 3757;
    public const uint CheckGnomeNameEvent = 3684;
    public const uint NavigatorSearchEvent = 3220;
    public const uint GetPetInformationEvent = 1235;
    public const uint GetGuestRoomEvent = 216;
    public const uint UpdateThreadEvent = 503;
    public const uint AcceptGroupMembershipEvent = 351;
    public const uint GetMarketplaceConfigurationEvent = 3215;
    public const uint Game2GetWeeklyLeaderboardEvent = 3761;
    public const uint BuyOfferEvent = 1946;
    public const uint RemoveSaddleFromHorseEvent = 2806;
    public const uint GiveRoomScoreEvent = 1979;
    public const uint GetHabboClubWindowEvent = 52;
    public const uint DeleteStickyNoteEvent = 1657;
    public const uint MuteUserEvent = 3359;
    public const uint ApplyHorseEffectEvent = 3654;
    public const uint ClientHelloEvent = 4000; //4000
    public const uint OnBullyClickEvent = 65455;
    public const uint HabboSearchEvent = 3614;
    public const uint PickTicketEvent = 3277;
    public const uint GetGiftWrappingConfigurationEvent = 838;
    public const uint GetCraftingRecipesAvailableEvent = 1434;
    public const uint GetThreadDataEvent = 271;
    public const uint ManageGroupEvent = 549;
    public const uint PlacePetEvent = 1050;
    public const uint MovePetEvent = 545;
    public const uint EditRoomPromotionEvent = 3306;
    public const uint UpdateFloorPropertiesEvent = 2502;
    public const uint MoveWallItemEvent = 245;
    public const uint VersionCheckEvent = 631;
    public const uint PongEvent = 923;
    public const uint DeleteGroupEvent = 3504;
    public const uint UpdateGroupSettingsEvent = 521;
    public const uint GetRecyclerPrizesEvent = 2854;
    public const uint PurchaseRoomAdEvent = 3513;
    public const uint PickUpBotEvent = 401;
    public const uint GetOffersEvent = 1919;
    public const uint GetHabboGroupBadgesEvent = 1132;
    public const uint GetUserTagsEvent = 65382;
    public const uint GetGameAchievementsEvent = 65475;
    public const uint GetCatalogRoomPromotionEvent = 3223;
    public const uint MoveAvatarEvent = 2995;
    public const uint SaveBrandingItemEvent = 3623;
    public const uint SetAreaHideEvent = 2228;
    public const uint SaveEnforcedCategorySettingsEvent = 188;
    public const uint RespectPetEvent = 493;
    public const uint GetMarketplaceCanMakeOfferEvent = 2036;
    public const uint UpdateMagicTileAdjacentEvent = 65449;
    public const uint UpdateMagicTileEvent = 1037;
    public const uint GetStickyNoteEvent = 3261;
    public const uint IgnoreUserEvent = 1342;
    public const uint BanUserEvent = 1352;
    public const uint UpdateForumSettingsEvent = 1398;
    public const uint GetRoomRightsEvent = 922;
    public const uint SendMsgEvent = 432;
    public const uint CloseTicketEvent = 515;

    //NotImplemented

    //Camera
    //public const uint InitCameraEvent =;
    //public const uint PhotoCompetitionEvent =;
    //public const uint PublishPhotoEvent =;
    //public const uint PurchasePhotoEvent =;
    //public const uint RenderRoomEvent =;
    //public const uint RenderRoomThumbnailEvent =;

    //campaign
    //public const uint OpenCampaignCalendarDoorAsStaffEvent =;
    //public const uint OpenCampaignCalendarDoorEvent =;

    //catalog
    public const uint GetHabboClubExtendOfferEvent = 610;
    public const uint PurchaseBasicMembershipExtensionEvent = 404;
    public const uint PurchaseVipMembershipExtensionEvent = 2697;
    public const uint SelectClubGiftEvent = 1697;


    //recycler
    public const uint GetRecyclerStatusEvent = 2868;
    public const uint RecyclerRecycleEvent = 3190;


    //FriendList
    //public const uint FriendFurniConfirmLockEvent =;
    //public const uint VisitUserEvent =;

    //crafting
    public const uint CraftEvent = 317;
    //public const uint CraftSecretEvent =;
    public const uint GetCraftableProductsEvent = 1942;
    public const uint GetCraftingRecipeEvent = 2524;
    //public const uint GetCraftingRecipesAvailableEvent =;

    // SnowStorm (AIR Game2).
    public const uint Game2CheckGameDirectoryStatusEvent = 983;
    public const uint Game2GetAccountGameStatusEvent = 2588;
    public const uint Game2QuickJoinEvent = 1723;
    public const uint Game2LeaveLobbyEvent = 2736;
    public const uint Game2LoadStageReadyEvent = 394;
    public const uint Game2ExitGameEvent = 2284;
    public const uint Game2GameChatEvent = 1312;
    public const uint Game2PlayAgainEvent = 309;
    public const uint Game2SetUserMoveTargetEvent = 3100;
    public const uint Game2ThrowSnowballAtPositionEvent = 1632;
    public const uint Game2ThrowSnowballAtHumanEvent = 1836;
    public const uint Game2MakeSnowballEvent = 2662;
    public const uint Game2RequestFullStatusUpdateEvent = 1559;
    public const uint Game2GetTotalLeaderboardEvent = 2306;
    public const uint Game2GetFriendsLeaderboardEvent = 3528;
    public const uint Game2GetWeeklyFriendsLeaderboardEvent = 475;
    public const uint Game2GetTotalGroupLeaderboardEvent = 3556;
    public const uint Game2GetWeeklyGroupLeaderboardEvent = 965;
    public const uint GetSnowWarGameTokensOfferEvent = 2236;
    public const uint PurchaseSnowWarGameTokensOfferEvent = 2146;




    public const uint TriggerHabbiconEvent = 3135;
    public const uint GetHabbiconShopDataEvent = 1454;
    public const uint GetHabbiconInfoEvent = 6;
    public const uint BuyHabbiconEvent = 900;
    public const uint BuyHabbiconCollectionEvent = 1215;
    public const uint ClaimHabbiconEvent = 411;
    public const uint FavoriteHabbiconEvent = 3078;
    public const uint UnfavoriteHabbiconEvent = 1852;
    public const uint UnseenResetCategoryEvent = 65428;
    public const uint UnseenResetItemsEvent = 65459;
    public const uint SendMessengerMessageEvent = 4902;
    // Camera internal IDs; wire IDs are revision-specific.
    public const uint InitCameraEvent = 3595;
    public const uint PhotoCompetitionEvent = 1294;
    public const uint PublishPhotoEvent = 2043;
    public const uint PurchasePhotoEvent = 2766;
    public const uint RenderRoomEvent = 2153;
    public const uint RenderRoomThumbnailEvent = 2465;
    public const uint GetRewardTracksEvent = 9450;
    public const uint ClaimRewardTrackPrizeEvent = 2685;
    public const uint PurchaseRewardTrackPremiumEvent = 590;
    // Daily tasks.
    public const uint GetDailyTasksEvent = 4100;
    public const uint ClaimDailyTaskEvent = 4101;
    public const uint GetQuizQuestionsEvent = 2731;
    public const uint PostQuizAnswersEvent = 2861;
    // Room music
    public const uint AddJukeboxDiskEvent = 3242;
    public const uint RemoveJukeboxDiskEvent = 439;
    public const uint GetJukeboxPlaylistEvent = 721;
    public const uint GetSoundMachinePlaylistEvent = 1864;
    public const uint GetUserSongDisksEvent = 1812;
    public const uint GetNowPlayingEvent = 1950;
    public const uint GetOfficialSongIdEvent = 2365;
    public const uint WiredVariableInspectionRequestEvent = 10112;
    public const uint WiredUserVariableUpdate64Event = 10110;
    public const uint WiredUserVariableManage64Event = 10111;
    public const uint WiredUserVariablesRequest64Event = 10113;
    public const uint WiredVariableHoldersRequest64Event = 10114;
    public const uint WiredVariableHoldersPage64Event = 10115;
    // Wired chests and wired trades; see reviews/chest-protocol-map.md.
    public const uint ChestOpenEvent = 1502;
    public const uint ChestCloseEvent = 3644;
    public const uint ChestStartDepositEvent = 3353;
    public const uint ChestWithdrawAllEvent = 2489;
    public const uint ChestWithdrawFurniEvent = 3495;
    public const uint ChestWithdrawCoinsEvent = 1703;
    public const uint ChestSaveOptionsEvent = 2202;
    public const uint ChestSavePreferencesEvent = 374;
    public const uint ChestSaveNotificationsEvent = 3109;
    public const uint WiredChestOfferItemsEvent = 642;
    public const uint WiredChestAcceptEvent = 1039;
    public const uint WiredChestCancelEvent = 402;
    public const uint ChestUpgradeEvent = 2044;
    public const uint WiredChestLockEvent = 2712;
    public const uint WiredTransactionGetRoomLogsEvent = 2517;
    public const uint WiredTransactionGetChestLogsEvent = 205;

    // October wired boxes.
    public const uint WiredWebApiKeyRequestEvent = 2332;
    public const uint WiredVariableShareSynchronizeEvent = 2482;
    public const uint WiredVariableLeaderboardSubscribeEvent = 3271;
    public const uint SaveWiredLabelTextEvent = 1837;
    public const uint SaveWiredLabelSettingsEvent = 3660;
    public const uint WiredLinkRequestConfigEvent = 957;
    public const uint WiredLinkSaveSelectionEvent = 2675;
    public const uint WiredLinkCreateNetworkEvent = 2063;
    public const uint WiredLinkRenameNetworkEvent = 1675;
    public const uint WiredLinkDeleteNetworkEvent = 593;
    public const uint WiredLinkGetConnectionsEvent = 1261;
    public const uint GetWiredContractContentsEvent = 153;
    public const uint UpdateWiredContractEvent = 1365;
    // Wired monitor. The monitor tab sends both requests together.
    public const uint GetWiredRoomStatsEvent = 1744;
    public const uint GetWiredErrorLogsEvent = 608;
    public const uint ClearWiredErrorLogsEvent = 3141;
    // The inspection tab's value edit, create and delete.
    public const uint WiredSetObjectVariableValueEvent = 2501;
    // The inspection tab's read of one object's variables.
    public const uint WiredGetVariablesForObjectEvent = 508;
    // The Variables tab's Highlight, Delete and Manage buttons.
    public const uint WiredGetAllVariableHoldersEvent = 1170;
    public const uint WiredDeleteAllVariableHoldersEvent = 2671;
    public const uint WiredGetVariableOwnersPageEvent = 3386;
    public const uint WiredGetUserPermanentVariablesEvent = 2233;
    public const uint WiredSetUserPermanentVariableEvent = 1969;

    // Free product claims (cursed dragon adoption) and the new-feature promo expiry.
    public const uint HasClaimedProductEvent = 3617;
    public const uint ClaimProductEvent = 2138;
    public const uint GetSecondsUntilEvent = 816;
}
