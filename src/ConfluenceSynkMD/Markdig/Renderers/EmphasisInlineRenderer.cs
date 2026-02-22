using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace ConfluenceSynkMD.Markdig.Renderers;

/// <summary>
/// Renders emphasis (bold, italic, strikethrough) as HTML tags.
/// Confluence supports standard &lt;strong&gt;, &lt;em&gt;, and &lt;del&gt; tags.
/// </summary>
public sealed class EmphasisInlineRenderer : MarkdownObjectRenderer<ConfluenceRenderer, EmphasisInline>
{
    protected override void Write(ConfluenceRenderer renderer, EmphasisInline emphasis)
    {
        // ==marked== text (from Markdig's UseMarked extension)
        if (emphasis.DelimiterChar == '=' && emphasis.DelimiterCount == 2)
        {
            MarkHighlightHelper.WriteHighlight(renderer, emphasis);
            return;
        }

        var tag = emphasis.DelimiterChar switch
        {
            '*' or '_' when emphasis.DelimiterCount == 2 => "strong",
            '*' or '_' => "em",
            '~' when emphasis.DelimiterCount == 2 => "del",
            _ => "em"
        };

        renderer.Write($"<{tag}>");
        renderer.WriteChildren(emphasis);
        renderer.Write($"</{tag}>");
    }
}
