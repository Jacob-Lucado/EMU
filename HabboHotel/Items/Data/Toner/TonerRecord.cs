namespace Plus.HabboHotel.Items.Data.Toner;

public sealed class TonerRecord
{
    public static TonerRecord New { get; } = new() { Enabled = false, Hue = 0, Saturation = 0, Lightness = 0 };

    public bool Enabled { get; init; }
    public int Hue { get; init; }
    public int Saturation { get; init; }
    public int Lightness { get; init; }
}
