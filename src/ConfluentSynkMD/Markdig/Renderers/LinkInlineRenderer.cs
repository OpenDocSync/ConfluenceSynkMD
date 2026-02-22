using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace ConfluentSynkMD.Markdig.Renderers;

/// <summary>
/// Renders links:
///   - External URLs → &lt;a href="..."&gt; tags
///   - Internal .md links → Confluence ac:link macros (or web-UI URLs with --webui-links)
///   - Images → ac:image macros with optional layout alignment and max-width
///
/// Supports --force-valid-url, --prefer-raster, --webui-links, and
/// layout-image-alignment / layout-image-max-width options.
/// Mirrors md2conf's _transform_link() and _transform_image().
/// </summary>
public sealed partial class LinkInlineRenderer : MarkdownObjectRenderer<ConfluenceRenderer, LinkInline>
{
    protected override void Write(ConfluenceRenderer renderer, LinkInline link)
    {
        if (link.IsImage)
        {
            WriteImage(renderer, link);
        }
        else
        {
            WriteLink(renderer, link);
        }
    }

    /// <summary>Pre-computed urn:uuid: mappings for status label SVG circles (6 colors).</summary>
    private static readonly Dictionary<string, string> StatusImageUrns = BuildStatusImageUrns();

    private static Dictionary<string, string> BuildStatusImageUrns()
    {
        var colors = new[] { "gray", "purple", "blue", "red", "yellow", "green" };
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var color in colors)
        {
            var svg = $"<svg height=\"10\" width=\"10\" xmlns=\"http://www.w3.org/2000/svg\"><circle r=\"5\" cx=\"5\" cy=\"5\" fill=\"{color}\" /></svg>";
#pragma warning disable CA5350 // SHA1 used for deterministic URN generation, not for security
            var sha1 = System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes(svg));
#pragma warning restore CA5350
            var guid = new Guid(sha1[..16]);
            var urn = $"urn:uuid:{guid}";
            dict[urn] = color;
        }
        return dict;
    }

    private static void WriteImage(ConfluenceRenderer renderer, LinkInline link)
    {
        var url = link.Url ?? string.Empty;
        var alt = link.FirstChild?.ToString() ?? string.Empty;

        // ── Status labels ───────────────────────────────────────────────
        // ![Caption](urn:uuid:...) where the URN matches a pre-computed status color
        if (url.StartsWith("urn:uuid:", StringComparison.OrdinalIgnoreCase)
            && StatusImageUrns.TryGetValue(url, out var statusColor)
            && !string.IsNullOrEmpty(alt))
        {
            var macroId = Guid.NewGuid().ToString();
            renderer.Write($"<ac:structured-macro ac:name=\"status\" ac:schema-version=\"1\" ac:macro-id=\"{macroId}\">");
            if (!statusColor.Equals("gray", StringComparison.OrdinalIgnoreCase))
            {
                var titleCaseColor = char.ToUpper(statusColor[0], CultureInfo.InvariantCulture) + statusColor[1..];
                renderer.Write($"<ac:parameter ac:name=\"colour\">{EscapeXml(titleCaseColor)}</ac:parameter>");
            }
            renderer.Write($"<ac:parameter ac:name=\"title\">{EscapeXml(alt)}</ac:parameter>");
            renderer.Write("</ac:structured-macro>");
            return;
        }

        var isExternal = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                         || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        // Build ac:image attributes
        var imageAttrs = new StringBuilder();

        // Layout: image alignment
        var alignment = renderer.LayoutOptions.ImageAlignment;
        if (!string.IsNullOrEmpty(alignment)
            && !alignment.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            imageAttrs.Append(CultureInfo.InvariantCulture, $" ac:align=\"{EscapeAttr(alignment)}\"");
        }

        // Layout: image max width
        var maxWidth = renderer.LayoutOptions.ImageMaxWidth;
        if (maxWidth.HasValue && maxWidth.Value > 0)
        {
            imageAttrs.Append(CultureInfo.InvariantCulture, $" ac:width=\"{maxWidth.Value}\"");
        }

        // Add alt text as title
        if (!string.IsNullOrEmpty(alt))
        {
            imageAttrs.Append(CultureInfo.InvariantCulture, $" ac:alt=\"{EscapeAttr(alt)}\"");
        }

        if (isExternal)
        {
            renderer.Write($"<ac:image{imageAttrs}>");
            renderer.Write($"<ri:url ri:value=\"{EscapeAttr(url)}\"/>");
            renderer.Write("</ac:image>");
        }
        else
        {
            // --prefer-raster: swap .svg to .png if the flag is set
            if (renderer.ConverterOptions.PreferRaster
                && url.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                url = url[..^4] + ".png";
            }

            var filename = Path.GetFileName(url);
            renderer.ReferencedImages.Add((filename, url));
            renderer.Write($"<ac:image{imageAttrs}>");
            renderer.Write($"<ri:attachment ri:filename=\"{EscapeAttr(filename)}\"/>");
            renderer.Write("</ac:image>");
        }
    }

    private static void WriteLink(ConfluenceRenderer renderer, LinkInline link)
    {
        var url = link.Url ?? string.Empty;

        // Apply --force-valid-url: sanitize the URL
        if (renderer.ConverterOptions.ForceValidUrl)
        {
            url = SanitizeUrl(url);
        }

        var resolver = renderer.LinkResolver;
        if (resolver is null)
        {
            // No resolver available — use legacy filename-based rendering
            WriteLinkLegacy(renderer, link, url);
            return;
        }

        var resolution = resolver.Resolve(
            url,
            renderer.ConverterOptions.WebUiLinks,
            renderer.CurrentDocumentPath);

        switch (resolution.Type)
        {
            case Services.LinkType.External:
                renderer.Write($"<a href=\"{EscapeAttr(url)}\">");
                renderer.WriteChildren(link);
                renderer.Write("</a>");
                break;

            case Services.LinkType.InternalPage when renderer.ConverterOptions.WebUiLinks:
                renderer.Write($"<a href=\"{EscapeAttr(resolution.DisplayUrl!)}\">");
                renderer.WriteChildren(link);
                renderer.Write("</a>");
                break;

            case Services.LinkType.InternalPage:
                renderer.Write("<ac:link>");
                renderer.Write($"<ri:page ri:content-title=\"{EscapeAttr(resolution.ResolvedTitle)}\"/>");
                if (resolution.Fragment is not null && resolution.Fragment.Length > 0)
                {
                    renderer.Write($"<ac:link-body>{EscapeXml(resolution.Fragment)}</ac:link-body>");
                }
                else
                {
                    renderer.Write("<ac:link-body>");
                    renderer.WriteChildren(link);
                    renderer.Write("</ac:link-body>");
                }
                renderer.Write("</ac:link>");
                break;

            case Services.LinkType.Anchor:
                renderer.Write($"<a href=\"{EscapeAttr(url)}\">");
                renderer.WriteChildren(link);
                renderer.Write("</a>");
                break;

            case Services.LinkType.Attachment:
                renderer.Write("<ac:link>");
                renderer.Write($"<ri:attachment ri:filename=\"{EscapeAttr(resolution.ResolvedTitle)}\"/>");
                renderer.Write("<ac:link-body>");
                renderer.WriteChildren(link);
                renderer.Write("</ac:link-body>");
                renderer.Write("</ac:link>");
                break;
        }
    }

    /// <summary>Legacy link rendering when no ILinkResolver is available.</summary>
    private static void WriteLinkLegacy(ConfluenceRenderer renderer, LinkInline link, string url)
    {
        var isExternal = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                         || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                         || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);

        var isMdLink = url.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                       || url.Contains(".md#", StringComparison.OrdinalIgnoreCase);

        if (isExternal)
        {
            renderer.Write($"<a href=\"{EscapeAttr(url)}\">");
            renderer.WriteChildren(link);
            renderer.Write("</a>");
        }
        else if (isMdLink)
        {
            var parts = url.Split('#', 2);
            var pageName = Path.GetFileNameWithoutExtension(parts[0]);

            renderer.Write("<ac:link>");
            renderer.Write($"<ri:page ri:content-title=\"{EscapeAttr(pageName)}\"/>");
            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
            {
                renderer.Write($"<ac:link-body>{EscapeXml(parts[1])}</ac:link-body>");
            }
            else
            {
                renderer.Write("<ac:link-body>");
                renderer.WriteChildren(link);
                renderer.Write("</ac:link-body>");
            }
            renderer.Write("</ac:link>");
        }
        else if (url.StartsWith('#'))
        {
            renderer.Write($"<a href=\"{EscapeAttr(url)}\">");
            renderer.WriteChildren(link);
            renderer.Write("</a>");
        }
        else
        {
            var filename = Path.GetFileName(url);
            renderer.Write("<ac:link>");
            renderer.Write($"<ri:attachment ri:filename=\"{EscapeAttr(filename)}\"/>");
            renderer.Write("<ac:link-body>");
            renderer.WriteChildren(link);
            renderer.Write("</ac:link-body>");
            renderer.Write("</ac:link>");
        }
    }

    /// <summary>
    /// Sanitizes a URL by encoding invalid characters.
    /// </summary>
    private static string SanitizeUrl(string url)
    {
        // Replace spaces with %20, trim whitespace
        url = url.Trim();
        url = InvalidUrlChars().Replace(url, match =>
            Uri.EscapeDataString(match.Value));
        return url;
    }

    private static string EscapeAttr(string text) =>
        text.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    [GeneratedRegex(@"[^\w\-._~:/?#\[\]@!$&'()*+,;=%]")]
    private static partial Regex InvalidUrlChars();
}
