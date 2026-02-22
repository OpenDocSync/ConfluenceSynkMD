using System.Text;
using System.Text.RegularExpressions;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace ConfluenceSynkMD.Markdig.Renderers;

/// <summary>
/// Renders headings as HTML heading tags with optional Confluence anchor macros
/// for deep-linking. Supports --skip-title-heading and --heading-anchors.
/// Mirrors md2conf's _transform_heading().
/// </summary>
public sealed partial class HeadingRenderer : MarkdownObjectRenderer<ConfluenceRenderer, HeadingBlock>
{
    protected override void Write(ConfluenceRenderer renderer, HeadingBlock heading)
    {
        var level = heading.Level;
        if (level < 1) level = 1;
        if (level > 6) level = 6;

        // --skip-title-heading: skip the first H1 heading (it becomes the page title)
        if (renderer.ConverterOptions.SkipTitleHeading && level == 1 && !renderer.FirstHeadingSeen)
        {
            renderer.FirstHeadingSeen = true;
            return;
        }

        if (level == 1)
            renderer.FirstHeadingSeen = true;

        // --heading-anchors: inject an anchor macro before the heading
        if (renderer.ConverterOptions.HeadingAnchors && heading.Inline is not null)
        {
            var headingText = ExtractPlainText(heading.Inline);
            var slug = GenerateSlug(headingText);
            renderer.Write("<ac:structured-macro ac:name=\"anchor\">");
            renderer.Write($"<ac:parameter ac:name=\"\">{EscapeXml(slug)}</ac:parameter>");
            renderer.Write("</ac:structured-macro>");
        }

        renderer.Write($"<h{level}>");
        renderer.WriteChildren(heading.Inline!);
        renderer.WriteLine($"</h{level}>");
    }

    private static string ExtractPlainText(ContainerInline inline)
    {
        var sb = new StringBuilder();
        foreach (var child in inline)
        {
            if (child is LiteralInline literal)
                sb.Append(literal.Content);
        }
        return sb.ToString();
    }

    private static string GenerateSlug(string text)
    {
        var slug = text.ToLowerInvariant().Trim();
        slug = SlugPattern().Replace(slug, "-");
        slug = slug.Trim('-');
        return slug;
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    [GeneratedRegex(@"[^\w]+")]
    private static partial Regex SlugPattern();
}
