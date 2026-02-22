using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace ConfluentSynkMD.Markdig.Renderers;

/// <summary>Renders HTML entities as-is.</summary>
public sealed class HtmlEntityInlineRenderer : MarkdownObjectRenderer<ConfluenceRenderer, HtmlEntityInline>
{
    protected override void Write(ConfluenceRenderer renderer, HtmlEntityInline entity)
    {
        renderer.Write(entity.Transcoded.ToString());
    }
}
