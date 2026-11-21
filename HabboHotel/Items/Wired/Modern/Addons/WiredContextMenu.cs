namespace Plus.HabboHotel.Items.Wired.Modern.Addons;

/// <summary>One item of a room unit's context menu as one viewer sees it; the text has its placeholders filled.</summary>
public sealed record WiredContextMenuEntry(string ButtonId, string Text, bool Enabled);
