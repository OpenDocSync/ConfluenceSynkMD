using Markdig.Extensions.Emoji;
using Markdig.Renderers;

namespace ConfluenceSynkMD.Markdig.Renderers;

/// <summary>
/// Renders emoji shortcodes as Confluence <c>ac:emoticon</c> elements.
/// Mirrors md2conf's <c>_transform_emoji()</c>.
///
/// Input:  <c>:wink:</c>
/// Output: <c>&lt;ac:emoticon ac:name="wink" ac:emoji-shortname=":wink:" ac:emoji-id="1f609" ac:emoji-fallback="&#128521;"/&gt;</c>
/// </summary>
public sealed class EmojiInlineRenderer : MarkdownObjectRenderer<ConfluenceRenderer, EmojiInline>
{
    /// <summary>Maps common emoji shortnames to Confluence emoticon names.</summary>
    private static readonly Dictionary<string, string> EmoticonMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        // Standard Confluence emoticons
        ["smile"] = "smile",
        ["sad"] = "sad",
        ["tongue"] = "cheeky",
        ["wink"] = "wink",
        ["thumbsup"] = "thumbs-up",
        ["thumbs_up"] = "thumbs-up",
        ["+1"] = "thumbs-up",
        ["thumbsdown"] = "thumbs-down",
        ["thumbs_down"] = "thumbs-down",
        ["-1"] = "thumbs-down",
        ["information_source"] = "information",
        ["white_check_mark"] = "tick",
        ["x"] = "cross",
        ["warning"] = "warning",
        ["star"] = "yellow-star",
        ["heart"] = "heart",
        ["broken_heart"] = "broken-heart",
        ["bulb"] = "light-on",
        ["question"] = "question",
        ["exclamation"] = "warning",
        ["laughing"] = "laugh",
    };

    protected override void Write(ConfluenceRenderer renderer, EmojiInline emoji)
    {
        var shortname = emoji.Content.ToString().Trim(':');
        var match = emoji.Match;

        // Get the Confluence emoticon name (or fall back to "blue-star")
        var emoticonName = EmoticonMapping.GetValueOrDefault(shortname, "blue-star");

        // Get the unicode representation
        var unicode = match is not null ? GetUnicodeHex(match) : "";

        // Build emoji fallback from the match character
        var fallback = match ?? $":{shortname}:";

        renderer.Write($"<ac:emoticon ac:name=\"{EscapeAttr(emoticonName)}\"");
        renderer.Write($" ac:emoji-shortname=\":{EscapeAttr(shortname)}:\"");
        if (!string.IsNullOrEmpty(unicode))
        {
            renderer.Write($" ac:emoji-id=\"{EscapeAttr(unicode)}\"");
        }
        renderer.Write($" ac:emoji-fallback=\"{EscapeAttr(fallback)}\"");
        renderer.Write("/>");
    }

    private static string GetUnicodeHex(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var parts = new List<string>();
        foreach (var ch in text.EnumerateRunes())
        {
            parts.Add($"{ch.Value:x}");
        }
        return string.Join("-", parts);
    }

    private static string EscapeAttr(string text) =>
        text.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
}
