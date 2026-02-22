using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace ConfluentSynkMD.Markdig.Renderers;

/// <summary>
/// Renders paragraphs as &lt;p&gt; tags.
/// Detects <c>[[_TOC_]]</c> and <c>[[_LISTING_]]</c> placeholders and emits the
/// corresponding Confluence structured macros instead.
/// Mirrors md2conf's <c>is_placeholder_for()</c> / <c>_transform_toc()</c> / <c>_transform_listing()</c>.
/// </summary>
public sealed class ParagraphRenderer : MarkdownObjectRenderer<ConfluenceRenderer, ParagraphBlock>
{
    protected override void Write(ConfluenceRenderer renderer, ParagraphBlock paragraph)
    {
        // Detect [[_TOC_]] or [[_LISTING_]] placeholders.
        // Markdig parses `[[_TOC_]]` as: LiteralInline("[[") + EmphasisInline("TOC") + LiteralInline("]]")
        if (TryDetectPlaceholder(paragraph, out var placeholderName))
        {
            if (placeholderName == "TOC")
            {
                // <ac:structured-macro ac:name="toc">
                renderer.Write("<ac:structured-macro ac:name=\"toc\" ac:schema-version=\"1\" data-layout=\"default\">");
                renderer.Write("<ac:parameter ac:name=\"outline\">clear</ac:parameter>");
                renderer.Write("<ac:parameter ac:name=\"style\">default</ac:parameter>");
                renderer.WriteLine("</ac:structured-macro>");
                return;
            }

            if (placeholderName == "LISTING")
            {
                // <ac:structured-macro ac:name="children">
                renderer.Write("<ac:structured-macro ac:name=\"children\" ac:schema-version=\"2\" data-layout=\"default\">");
                renderer.Write("<ac:parameter ac:name=\"allChildren\">true</ac:parameter>");
                renderer.WriteLine("</ac:structured-macro>");
                return;
            }
        }

        // Default: render as <p>
        renderer.Write("<p>");
        renderer.WriteChildren(paragraph.Inline!);
        renderer.WriteLine("</p>");
    }

    /// <summary>
    /// Detects whether a paragraph represents a <c>[[_NAME_]]</c> placeholder.
    /// Markdig parses <c>[[_TOC_]]</c> as:
    ///   LiteralInline("[[") → EmphasisInline containing LiteralInline("TOC") → LiteralInline("]]")
    /// </summary>
    private static bool TryDetectPlaceholder(ParagraphBlock paragraph, out string name)
    {
        name = string.Empty;

        if (paragraph.Inline is null)
            return false;

        // Collect inline children
        var inlines = new List<Inline>();
        var inline = paragraph.Inline.FirstChild;
        while (inline is not null)
        {
            inlines.Add(inline);
            inline = inline.NextSibling;
        }

        // We expect exactly 3 inlines: Literal("[["), Emphasis("NAME"), with tail text "]]"
        if (inlines.Count < 2)
            return false;

        // First inline: Literal starting with "[["
        if (inlines[0] is not LiteralInline firstLiteral)
            return false;
        var firstText = firstLiteral.Content.ToString().TrimEnd();
        if (firstText != "[[")
            return false;

        // Second inline: EmphasisInline containing the name
        if (inlines[1] is not EmphasisInline emphasis)
            return false;

        // Extract text from emphasis
        var emphasisChild = emphasis.FirstChild;
        if (emphasisChild is not LiteralInline emphasisLiteral)
            return false;
        var emphasisText = emphasisLiteral.Content.ToString().Trim();

        // The "]]" should be the tail of the emphasis inline (or a third literal)
        // Markdig typically stores the "]]" as the next sibling's content or as part of emphasis tail
        if (inlines.Count == 3 && inlines[2] is LiteralInline lastLiteral)
        {
            if (lastLiteral.Content.ToString().TrimStart() != "]]")
                return false;
        }
        else
        {
            // Check if "]]" is in the tail text after emphasis
            // In some Markdig versions, the emphasis has no tail and "]]" becomes the next literal
            return false;
        }

        if (emphasisText is "TOC" or "LISTING")
        {
            name = emphasisText;
            return true;
        }

        return false;
    }
}
