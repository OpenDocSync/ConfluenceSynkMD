using Markdig.Extensions.Alerts;
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
        // GitHub-style alerts (> [!NOTE], > [!TIP], ...) — Markdig's Alerts
        // extension (loaded by UseAdvancedExtensions) parses these into a typed
        // AlertBlock subclass with a Kind property and consumes the "[!TYPE]"
        // marker line. We map Kind directly; the previous text-scanning approach
        // never fired because the marker was already gone by the time we ran.
        if (block is AlertBlock alertBlock)
        {
            var kind = alertBlock.Kind.ToString();
            if (!string.IsNullOrEmpty(kind)
                && AlertTypeMapping.TryGetValue(kind, out var alertMacro))
            {
                WriteAlertMacro(renderer, block, kind, alertMacro, skipFirstLine: false);
                return;
            }
        }

        // Try to detect GitLab-style alert: > FLAG: ... or > NOTE: ... etc.
        // Markdig has no extension for this so we still scan inline text.
        var (gitlabType, hasGitLabAlert) = DetectGitLabAlert(block);
        if (hasGitLabAlert && GitLabAlertMapping.TryGetValue(gitlabType!, out var gitlabMacro))
        {
            WriteAlertMacro(renderer, block, gitlabType!, gitlabMacro, skipFirstLine: false, gitlabPrefix: gitlabType!);
            return;
        }

        // Plain Markdown blockquote → standard HTML <blockquote>.
        // Earlier versions rewrote every plain quote to a Confluence "info" macro,
        // which round-tripped back as a "[!NOTE]" GitHub alert and silently changed
        // the source semantics ("> Just a quote" became "> [!NOTE]\n> Just a quote").
        // Confluence Storage Format accepts <blockquote> natively; the download path
        // converts it back to "> " prefixed Markdown lines, preserving fidelity.
        // --use-panel intentionally does NOT apply here — that flag governs how
        // explicit GitHub/GitLab alerts render, not how plain quotes render.
        renderer.Write("<blockquote>");
        renderer.WriteChildren(block);
        renderer.WriteLine("</blockquote>");
    }

    private static void WriteAlertMacro(
        ConfluenceRenderer renderer, QuoteBlock block,
        string alertType, string macroName,
        bool skipFirstLine, string? gitlabPrefix = null)
    {
        // skipFirstLine is no longer reachable: GitHub alerts are now detected via
        // Markdig's AlertBlock node type (which already strips the "[!TYPE]" line)
        // and GitLab alerts use gitlabPrefix. The parameter is kept for ABI stability
        // and to match the signature of WriteBlockContentSkippingGitLabPrefix.
        _ = skipFirstLine;

        var effectiveMacro = renderer.ConverterOptions.UsePanel ? "panel" : macroName;

        renderer.Write($"<ac:structured-macro ac:name=\"{effectiveMacro}\">");

        if (renderer.ConverterOptions.UsePanel)
        {
            renderer.Write($"<ac:parameter ac:name=\"title\">{alertType}</ac:parameter>");
        }

        renderer.Write("<ac:rich-text-body>");

        if (gitlabPrefix is not null)
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
