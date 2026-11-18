namespace Plus.HabboHotel.Items.Data.Toner;

public sealed class TonerRecord
{
    public static TonerRecord New { get; } = new() { Enabled = false, Hue = 127, Saturation = 127, Lightness = 127 };

    public bool Enabled { get; init; }
    public int Hue { get; init; }
    public int Saturation { get; init; }
    public int Lightness { get; init; }

    /// <summary>The four values as items.extra_data stores them, so an inventory toner keeps its data.</summary>
    public string Serialize() => $"{(Enabled ? 1 : 0)}\n{Hue}\n{Saturation}\n{Lightness}";
}
