using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using ConfluentSynkMD.ETL.Core;
using ConfluentSynkMD.Models;
using Serilog;

namespace ConfluentSynkMD.ETL.Transform;

/// <summary>
/// Transform step (Download direction): converts Confluence pages (Storage Format XHTML)
/// from <see cref="TranslationBatchContext.ExtractedConfluencePages"/> back to Markdown
/// and writes them to <see cref="TranslationBatchContext.TransformedDocuments"/>.
/// Replaces the former <c>MarkdownTransformer</c>.
/// </summary>
public sealed partial class MarkdownTransformStep : IPipelineStep
{
    private readonly ILogger _logger;

    public string StepName => "MarkdownTransform";

    public MarkdownTransformStep(ILogger logger)
    {
        _logger = logger.ForContext<MarkdownTransformStep>();
    }

    public async Task<PipelineResult> ExecuteAsync(TranslationBatchContext context, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var successCount = 0;
        var failedCount = 0;

        foreach (var pageWithAttachments in context.ExtractedConfluencePages)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var doc = await TransformSingleAsync(pageWithAttachments, ct);
                context.TransformedDocuments.Add(doc);
                successCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.Error(ex, "Failed to transform page '{Title}' (ID: {Id}). Skipping.",
                    pageWithAttachments.Page.Title, pageWithAttachments.Page.Id);
            }
        }

        sw.Stop();

        if (successCount == 0 && failedCount > 0)
        {
            return PipelineResult.CriticalError(
                StepName,
                $"All {failedCount} pages failed to transform.");
        }

        if (failedCount > 0)
        {
            return PipelineResult.Warning(
                StepName, successCount, failedCount, sw.Elapsed,
                $"{successCount} pages transformed, {failedCount} failed.");
        }

        return PipelineResult.Success(StepName, successCount, sw.Elapsed);
    }

    private async Task<ConvertedDocument> TransformSingleAsync(
        ConfluencePageWithAttachments pageWithAttachments, CancellationToken ct)
    {
        var page = pageWithAttachments.Page;
        var storageValue = page.Body?.Storage?.Value ?? string.Empty;
        _logger.Debug("Transforming page '{Title}' ({Length} chars XHTML) → Markdown.",
            page.Title, storageValue.Length);

        // Pre-extract CDATA blocks before AngleSharp parses (HTML5 parser doesn't support CDATA)
        var cdataBlocks = new Dictionary<string, string>();
        var preprocessed = CdataRegex().Replace(storageValue, match =>
        {
            var key = $"CDATA_PLACEHOLDER_{cdataBlocks.Count}";
            cdataBlocks[key] = match.Groups[1].Value;
            return key;
        });

        var config = AngleSharp.Configuration.Default;
        var browsingContext = BrowsingContext.New(config);
        var doc = await browsingContext.OpenAsync(req => req.Content(preprocessed), ct);

        var sb = new StringBuilder();
        var referencedImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Generate frontmatter
        sb.AppendLine("---");
        sb.AppendLine(CultureInfo.InvariantCulture, $"title: \"{EscapeYaml(page.Title)}\"");
        sb.AppendLine(CultureInfo.InvariantCulture, $"page_id: \"{page.Id}\"");
        if (page.SpaceId is not null)
            sb.AppendLine(CultureInfo.InvariantCulture, $"space_id: \"{page.SpaceId}\"");
        sb.AppendLine("---");
        sb.AppendLine();

        // Convert body — track referenced images
        // First, extract original filename + source path from metadata macro (if present)
        string? originalFilename = null;
        string? originalSourcePath = null;
        var metaMacro = doc.QuerySelector("ac\\:structured-macro[ac\\:name='expand']");
        if (metaMacro is not null)
        {
            var metaTitle = metaMacro.QuerySelector("ac\\:parameter[ac\\:name='title']")?.TextContent;
            if (metaTitle == "__ConfluentSynkMD_metadata__")
            {
                var body = metaMacro.QuerySelector("ac\\:rich-text-body")?.TextContent ?? "";
                foreach (var line in body.Split('\n', StringSplitOptions.TrimEntries))
                {
                    if (line.StartsWith("source-file:", StringComparison.Ordinal))
                        originalFilename = line["source-file:".Length..].Trim();
                    else if (line.StartsWith("source-path:", StringComparison.Ordinal))
                        originalSourcePath = line["source-path:".Length..].Trim();
                }
                metaMacro.Remove(); // Remove from DOM so it doesn't appear in output
            }
        }
        // Strip auto-generated info macro (generated_by) so it doesn't appear in download output
        var infoMacro = doc.QuerySelector("ac\\:structured-macro[ac\\:name='info']");
        if (infoMacro is not null)
        {
            var infoBody = infoMacro.QuerySelector("ac\\:rich-text-body")?.TextContent ?? "";
            if (infoBody.Contains("MARKDOWN", StringComparison.OrdinalIgnoreCase)
                || infoBody.Contains("generated", StringComparison.OrdinalIgnoreCase))
            {
                infoMacro.Remove();
            }
        }
        ConvertNode(doc.Body!, sb, 0, referencedImages, cdataBlocks);

        // Normalize excessive blank lines: collapse 3+ consecutive newlines to 2
        var markdown = ExcessiveNewlinesRegex().Replace(sb.ToString().TrimEnd(), "\n\n") + "\n";

        // Build attachment list: only include attachments that are referenced in the page
        // or include all image attachments if none are specifically referenced
        var attachments = new List<AttachmentInfo>();
        foreach (var att in pageWithAttachments.Attachments)
        {
            // Build download URL from the attachment's _links.download
            var downloadPath = att.Links?.Download ?? $"/wiki/download/attachments/{page.Id}/{Uri.EscapeDataString(att.Title)}";

            attachments.Add(new AttachmentInfo(
                FileName: att.Title,
                AbsolutePath: downloadPath, // Reuse AbsolutePath to carry the download path
                MimeType: att.MediaType));
        }

        return new ConvertedDocument(
            Title: page.Title,
            Content: markdown,
            Metadata: new DocumentMetadata(
                PageId: page.Id,
                SpaceKey: null,
                Title: page.Title),
            SourcePath: string.Empty,
            Attachments: attachments,
            ParentPageId: pageWithAttachments.ParentPageId,
            HasChildren: pageWithAttachments.HasChildren,
            OriginalFilename: originalFilename,
            OriginalSourcePath: originalSourcePath);
    }

    private void ConvertNode(INode node, StringBuilder sb, int depth, HashSet<string> referencedImages, Dictionary<string, string> cdataBlocks)
    {
        foreach (var child in node.ChildNodes)
        {
            switch (child)
            {
                case IText textNode:
                    sb.Append(textNode.TextContent);
                    break;

                case IElement element:
                    ConvertElement(element, sb, depth, referencedImages, cdataBlocks);
                    break;
            }
        }
    }

    private void ConvertElement(IElement el, StringBuilder sb, int depth, HashSet<string> referencedImages, Dictionary<string, string> cdataBlocks)
    {
        var tagName = el.TagName.ToLowerInvariant();

        switch (tagName)
        {
            // Headings
            case "h1": sb.Append("# "); ConvertNode(el, sb, depth, referencedImages, cdataBlocks); sb.AppendLine(); sb.AppendLine(); break;
            case "h2": sb.Append("## "); ConvertNode(el, sb, depth, referencedImages, cdataBlocks); sb.AppendLine(); sb.AppendLine(); break;
            case "h3": sb.Append("### "); ConvertNode(el, sb, depth, referencedImages, cdataBlocks); sb.AppendLine(); sb.AppendLine(); break;
            case "h4": sb.Append("#### "); ConvertNode(el, sb, depth, referencedImages, cdataBlocks); sb.AppendLine(); sb.AppendLine(); break;
            case "h5": sb.Append("##### "); ConvertNode(el, sb, depth, referencedImages, cdataBlocks); sb.AppendLine(); sb.AppendLine(); break;
            case "h6": sb.Append("###### "); ConvertNode(el, sb, depth, referencedImages, cdataBlocks); sb.AppendLine(); sb.AppendLine(); break;

            // Paragraphs
            case "p":
                ConvertNode(el, sb, depth, referencedImages, cdataBlocks);
                sb.AppendLine();
                sb.AppendLine();
                break;

            // Inline formatting
            case "strong" or "b": sb.Append("**"); ConvertNode(el, sb, depth, referencedImages, cdataBlocks); sb.Append("**"); break;
            case "em" or "i": sb.Append('*'); ConvertNode(el, sb, depth, referencedImages, cdataBlocks); sb.Append('*'); break;
            case "del" or "s": sb.Append("~~"); ConvertNode(el, sb, depth, referencedImages, cdataBlocks); sb.Append("~~"); break;
            case "code": sb.Append('`'); ConvertNode(el, sb, depth, referencedImages, cdataBlocks); sb.Append('`'); break;

            // Links
            case "a":
                var href = el.GetAttribute("href") ?? "";
                sb.Append(CultureInfo.InvariantCulture, $"[{el.TextContent}]({href})");
                break;

            // Lists
            case "ul": ConvertList(el, sb, depth, ordered: false, referencedImages, cdataBlocks); break;
            case "ol": ConvertList(el, sb, depth, ordered: true, referencedImages, cdataBlocks); break;

            // Tables
            case "table": ConvertTable(el, sb); break;

            // Line break
            case "br": sb.AppendLine(); break;
            case "hr": sb.AppendLine("---"); sb.AppendLine(); break;

            // Confluence structured macros
            case "ac:structured-macro":
                ConvertMacro(el, sb, depth, referencedImages, cdataBlocks);
                break;

            // Confluence link
            case "ac:link":
                var pageTitle = el.QuerySelector("ri\\:page")?.GetAttribute("ri:content-title") ?? "";
                var linkBody = el.QuerySelector("ac\\:link-body")?.TextContent ?? pageTitle;
                sb.Append(CultureInfo.InvariantCulture, $"[{linkBody}]({pageTitle}.md)");
                break;

            // Confluence image — skip mermaid PNGs when source macro follows, otherwise output as markdown image
            case "ac:image":
                var attachment = el.QuerySelector("ri\\:attachment");
                var urlEl = el.QuerySelector("ri\\:url");
                var alt = el.GetAttribute("ac:alt") ?? "";

                // Check if the next sibling is a mermaid code macro (auto-generated by upload)
                // If so, skip this image — the macro handler will emit the ```mermaid block
                if (attachment is not null)
                {
                    var imgFilename = attachment.GetAttribute("ri:filename") ?? "";
                    if (imgFilename.StartsWith("mermaid-", StringComparison.OrdinalIgnoreCase))
                    {
                        // Look ahead for a code macro with language=mermaid
                        var next = el.NextElementSibling;
                        if (next is not null &&
                            next.TagName.Equals("ac:structured-macro", StringComparison.OrdinalIgnoreCase) &&
                            next.GetAttribute("ac:name") == "code")
                        {
                            var langParam = next.QuerySelector("ac\\:parameter[ac\\:name='language']");
                            if (langParam is not null &&
                                string.Equals(langParam.TextContent, "mermaid", StringComparison.OrdinalIgnoreCase))
                            {
                                // Skip this image — the mermaid code macro will be handled next
                                break;
                            }
                        }
                    }

                    referencedImages.Add(imgFilename);
                    sb.Append(CultureInfo.InvariantCulture, $"![{alt}](img/{imgFilename})");
                    sb.AppendLine();
                    sb.AppendLine();
                }
                else if (urlEl is not null)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"![{alt}]({urlEl.GetAttribute("ri:value")})");
                    sb.AppendLine();
                    sb.AppendLine();
                }
                break;

            default:
                // Fallback: recurse into children
                ConvertNode(el, sb, depth, referencedImages, cdataBlocks);
                break;
        }
    }

    private void ConvertMacro(IElement macro, StringBuilder sb, int depth, HashSet<string> referencedImages, Dictionary<string, string> cdataBlocks)
    {
        var macroName = macro.GetAttribute("ac:name") ?? "";

        switch (macroName)
        {
            case "code":
                var lang = macro.QuerySelector("ac\\:parameter[ac\\:name='language']")?.TextContent ?? "";
                // Extract code from pre-extracted CDATA blocks (placeholder was left in DOM)
                var codeEl = macro.QuerySelector("ac\\:plain-text-body");
                var code = "";
                if (codeEl is not null)
                {
                    var textContent = codeEl.TextContent.Trim();
                    // Check if the content is a CDATA placeholder
                    if (cdataBlocks.TryGetValue(textContent, out var cdataContent))
                    {
                        code = cdataContent;
                    }
                    else
                    {
                        code = StripCdataMarkers(textContent);
                    }
                }

                // Mermaid code macros: emit as ```mermaid block
                // These are auto-generated by the upload pipeline to preserve source
                if (string.Equals(lang, "mermaid", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(code))
                {
                    sb.AppendLine("```mermaid");
                    sb.AppendLine(code.Trim());
                    sb.AppendLine("```");
                    sb.AppendLine();
                    break;
                }

                sb.AppendLine(CultureInfo.InvariantCulture, $"```{lang}");
                sb.AppendLine(code.Trim());
                sb.AppendLine("```");
                sb.AppendLine();
                break;

            case "info": WriteAlert(sb, "NOTE", macro, depth); break;
            case "tip": WriteAlert(sb, "TIP", macro, depth); break;
            case "note": WriteAlert(sb, "IMPORTANT", macro, depth); break;
            case "warning": WriteAlert(sb, "WARNING", macro, depth); break;

            case "toc":
                sb.AppendLine("[[_TOC_]]");
                sb.AppendLine();
                break;

            case "children":
                sb.AppendLine("[[_LISTING_]]");
                sb.AppendLine();
                break;

            default:
                // Preserve unknown macros as HTML comments for future handling
                _logger.Debug("Unknown macro '{Name}', preserving as comment.", macroName);
                sb.AppendLine($"<!-- confluence-macro: {macroName} -->");
                var body = macro.QuerySelector("ac\\:rich-text-body")?.TextContent ?? "";
                if (!string.IsNullOrWhiteSpace(body))
                    sb.AppendLine(body.Trim());
                sb.AppendLine();
                break;
        }
    }

    private static void WriteAlert(StringBuilder sb, string alertType, IElement macro, int depth)
    {
        sb.AppendLine(CultureInfo.InvariantCulture, $"> [!{alertType}]");
        var body = macro.QuerySelector("ac\\:rich-text-body");
        if (body is not null)
        {
            foreach (var child in body.Children)
            {
                if (child.TagName.Equals("p", StringComparison.OrdinalIgnoreCase))
                {
                    // Check if this <p> has a leading <strong> title (from admonition pre-processing)
                    var firstChild = child.FirstChild;
                    if (firstChild is IElement strongEl
                        && strongEl.TagName.Equals("strong", StringComparison.OrdinalIgnoreCase))
                    {
                        // Emit the title on its own line
                        sb.AppendLine(CultureInfo.InvariantCulture, $"> **{strongEl.TextContent.Trim()}**");
                        // Emit the remaining inline content as body
                        var remaining = new StringBuilder();
                        foreach (var sibling in child.ChildNodes)
                        {
                            if (sibling == strongEl) continue;
                            if (sibling is IElement el && el.TagName.Equals("code", StringComparison.OrdinalIgnoreCase))
                                remaining.Append(CultureInfo.InvariantCulture, $"`{el.TextContent}`");
                            else
                                remaining.Append(sibling.TextContent);
                        }
                        var bodyText = remaining.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(bodyText))
                        {
                            foreach (var line in bodyText.Split('\n'))
                                sb.AppendLine(CultureInfo.InvariantCulture, $"> {line.Trim()}");
                        }
                    }
                    else
                    {
                        // Regular paragraph — emit each line as quoted
                        var text = child.TextContent.Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            foreach (var line in text.Split('\n'))
                                sb.AppendLine(CultureInfo.InvariantCulture, $"> {line.Trim()}");
                        }
                    }
                }
                else
                {
                    var text = child.TextContent.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        foreach (var line in text.Split('\n'))
                            sb.AppendLine(CultureInfo.InvariantCulture, $"> {line.Trim()}");
                    }
                }
            }
        }
        sb.AppendLine();
    }

    private void ConvertList(IElement list, StringBuilder sb, int depth, bool ordered, HashSet<string> referencedImages, Dictionary<string, string> cdataBlocks)
    {
        var index = 1;
        foreach (var li in list.Children)
        {
            if (!li.TagName.Equals("li", StringComparison.OrdinalIgnoreCase)) continue;

            var indent = new string(' ', depth * 2);
            var bullet = ordered ? $"{index++}." : "-";
            sb.Append(CultureInfo.InvariantCulture, $"{indent}{bullet} ");

            // Flatten inline text — handle <p> inside <li> without double spacing
            foreach (var child in li.ChildNodes)
            {
                if (child is IText text)
                    sb.Append(text.TextContent.Trim());
                else if (child is IElement childEl)
                {
                    var tag = childEl.TagName.ToLowerInvariant();
                    if (tag is "ul" or "ol")
                    {
                        sb.AppendLine();
                        ConvertList(childEl, sb, depth + 1, tag == "ol", referencedImages, cdataBlocks);
                        continue;
                    }
                    // <p> inside <li>: emit content only, no paragraph spacing
                    if (tag == "p")
                    {
                        ConvertNode(childEl, sb, depth, referencedImages, cdataBlocks);
                        continue;
                    }
                    // Block-level macros (code blocks etc.) inside <li> need a newline before them
                    // and proper indentation to be valid Markdown
                    if (tag == "ac:structured-macro")
                    {
                        sb.AppendLine();
                        var blockIndent = new string(' ', (depth + 1) * 2 + (ordered ? 1 : 0));
                        var tempSb = new StringBuilder();
                        ConvertElement(childEl, tempSb, depth, referencedImages, cdataBlocks);
                        var blockContent = tempSb.ToString().TrimEnd();
                        foreach (var blockLine in blockContent.Split('\n'))
                            sb.AppendLine(CultureInfo.InvariantCulture, $"{blockIndent}{blockLine}");
                        continue;
                    }
                    ConvertElement(childEl, sb, depth, referencedImages, cdataBlocks);
                }
            }
            sb.AppendLine();
        }
        if (depth == 0) sb.AppendLine();
    }

    private static void ConvertTable(IElement table, StringBuilder sb)
    {
        var rows = table.QuerySelectorAll("tr").ToList();
        if (rows.Count == 0) return;

        var isFirstRow = true;
        foreach (var row in rows)
        {
            var cells = row.QuerySelectorAll("th, td").ToList();
            sb.Append("| ");
            sb.Append(string.Join(" | ", cells.Select(c => c.TextContent.Trim())));
            sb.AppendLine(" |");

            if (isFirstRow)
            {
                sb.Append("| ");
                sb.Append(string.Join(" | ", cells.Select(_ => "---")));
                sb.AppendLine(" |");
                isFirstRow = false;
            }
        }
        sb.AppendLine();
    }

    private static string EscapeYaml(string text) =>
        text.Replace("\"", "\\\"");

    /// <summary>
    /// Strips CDATA markers that AngleSharp may include in TextContent.
    /// </summary>
    private static string StripCdataMarkers(string text)
    {
        // Remove leading <![CDATA[ and trailing ]]> if present
        text = text.TrimStart();
        if (text.StartsWith("<![CDATA[", StringComparison.Ordinal))
            text = text["<![CDATA[".Length..];
        text = text.TrimEnd();
        if (text.EndsWith("]]>", StringComparison.Ordinal))
            text = text[..^"]]>".Length];
        return text;
    }

    // Matches CDATA content in raw XHTML (also handles HTML-encoded version from AngleSharp)
    [GeneratedRegex(@"<!\[CDATA\[(.*?)\]\]>", RegexOptions.Singleline)]
    private static partial Regex CdataRegex();

    // Matches HTML-encoded CDATA (as AngleSharp serializes in OuterHtml)
    [GeneratedRegex(@"&lt;!\[CDATA\[(.*?)\]\]&gt;", RegexOptions.Singleline)]
    private static partial Regex HtmlEncodedCdataRegex();

    // Collapses 3+ consecutive newlines to exactly 2 (one blank line)
    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessiveNewlinesRegex();
}
