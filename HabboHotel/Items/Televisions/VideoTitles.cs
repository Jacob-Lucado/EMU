using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Plus.HabboHotel.Items.Televisions;

public static class VideoTitles
{
    public const int MaxLength = 120;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(4) };

    // Returns the video's real title, or null when YouTube does not answer.
    public static async Task<string?> Fetch(string videoId)
    {
        try {
            var info = await Http.GetFromJsonAsync<OEmbed>($"https://www.youtube.com/oembed?format=json&url=https://youtu.be/{videoId}");

            return string.IsNullOrWhiteSpace(info?.Title) ? null : Shorten(info.Title.Trim());
        }
        catch (Exception) {
            return null;
        }
    }

    public static string Shorten(string title) => title.Length > MaxLength ? title[..(MaxLength - 1)] + "…" : title;

    private sealed record OEmbed([property: JsonPropertyName("title")] string? Title);
}
