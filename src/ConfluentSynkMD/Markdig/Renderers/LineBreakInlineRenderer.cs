using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace ConfluentSynkMD.Markdig.Renderers;

/// <summary>Renders line breaks as &lt;br/&gt; (hard break) or nothing (soft break).</summary>
public sealed class LineBreakInlineRenderer : MarkdownObjectRenderer<ConfluenceRenderer, LineBreakInline>
{
    protected override void Write(ConfluenceRenderer renderer, LineBreakInline lineBreak)
    {
        if (lineBreak.IsHard)
            renderer.Write("<br/>");
    }
}
