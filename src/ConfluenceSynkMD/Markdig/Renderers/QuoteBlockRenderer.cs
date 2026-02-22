using Markdig.Renderers;
using Markdig.Syntax;

namespace ConfluenceSynkMD.Markdig.Renderers;

/// <summary>
/// Renders blockquotes, detecting GitHub/GitLab alert syntax and converting
/// to Confluence info/tip/note/warning macros.
/// Mirrors md2conf's _transform_alert(), _transform_github_alert(),
/// _transform_gitlab_alert(), and _transform_admonition().
///
/// Input:  &gt; [!NOTE]
///         &gt; Content here
///
/// Output: &lt;ac:structured-macro ac:name="info"&gt;
///           &lt;ac:rich-text-body&gt;Content here&lt;/ac:rich-text-body&gt;
///         &lt;/ac:structured-macro&gt;
/// </summary>
public sealed class QuoteBlockRenderer : MarkdownObjectRenderer<ConfluenceRenderer, QuoteBlock>
{
    /// <summary>Maps GitHub alert types to Confluence macro names.</summary>
    private static readonly Dictionary<string, string> AlertTypeMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NOTE"] = "info",
        ["TIP"] = "tip",
        ["IMPORTANT"] = "note",
        ["WARNING"] = "warning",
        ["CAUTION"] = "warning"
    };

    /// <summary>Maps GitLab alert prefixes to Confluence macro names.</summary>
    private static readonly Dictionary<string, string> GitLabAlertMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FLAG"] = "note",
        ["NOTE"] = "info",
        ["WARNING"] = "note",
        ["DISCLAIMER"] = "info"
    };

    protected override void Write(ConfluenceRenderer renderer, QuoteBlock block)
    {
        // Try to detect GitHub-style alert: > [!TYPE]
        var (alertType, hasAlert) = DetectGitHubAlert(block);

        if (hasAlert && AlertTypeMapping.TryGetValue(alertType!, out var macroName))
        {
            WriteAlertMacro(renderer, block, alertType!, macroName, skipFirstLine: true);
            return;
        }

        // Try to detect GitLab-style alert: > FLAG: ... or > NOTE: ... etc.
        var (gitlabType, hasGitLabAlert) = DetectGitLabAlert(block);
        if (hasGitLabAlert && GitLabAlertMapping.TryGetValue(gitlabType!, out var gitlabMacro))
        {
            WriteAlertMacro(renderer, block, gitlabType!, gitlabMacro, skipFirstLine: false, gitlabPrefix: gitlabType!);
            return;
        }

        // Regular blockquote → Confluence info macro (or panel with --use-panel)
        var fallbackMacro = renderer.ConverterOptions.UsePanel ? "panel" : "info";
        renderer.Write($"<ac:structured-macro ac:name=\"{fallbackMacro}\">");
        renderer.Write("<ac:rich-text-body>");
        renderer.WriteChildren(block);
        renderer.Write("</ac:rich-text-body>");
        renderer.WriteLine("</ac:structured-macro>");
    }

    private static void WriteAlertMacro(
        ConfluenceRenderer renderer, QuoteBlock block,
        string alertType, string macroName,
        bool skipFirstLine, string? gitlabPrefix = null)
    {
        var effectiveMacro = renderer.ConverterOptions.UsePanel ? "panel" : macroName;

        renderer.Write($"<ac:structured-macro ac:name=\"{effectiveMacro}\">");

        if (renderer.ConverterOptions.UsePanel)
        {
            renderer.Write($"<ac:parameter ac:name=\"title\">{alertType}</ac:parameter>");
        }

        renderer.Write("<ac:rich-text-body>");

        if (skipFirstLine)
        {
            WriteBlockContentSkippingAlert(renderer, block);
        }
        else if (gitlabPrefix is not null)
        {
            WriteBlockContentSkippingGitLabPrefix(renderer, block, gitlabPrefix);
        }
        else
        {
            renderer.WriteChildren(block);
        }

        renderer.Write("</ac:rich-text-body>");
        renderer.WriteLine("</ac:structured-macro>");
    }

    private static (string? AlertType, bool HasAlert) DetectGitHubAlert(QuoteBlock block)
    {
        if (block.Count == 0) return (null, false);

        // First child should be a ParagraphBlock
        if (block[0] is not ParagraphBlock paragraph) return (null, false);
        if (paragraph.Inline is null) return (null, false);

        var firstText = paragraph.Inline.FirstChild?.ToString() ?? "";

        // Match [!TYPE] pattern
        if (firstText.StartsWith("[!", StringComparison.Ordinal) && firstText.Contains(']'))
        {
            var endBracket = firstText.IndexOf(']');
            var alertType = firstText[2..endBracket].Trim();
            return (alertType, !string.IsNullOrEmpty(alertType));
        }

        return (null, false);
    }

    private static void WriteBlockContentSkippingAlert(ConfluenceRenderer renderer, QuoteBlock block)
    {
        for (var i = 0; i < block.Count; i++)
        {
            var child = block[i];

            if (i == 0 && child is ParagraphBlock firstParagraph && firstParagraph.Inline is not null)
            {
                // Skip the [!TYPE] part from the first paragraph
                var text = firstParagraph.Inline.FirstChild?.ToString() ?? "";
                if (text.StartsWith("[!", StringComparison.Ordinal) && text.Contains(']'))
                {
                    // Write remaining content after the alert marker
                    var remaining = text[(text.IndexOf(']') + 1)..].TrimStart();
                    if (!string.IsNullOrEmpty(remaining))
                    {
                        renderer.Write($"<p>{remaining}");
                    }
                    else
                    {
                        renderer.Write("<p>");
                    }

                    // Write remaining inlines
                    var inline = firstParagraph.Inline.FirstChild?.NextSibling;
                    while (inline is not null)
                    {
                        renderer.Write(inline);
                        inline = inline.NextSibling;
                    }
                    renderer.Write("</p>");
                    continue;
                }
            }

            renderer.Write(child);
        }
    }

    private static (string? AlertType, bool HasAlert) DetectGitLabAlert(QuoteBlock block)
    {
        if (block.Count == 0) return (null, false);
        if (block[0] is not ParagraphBlock paragraph) return (null, false);
        if (paragraph.Inline is null) return (null, false);

        var firstText = paragraph.Inline.FirstChild?.ToString() ?? "";

        // Match PREFIX: pattern (e.g. "FLAG: ...", "NOTE: ...", "WARNING: ...", "DISCLAIMER: ...")
        foreach (var prefix in GitLabAlertMapping.Keys)
        {
            if (firstText.StartsWith($"{prefix}:", StringComparison.OrdinalIgnoreCase))
            {
                return (prefix, true);
            }
        }

        return (null, false);
    }

    private static void WriteBlockContentSkippingGitLabPrefix(ConfluenceRenderer renderer, QuoteBlock block, string prefix)
    {
        for (var i = 0; i < block.Count; i++)
        {
            var child = block[i];

            if (i == 0 && child is ParagraphBlock firstParagraph && firstParagraph.Inline is not null)
            {
                var text = firstParagraph.Inline.FirstChild?.ToString() ?? "";
                if (text.StartsWith($"{prefix}:", StringComparison.OrdinalIgnoreCase))
                {
                    // Strip the "PREFIX:" part and write remaining text
                    var remaining = text[(prefix.Length + 1)..].TrimStart();
                    if (!string.IsNullOrEmpty(remaining))
                    {
                        renderer.Write($"<p>{remaining}");
                    }
                    else
                    {
                        renderer.Write("<p>");
                    }

                    var inline = firstParagraph.Inline.FirstChild?.NextSibling;
                    while (inline is not null)
                    {
                        renderer.Write(inline);
                        inline = inline.NextSibling;
                    }
                    renderer.Write("</p>");
                    continue;
                }
            }

            renderer.Write(child);
        }
    }
}
