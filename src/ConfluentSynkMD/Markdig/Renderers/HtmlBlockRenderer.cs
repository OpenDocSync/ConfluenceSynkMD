using System.Text.RegularExpressions;
using Markdig.Renderers;
using Markdig.Syntax;

namespace ConfluentSynkMD.Markdig.Renderers;

/// <summary>
/// Passes through raw HTML blocks with special handling for:
/// - <c>&lt;!-- confluence-skip-start/end --&gt;</c> markers (suppress content)
/// - <c>&lt;details markdown="1"&gt;</c> collapsed sections (→ Confluence expand macro)
/// - <c>&lt;input type="date"&gt;</c> date widgets (→ <c>&lt;time&gt;</c>)
/// - <c>&lt;ins&gt;</c> normalization (→ <c>&lt;u&gt;</c>)
///
/// Mirrors md2conf's <c>_transform_collapsed()</c>, <c>transform_skip_comments_in_html()</c>,
/// and various inline transforms.
/// </summary>
public sealed partial class HtmlBlockRenderer : MarkdownObjectRenderer<ConfluenceRenderer, HtmlBlock>
{
    protected override void Write(ConfluenceRenderer renderer, HtmlBlock block)
    {
        var content = block.Lines.ToString() ?? "";

        // ── confluence-skip markers ─────────────────────────────────────────
        if (content.Contains("confluence-skip-start", StringComparison.OrdinalIgnoreCase))
        {
            renderer.SkipUntilEnd = true;
            return;
        }

        if (content.Contains("confluence-skip-end", StringComparison.OrdinalIgnoreCase))
        {
            renderer.SkipUntilEnd = false;
            return;
        }

        if (renderer.SkipUntilEnd)
            return;

        // ── <details> → Confluence expand macro ─────────────────────────────
        if (DetailsOpenRegex().IsMatch(content))
        {
            var summary = ExtractSummary(content);
            var body = ExtractDetailsBody(content);

            renderer.Write("<ac:structured-macro ac:name=\"expand\" ac:schema-version=\"1\">");
            if (!string.IsNullOrEmpty(summary))
            {
                renderer.Write($"<ac:parameter ac:name=\"title\">{EscapeXml(summary)}</ac:parameter>");
            }
            renderer.Write("<ac:rich-text-body>");
            renderer.Write(body);
            renderer.Write("</ac:rich-text-body>");
            renderer.WriteLine("</ac:structured-macro>");
            return;
        }

        // ── <input type="date"> → <time> ───────────────────────────────────
        content = DateInputRegex().Replace(content, m =>
        {
            var value = m.Groups["date"].Value;
            return $"<time datetime=\"{EscapeXml(value)}\"/>";
        });

        // ── <ins> → <u> normalization ───────────────────────────────────────
        content = content.Replace("<ins>", "<u>", StringComparison.OrdinalIgnoreCase);
        content = content.Replace("</ins>", "</u>", StringComparison.OrdinalIgnoreCase);

        renderer.Write(content);
    }

    /// <summary>Extracts the text inside <c>&lt;summary&gt;...&lt;/summary&gt;</c>.</summary>
    private static string ExtractSummary(string html)
    {
        var match = SummaryRegex().Match(html);
        return match.Success ? match.Groups["text"].Value.Trim() : string.Empty;
    }

    /// <summary>
    /// Extracts the body content between <c>&lt;/summary&gt;</c> and <c>&lt;/details&gt;</c>.
    /// </summary>
    private static string ExtractDetailsBody(string html)
    {
        var summaryEnd = SummaryEndRegex().Match(html);
        var detailsEnd = DetailsEndRegex().Match(html);

        if (!summaryEnd.Success || !detailsEnd.Success)
            return string.Empty;

        var bodyStart = summaryEnd.Index + summaryEnd.Length;
        var bodyEnd = detailsEnd.Index;

        return bodyEnd > bodyStart
            ? html[bodyStart..bodyEnd].Trim()
            : string.Empty;
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    [GeneratedRegex(@"<details(\s+[^>]*)?>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DetailsOpenRegex();

    [GeneratedRegex(@"<summary[^>]*>(?<text>.*?)</summary>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex SummaryRegex();

    [GeneratedRegex(@"</summary>", RegexOptions.IgnoreCase)]
    private static partial Regex SummaryEndRegex();

    [GeneratedRegex(@"</details>", RegexOptions.IgnoreCase)]
    private static partial Regex DetailsEndRegex();

    [GeneratedRegex(@"<input\s+type\s*=\s*""date""\s+value\s*=\s*""(?<date>[^""]+)""\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex DateInputRegex();
}
