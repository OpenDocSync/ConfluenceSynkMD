using System.Globalization;
using Markdig.Extensions.Footnotes;
using Markdig.Renderers;

namespace ConfluenceSynkMD.Markdig.Renderers;

/// <summary>
/// Renders footnote references and definitions using Confluence anchor/link macros.
/// Mirrors md2conf's <c>_transform_footnote_ref()</c> and <c>_transform_footnote_def()</c>.
///
/// References: <c>[^name]</c> → anchor macro + link to definition.
/// Definitions: <c>[^name]: text</c> → anchor macro + back-links to each reference.
///
/// Multi-reference support: when a footnote is referenced multiple times,
/// each reference gets a unique anchor and the definition has multiple back-links
/// with superscript numbering (↩¹ ↩² etc.).
/// </summary>
public sealed class FootnoteGroupRenderer : MarkdownObjectRenderer<ConfluenceRenderer, FootnoteGroup>
{
    private static readonly string[] SuperscriptDigits = ["⁰", "¹", "²", "³", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹"];

    protected override void Write(ConfluenceRenderer renderer, FootnoteGroup group)
    {
        renderer.Write("<hr/>");
        renderer.WriteLine("<ol>");

        foreach (var footnote in group)
        {
            if (footnote is not Footnote fn) continue;

            var name = fn.Label ?? fn.Order.ToString(CultureInfo.InvariantCulture);

            // Anchor for this definition
            renderer.Write("<li>");
            renderer.Write($"<ac:structured-macro ac:name=\"anchor\" ac:schema-version=\"1\">");
            renderer.Write($"<ac:parameter ac:name=\"\">footnote-def-{EscapeXml(name)}</ac:parameter>");
            renderer.Write("</ac:structured-macro>");

            // Render footnote body content
            renderer.WriteChildren(fn);

            // Back-links to each reference
            var linkCount = fn.Links.Count;
            for (var i = 0; i < linkCount; i++)
            {
                string anchorName;
                string linkText;

                if (linkCount == 1)
                {
                    anchorName = $"footnote-ref-{name}";
                    linkText = "↩";
                }
                else
                {
                    anchorName = i == 0
                        ? $"footnote-ref-{name}"
                        : $"footnote-ref-{name}-{i + 1}";
                    linkText = $"↩{ToSuperscript(i + 1)}";
                }

                renderer.Write(" ");
                renderer.Write($"<ac:link ac:anchor=\"{EscapeXml(anchorName)}\">");
                renderer.Write($"<ac:link-body><![CDATA[{linkText}]]></ac:link-body>");
                renderer.Write("</ac:link>");
            }

            renderer.WriteLine("</li>");
        }

        renderer.WriteLine("</ol>");
    }

    private static string ToSuperscript(int number)
    {
        var digits = number.ToString(CultureInfo.InvariantCulture);
        var result = new char[digits.Length];
        for (var i = 0; i < digits.Length; i++)
        {
            var d = digits[i] - '0';
            // SuperscriptDigits are strings, take first char
            result[i] = SuperscriptDigits[d][0];
        }
        return new string(result);
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\"", "&quot;");
}

/// <summary>
/// Renders inline footnote references (<c>[^name]</c>) as anchor + link macros.
/// </summary>
public sealed class FootnoteLinkRenderer : MarkdownObjectRenderer<ConfluenceRenderer, FootnoteLink>
{
    protected override void Write(ConfluenceRenderer renderer, FootnoteLink link)
    {
        if (link.IsBackLink)
        {
            // Back-links are handled by FootnoteGroupRenderer
            return;
        }

        var footnote = link.Footnote;
        var name = footnote.Label ?? footnote.Order.ToString(CultureInfo.InvariantCulture);
        var index = link.Index;

        // Unique anchor for this reference
        var refAnchorName = index <= 1
            ? $"footnote-ref-{name}"
            : $"footnote-ref-{name}-{index}";

        // Anchor macro for this reference point
        renderer.Write("<sup>");
        renderer.Write($"<ac:structured-macro ac:name=\"anchor\" ac:schema-version=\"1\">");
        renderer.Write($"<ac:parameter ac:name=\"\">{EscapeXml(refAnchorName)}</ac:parameter>");
        renderer.Write("</ac:structured-macro>");

        // Link to the definition
        renderer.Write($"<ac:link ac:anchor=\"footnote-def-{EscapeXml(name)}\">");
        renderer.Write($"<ac:link-body><![CDATA[{footnote.Order}]]></ac:link-body>");
        renderer.Write("</ac:link>");
        renderer.Write("</sup>");
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\"", "&quot;");
}
