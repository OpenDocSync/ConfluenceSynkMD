using System.Diagnostics;
using Markdig;
using ConfluenceSynkMD.ETL.Core;
using ConfluenceSynkMD.Markdig;
using ConfluenceSynkMD.Models;
using ConfluenceSynkMD.Services;
using Serilog;

namespace ConfluenceSynkMD.ETL.Transform;

/// <summary>
/// Transform step (Upload direction): converts <see cref="DocumentNode"/> items
/// from <see cref="TranslationBatchContext.ExtractedDocumentNodes"/> into
/// Confluence Storage Format XHTML and writes them to
/// <see cref="TranslationBatchContext.TransformedDocuments"/>.
/// Replaces the former <c>ConfluenceXhtmlTransformer</c>.
/// </summary>
public sealed class ConfluenceXhtmlTransformStep : IPipelineStep
{
    private readonly MarkdownPipeline _pipeline;
    private readonly IMermaidRenderer _mermaidRenderer;
    private readonly IDiagramRenderer _drawioRenderer;
    private readonly IDiagramRenderer _plantUmlRenderer;
    private readonly ILatexRenderer _latexRenderer;
    private readonly IImageOptimizer _imageOptimizer;
    private readonly ILogger _logger;

    public string StepName => "ConfluenceXhtmlTransform";

    public ConfluenceXhtmlTransformStep(
        IMermaidRenderer mermaidRenderer,
        IDiagramRenderer drawioRenderer,
        IDiagramRenderer plantUmlRenderer,
        ILatexRenderer latexRenderer,
        IImageOptimizer imageOptimizer,
        ILogger logger)
    {
        _mermaidRenderer = mermaidRenderer;
        _drawioRenderer = drawioRenderer;
        _plantUmlRenderer = plantUmlRenderer;
        _latexRenderer = latexRenderer;
        _imageOptimizer = imageOptimizer;
        _logger = logger.ForContext<ConfluenceXhtmlTransformStep>();
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .UseTaskLists()
            .UseFootnotes()
            .UseEmojiAndSmiley()
            .UseMathematics()
            .Build();
    }

    /// <summary>Cached context reference so TransformSingleAsync can access converter/layout options.</summary>
    private TranslationBatchContext? _context;

    /// <summary>Link resolver service created from the page title mapping (TKT-005).</summary>
    private Services.ILinkResolver? _linkResolver;

    public async Task<PipelineResult> ExecuteAsync(TranslationBatchContext context, CancellationToken ct = default)
    {
        _context = context;
        var (pageTitleMapping, pageIdMapping) = BuildPageMappings(context);
        var urlBuilder = new Services.ConfluenceUrlBuilder(context.ConverterOptions.WebUiLinkStrategy);
        _linkResolver = new Services.LinkResolver(
            pageTitleMapping,
            context.Options.ConfluenceSpaceKey,
            pageIdMapping,
            urlBuilder,
            onUnresolvedLink: (linkPath, sourcePath, fallbackTitle) =>
            {
                context.UnresolvedLinkFallbackCount++;
                if (context.UnresolvedLinkSamples.Count < 10)
                {
                    context.UnresolvedLinkSamples.Add(
                        $"source='{sourcePath ?? "<unknown>"}', link='{linkPath}', fallback='{fallbackTitle}'");
                }
            },
            onWebUiPageIdFallback: (linkPath, sourcePath) =>
            {
                context.WebUiPageIdFallbackCount++;
                if (context.WebUiPageIdFallbackSamples.Count < 10)
                {
                    context.WebUiPageIdFallbackSamples.Add(
                        $"source='{sourcePath ?? "<unknown>"}', link='{linkPath}'");
                }
            });
        var sw = Stopwatch.StartNew();
        var successCount = 0;
        var failedCount = 0;

        foreach (var node in context.ExtractedDocumentNodes)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var doc = await TransformSingleAsync(node, ct);
                context.TransformedDocuments.Add(doc);
                successCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.Error(ex, "Failed to transform '{Path}'. Skipping.", node.RelativePath);
            }
        }

        sw.Stop();

        if (successCount == 0 && failedCount > 0)
        {
            return PipelineResult.CriticalError(
                StepName,
                $"All {failedCount} documents failed to transform.");
        }

        if (failedCount > 0)
        {
            return PipelineResult.Warning(
                StepName, successCount, failedCount, sw.Elapsed,
                $"{successCount} documents transformed, {failedCount} failed.");
        }

        return PipelineResult.Success(StepName, successCount, sw.Elapsed);
    }

    private async Task<ConvertedDocument> TransformSingleAsync(DocumentNode node, CancellationToken ct)
    {
        _logger.Debug("Transforming: {Path}", node.RelativePath);

        // Pre-process MkDocs admonitions (!!! type) into GitHub alerts (> [!TYPE])
        var preprocessed = AdmonitionPreProcessor.ConvertAdmonitions(node.MarkdownContent);
        var markdownDoc = Markdown.Parse(preprocessed, _pipeline);

        using var writer = new StringWriter();
        var renderer = new ConfluenceRenderer(writer);

        // Inject link resolver (TKT-005)
        renderer.LinkResolver = _linkResolver;
        renderer.CurrentDocumentPath = NormalizeVirtualPath(node.RelativePath);

        // Pass converter and layout options to the renderer
        if (_context is not null)
        {
            renderer.ConverterOptions = _context.ConverterOptions;

            // Per-document layout override from YAML frontmatter (coalesce with global defaults)
            var layoutOverride = node.Metadata.LayoutOverride;
            if (layoutOverride is not null)
            {
                var global = _context.LayoutOptions;
                renderer.LayoutOptions = new Configuration.LayoutOptions
                {
                    ImageAlignment = layoutOverride.ImageAlignment ?? global.ImageAlignment,
                    ImageMaxWidth = layoutOverride.ImageMaxWidth ?? global.ImageMaxWidth,
                    TableWidth = layoutOverride.TableWidth ?? global.TableWidth,
                    TableDisplayMode = layoutOverride.TableDisplayMode ?? global.TableDisplayMode,
                    ContentAlignment = layoutOverride.ContentAlignment ?? global.ContentAlignment,
                };
            }
            else
            {
                renderer.LayoutOptions = _context.LayoutOptions;
            }
        }

        // Render Markdown to Confluence XHTML with optional debug line markers
        try
        {
            renderer.Render(markdownDoc);
        }
        catch (Exception ex) when (_context?.ConverterOptions.DebugLineMarkers == true)
        {
            // Find the Markdig AST node closest to the error for source line traceability
            var lineInfo = "";
            foreach (var block in markdownDoc)
            {
                if (block.Line > 0)
                    lineInfo = $" @ line {block.Line + 1}";
            }
            throw new InvalidOperationException(
                $"Failed to convert Markdown file: {node.RelativePath}{lineInfo}", ex);
        }
        writer.Flush();

        var confluenceXhtml = writer.ToString();

        // Append hidden metadata macro with original filename + relative path for round-trip fidelity
        var originalFilename = GetNodeFileName(node);
        var relativePath = node.RelativePath.Replace('\\', '/');
        confluenceXhtml += "<ac:structured-macro ac:name=\"expand\">" +
            "<ac:parameter ac:name=\"title\">__ConfluenceSynkMD_metadata__</ac:parameter>" +
            $"<ac:rich-text-body><p>source-file:{originalFilename}\nsource-path:{relativePath}</p></ac:rich-text-body>" +
            "</ac:structured-macro>";

        // Extract title: prefer explicit frontmatter title, else first H1, else filename
        var title = node.Metadata.Title
                    ?? ExtractFirstHeading(node.MarkdownContent)
                    ?? GetNodeFileStem(node);

        // --title-prefix: prepend a prefix to all page titles
        var titlePrefix = _context?.ConverterOptions.TitlePrefix;
        if (!string.IsNullOrEmpty(titlePrefix))
        {
            title = titlePrefix + title;
        }

        // Collect local image attachments
        var sourceDir = Path.GetDirectoryName(node.AbsolutePath) ?? ".";
        var attachments = new List<AttachmentInfo>();

        foreach (var img in renderer.ReferencedImages)
        {
            var absPath = Path.GetFullPath(Path.Combine(sourceDir, img.RelativePath));
            if (!File.Exists(absPath)) continue;

            var optimizedPath = await _imageOptimizer.OptimizeImageAsync(absPath, ct);
            var mimeType = GetMimeType(img.FileName);
            attachments.Add(new AttachmentInfo(img.FileName, optimizedPath, mimeType));
        }

        // Render Mermaid diagrams to PNG and add as attachments
        await RenderDiagramsAsync(renderer.MermaidDiagrams, "Mermaid", title, attachments,
            async (source, _ct) => await _mermaidRenderer.RenderToPngAsync(source, _ct), ct);

        // Render Draw.io diagrams
        var diagramFormat = _context?.ConverterOptions.DiagramOutputFormat ?? "png";
        await RenderDiagramsAsync(renderer.DrawioDiagrams, "Draw.io", title, attachments,
            async (source, _ct) => await _drawioRenderer.RenderAsync(source, diagramFormat, _ct), ct);

        // Render PlantUML diagrams
        await RenderDiagramsAsync(renderer.PlantUmlDiagrams, "PlantUML", title, attachments,
            async (source, _ct) => await _plantUmlRenderer.RenderAsync(source, diagramFormat, _ct), ct);

        // Render LaTeX formulas
        await RenderDiagramsAsync(renderer.LatexFormulas, "LaTeX", title, attachments,
            async (source, _ct) => await _latexRenderer.RenderAsync(source, _ct), ct);

        // --generated-by: per-doc override + template substitution → prepended info macro (Python parity)
        var generatedBy = node.Metadata.GeneratedBy
                          ?? _context?.ConverterOptions.GeneratedBy;
        if (!string.IsNullOrEmpty(generatedBy))
        {
            generatedBy = ApplyGeneratedByTemplate(generatedBy, node);
            var infoMacro = "<ac:structured-macro ac:name=\"info\" ac:schema-version=\"1\">"
                + $"<ac:rich-text-body><p>{EscapeXml(generatedBy)}</p></ac:rich-text-body>"
                + "</ac:structured-macro>";
            confluenceXhtml = infoMacro + confluenceXhtml;
        }

        _logger.Debug("Transformed '{Title}' → {Length} chars XHTML, {Attachments} attachment(s).",
        title, confluenceXhtml.Length, attachments.Count);

        return new ConvertedDocument(
            Title: title,
            Content: confluenceXhtml,
            Metadata: node.Metadata,
            SourcePath: node.AbsolutePath,
            Attachments: attachments,
            ParentSourcePath: node.ParentSourcePath,
            HasChildren: node.Children.Count > 0);
    }

    /// <summary>
    /// Generic helper to render a list of diagram entries (Mermaid/Draw.io/PlantUML/LaTeX)
    /// to image attachments.
    /// </summary>
    private async Task RenderDiagramsAsync(
        List<(string FileName, string Source)> diagrams,
        string diagramType, string pageTitle,
        List<AttachmentInfo> attachments,
        Func<string, CancellationToken, Task<(byte[] ImageBytes, string Format)>> renderFunc,
        CancellationToken ct)
    {
        if (diagrams.Count == 0) return;

        _logger.Information("Rendering {Count} {Type} diagram(s) for '{Title}'.",
        diagrams.Count, diagramType, pageTitle);

        var tempDir = Path.Combine(Path.GetTempPath(), $"ConfluenceSynkMD-{diagramType.ToLowerInvariant()}-out");
        Directory.CreateDirectory(tempDir);

        foreach (var (fileName, source) in diagrams)
        {
            try
            {
                var (imageBytes, format) = await renderFunc(source, ct);
                var tempFile = Path.Combine(tempDir, fileName);
                await File.WriteAllBytesAsync(tempFile, imageBytes, ct);

                var optimizedPath = await _imageOptimizer.OptimizeImageAsync(tempFile, ct);
                var mimeType = $"image/{format}";
                attachments.Add(new AttachmentInfo(fileName, optimizedPath, mimeType));

                _logger.Debug("{Type} diagram '{File}' rendered ({Size} bytes).",
                diagramType, fileName, imageBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex,
                "Failed to render {Type} diagram '{File}'. It will appear as a broken image.",
                diagramType, fileName);
            }
        }
    }

    private static string? ExtractFirstHeading(string markdown)
    {
        foreach (var line in markdown.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
                return trimmed[2..].Trim();
        }
        return null;
    }

    private static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".drawio" => "application/vnd.jgraph.mxfile",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Apply template substitution to the generated_by string (Python parity).
    /// Supports: %{filepath}, %{filename}, %{filedir}, %{filestem}
    /// </summary>
    private static string ApplyGeneratedByTemplate(string template, DocumentNode node)
    {
        var relativePath = node.RelativePath.Replace('\\', '/');
        var fileName = Path.GetFileName(relativePath);
        var fileDir = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? ".";
        var fileStem = Path.GetFileNameWithoutExtension(relativePath);

        return template
            .Replace("%{filepath}", relativePath)
            .Replace("%{filename}", fileName)
            .Replace("%{filedir}", fileDir)
            .Replace("%{filestem}", fileStem);
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    // ─── Page Title Mapping (TKT-002) ───────────────────────────────────────

    /// <summary>
    /// Builds a mapping from relative .md paths to resolved page titles,
    /// used by <see cref="Markdig.Renderers.LinkInlineRenderer"/> for internal link resolution.
    /// </summary>
    private (Dictionary<string, string> Titles, Dictionary<string, string> PageIds) BuildPageMappings(TranslationBatchContext context)
    {
        var titleMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pageIdMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var titlePrefix = context.ConverterOptions.TitlePrefix ?? "";

        foreach (var node in context.ExtractedDocumentNodes)
            AddNodeToMapping(node, titleMapping, pageIdMapping, titlePrefix);

        _logger.Debug(
            "Built page mapping with {TitleCount} titles and {PageIdCount} page IDs.",
            titleMapping.Count,
            pageIdMapping.Count);
        return (titleMapping, pageIdMapping);
    }

    private static void AddNodeToMapping(
        DocumentNode node,
        Dictionary<string, string> titleMapping,
        Dictionary<string, string> pageIdMapping,
        string titlePrefix)
    {
        var title = titlePrefix + (node.Metadata.Title
            ?? ExtractFirstHeading(node.MarkdownContent)
            ?? GetNodeFileStem(node));

        var normalizedPath = NormalizeVirtualPath(node.RelativePath);
        titleMapping[normalizedPath] = title;

        if (!string.IsNullOrWhiteSpace(node.Metadata.PageId))
        {
            pageIdMapping[normalizedPath] = node.Metadata.PageId;
        }

        foreach (var child in node.Children)
            AddNodeToMapping(child, titleMapping, pageIdMapping, titlePrefix);
    }

    private static string GetNodeFileName(DocumentNode node)
    {
        var relativePath = node.RelativePath.Replace('\\', '/');
        return Path.GetFileName(relativePath);
    }

    private static string GetNodeFileStem(DocumentNode node)
    {
        var relativePath = node.RelativePath.Replace('\\', '/');
        return Path.GetFileNameWithoutExtension(relativePath);
    }

    private static string NormalizeVirtualPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new List<string>(parts.Length);

        foreach (var part in parts)
        {
            if (part == ".")
                continue;

            if (part == "..")
            {
                if (stack.Count > 0 && stack[^1] != "..")
                {
                    stack.RemoveAt(stack.Count - 1);
                }
                else
                {
                    stack.Add(part);
                }

                continue;
            }

            stack.Add(part);
        }

        return string.Join('/', stack);
    }
}
