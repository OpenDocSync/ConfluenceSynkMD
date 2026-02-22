using Markdig.Renderers;
using Markdig.Syntax;

namespace ConfluentSynkMD.Markdig.Renderers;

/// <summary>Renders thematic breaks (---) as &lt;hr/&gt;.</summary>
public sealed class ThematicBreakRenderer : MarkdownObjectRenderer<ConfluenceRenderer, ThematicBreakBlock>
{
    protected override void Write(ConfluenceRenderer renderer, ThematicBreakBlock block)
    {
        renderer.WriteLine("<hr/>");
    }
}
