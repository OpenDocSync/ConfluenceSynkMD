using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace ConfluenceSynkMD.Markdig.Renderers;

/// <summary>
/// Renders line breaks for Confluence Storage Format.
///
/// Hard break (two trailing spaces or trailing backslash in source) → <c>&lt;br/&gt;</c>.
/// Soft break (plain newline between two text lines inside a paragraph) → literal "\n".
///
/// The literal newline is required for round-trip fidelity. Without it, two adjacent
/// lines like:
///   <code>
///     This page was uploaded
///     by an automated test.
///   </code>
/// produce <c>&lt;p&gt;This page was uploadedby an automated test.&lt;/p&gt;</c> on
/// upload, and the download path faithfully reproduces "uploadedby" because that is
/// literally what the storage value contains. Emitting a "\n" preserves the soft
/// break across the round-trip; HTML/Storage Format renderers collapse it to a single
/// space at display time, matching the CommonMark spec for soft line breaks.
/// </summary>
public sealed class LineBreakInlineRenderer : MarkdownObjectRenderer<ConfluenceRenderer, LineBreakInline>
{
    protected override void Write(ConfluenceRenderer renderer, LineBreakInline lineBreak)
    {
        if (lineBreak.IsHard)
            renderer.Write("<br/>");
        else
            renderer.Write("\n");
    }
}
