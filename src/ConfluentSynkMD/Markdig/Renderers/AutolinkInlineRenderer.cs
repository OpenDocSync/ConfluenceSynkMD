using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace ConfluentSynkMD.Markdig.Renderers;

/// <summary>Renders auto-links as standard &lt;a&gt; tags.</summary>
public sealed class AutolinkInlineRenderer : MarkdownObjectRenderer<ConfluenceRenderer, AutolinkInline>
{
    protected override void Write(ConfluenceRenderer renderer, AutolinkInline autolink)
    {
        var url = autolink.IsEmail ? $"mailto:{autolink.Url}" : autolink.Url;
        renderer.Write($"<a href=\"{EscapeAttr(url)}\">{EscapeXml(autolink.Url)}</a>");
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string EscapeAttr(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\"", "&quot;");
}
