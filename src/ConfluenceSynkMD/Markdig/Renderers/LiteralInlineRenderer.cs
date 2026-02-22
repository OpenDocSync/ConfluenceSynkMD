using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace ConfluenceSynkMD.Markdig.Renderers;

/// <summary>Renders literal text, escaping XML special characters.</summary>
public sealed class LiteralInlineRenderer : MarkdownObjectRenderer<ConfluenceRenderer, LiteralInline>
{
    protected override void Write(ConfluenceRenderer renderer, LiteralInline literal)
    {
        renderer.Write(EscapeXml(literal.Content.ToString()));
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
