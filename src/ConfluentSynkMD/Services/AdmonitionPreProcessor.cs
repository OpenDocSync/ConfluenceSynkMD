using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ConfluentSynkMD.Services;

/// <summary>
/// Pre-processes MkDocs-style admonitions (!!! type "title") into
/// GitHub-style alerts (> [!TYPE]) before Markdig parsing.
/// This allows the existing QuoteBlockRenderer to convert them to
/// Confluence info/tip/note/warning macros.
/// </summary>
public static partial class AdmonitionPreProcessor
{
    /// <summary>Maps MkDocs admonition types to GitHub alert types.</summary>
    private static readonly Dictionary<string, string> TypeMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["info"] = "NOTE",
        ["note"] = "NOTE",
        ["tip"] = "TIP",
        ["hint"] = "TIP",
        ["important"] = "IMPORTANT",
        ["warning"] = "WARNING",
        ["danger"] = "CAUTION",
        ["caution"] = "CAUTION",
        ["attention"] = "WARNING",
        ["error"] = "CAUTION",
        ["example"] = "TIP",
        ["question"] = "NOTE",
        ["quote"] = "NOTE",
        ["abstract"] = "NOTE",
        ["summary"] = "NOTE",
        ["todo"] = "IMPORTANT",
        ["success"] = "TIP",
        ["check"] = "TIP",
        ["done"] = "TIP",
        ["fail"] = "CAUTION",
        ["failure"] = "CAUTION",
        ["bug"] = "CAUTION"
    };

    /// <summary>
    /// Converts MkDocs admonition blocks to GitHub alert syntax.
    /// Input:  !!! info "Pro Tip"
    ///             Use the watch mode to auto-update.
    /// Output: > [!NOTE]
    ///         > **Pro Tip**
    ///         > Use the watch mode to auto-update.
    /// </summary>
    public static string ConvertAdmonitions(string markdown)
    {
        var lines = markdown.Split('\n');
        var sb = new StringBuilder();
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];
            var match = AdmonitionHeaderRegex().Match(line);

            if (match.Success)
            {
                var admonType = match.Groups[1].Value;
                var title = match.Groups[2].Success ? match.Groups[2].Value.Trim('"').Trim('\'') : null;

                var alertType = TypeMapping.GetValueOrDefault(admonType, "NOTE");

                sb.AppendLine(CultureInfo.InvariantCulture, $"> [!{alertType}]");
                if (!string.IsNullOrWhiteSpace(title))
                    sb.AppendLine(CultureInfo.InvariantCulture, $"> **{title}**");

                // Consume indented body lines (4 spaces or 1 tab)
                i++;
                while (i < lines.Length)
                {
                    var bodyLine = lines[i];
                    if (bodyLine.StartsWith("    ", StringComparison.Ordinal) || bodyLine.StartsWith('\t'))
                    {
                        var content = bodyLine.StartsWith("    ", StringComparison.Ordinal)
                            ? bodyLine[4..]
                            : bodyLine[1..];
                        sb.AppendLine(CultureInfo.InvariantCulture, $"> {content}");
                        i++;
                    }
                    else if (string.IsNullOrWhiteSpace(bodyLine))
                    {
                        // Blank line could be inside admonition or end it
                        // Peek ahead to see if next line is also indented
                        if (i + 1 < lines.Length &&
                            (lines[i + 1].StartsWith("    ", StringComparison.Ordinal) || lines[i + 1].StartsWith('\t')))
                        {
                            sb.AppendLine(">");
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine(line);
                i++;
            }
        }

        return sb.ToString().TrimEnd('\n', '\r') + "\n";
    }

    // Matches: !!! type "optional title" or !!! type 'optional title' or !!! type
    [GeneratedRegex(@"^!!!\s+(\w+)(?:\s+[""']([^""']+)[""'])?\s*$")]
    private static partial Regex AdmonitionHeaderRegex();
}
