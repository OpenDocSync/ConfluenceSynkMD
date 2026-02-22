using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace ConfluenceSynkMD.Markdig.Renderers;

/// <summary>Renders inline code as &lt;code&gt; tags.</summary>
public sealed class CodeInlineRenderer : MarkdownObjectRenderer<ConfluenceRenderer, CodeInline>
{
    protected override void Write(ConfluenceRenderer renderer, CodeInline code)
    {
        renderer.Write("<code>");
        renderer.Write(EscapeXml(code.Content));
        renderer.Write("</code>");
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
