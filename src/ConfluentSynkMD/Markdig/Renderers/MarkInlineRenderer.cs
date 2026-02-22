using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace ConfluentSynkMD.Markdig.Renderers;

/// <summary>
/// Renders <c>==highlighted text==</c> (Markdig's marked extension) as a
/// styled span with a highlight background color.
/// Mirrors md2conf's <c>_transform_mark()</c>.
///
/// Input:  <c>==important==</c>
/// Output: <c>&lt;span style="background-color: rgb(254,222,200);"&gt;important&lt;/span&gt;</c>
/// </summary>
/// <remarks>
/// Markdig renders marked text as <c>EmphasisInline</c> with <c>DelimiterChar == '='</c>.
/// This renderer is registered as an override for that specific case in the
/// <see cref="EmphasisInlineRenderer"/> (which delegates here when it detects '=').
/// Alternatively, this can handle a custom inline object if the Markdig pipeline
/// is configured differently.
/// </remarks>
public static class MarkHighlightHelper
{
    /// <summary>
    /// Writes a highlighted span to the renderer.
    /// Called from <see cref="EmphasisInlineRenderer"/> when it detects a mark emphasis ('=').
    /// </summary>
    public static void WriteHighlight(ConfluenceRenderer renderer, EmphasisInline emphasis)
    {
        renderer.Write("<span style=\"background-color: rgb(254,222,200);\">");
        renderer.WriteChildren(emphasis);
        renderer.Write("</span>");
    }
}
