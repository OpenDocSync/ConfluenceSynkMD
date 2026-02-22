using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace ConfluentSynkMD.Markdig.Renderers;

/// <summary>Passes through raw inline HTML.</summary>
public sealed class HtmlInlineRenderer : MarkdownObjectRenderer<ConfluenceRenderer, HtmlInline>
{
    protected override void Write(ConfluenceRenderer renderer, HtmlInline html)
    {
        renderer.Write(html.Tag);
    }
}
