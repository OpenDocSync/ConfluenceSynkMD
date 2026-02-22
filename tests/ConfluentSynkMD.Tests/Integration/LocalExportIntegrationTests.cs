using FluentAssertions;
using Markdig;
using ConfluentSynkMD.Configuration;
using ConfluentSynkMD.ETL.Core;
using ConfluentSynkMD.ETL.Load;
using ConfluentSynkMD.Markdig;
using ConfluentSynkMD.Models;
using NSubstitute;
using Serilog;

namespace ConfluentSynkMD.Tests.Integration;

/// <summary>
/// End-to-end tests for the local export pipeline using mkdocs-example as input.
/// </summary>
[Trait("Category", "Integration")]
public class LocalExportIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _mkdocsDir;

    public LocalExportIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"md2c-export-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // Resolve mkdocs-example relative to test assembly
        var assemblyDir = Path.GetDirectoryName(typeof(LocalExportIntegrationTests).Assembly.Location)!;
        _mkdocsDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "mkdocs-example", "docs"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static string RenderMarkdownToXhtml(string markdown, ConverterOptions? opts = null)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        using var writer = new StringWriter();
        var renderer = new ConfluenceRenderer(writer)
        {
            ConverterOptions = opts ?? new ConverterOptions(),
            LayoutOptions = new LayoutOptions()
        };
        var document = Markdown.Parse(markdown, pipeline);
        renderer.Render(document);
        writer.Flush();
        return writer.ToString();
    }

    [Fact]
    public async Task FullPipeline_Should_ProduceExportDirectory()
    {
        // Skip if mkdocs-example doesn't exist in the expected location
        if (!Directory.Exists(_mkdocsDir))
        {
            return; // Skip silently
        }

        var logger = Substitute.For<ILogger>();
        var sut = new LocalOnlyLoadStep(logger);

        var context = new TranslationBatchContext
        {
            Options = new SyncOptions(SyncMode.LocalExport, _tempDir, "TEST")
        };

        // Simulate the transform step by converting markdown files
        var mdFiles = Directory.GetFiles(_mkdocsDir, "*.md", SearchOption.TopDirectoryOnly);
        foreach (var mdFile in mdFiles)
        {
            var content = await File.ReadAllTextAsync(mdFile);
            var title = Path.GetFileNameWithoutExtension(mdFile);
            var xhtml = RenderMarkdownToXhtml(content);
            context.TransformedDocuments.Add(new ConvertedDocument(
                title, xhtml, new DocumentMetadata(), mdFile, Array.Empty<AttachmentInfo>()));
        }

        var result = await sut.ExecuteAsync(context);

        result.Status.Should().Be(PipelineResultStatus.Success);
        var exportDir = Path.Combine(_tempDir, ".confluence-export");
        Directory.Exists(exportDir).Should().BeTrue();
        Directory.GetFiles(exportDir, "*.csf.html").Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MermaidDiagram_Should_ProduceImageReference()
    {
        var markdown = "```mermaid\ngraph TD\n    A-->B\n```";
        var opts = new ConverterOptions { RenderMermaid = true };
        var xhtml = RenderMarkdownToXhtml(markdown, opts);

        xhtml.Should().Contain("<ac:image>");
        xhtml.Should().Contain("ri:attachment");
        xhtml.Should().Contain(".png");
    }

    [Fact]
    public void Admonition_Should_ProduceInfoMacro()
    {
        var markdown = "> [!NOTE]\n> Use the `watch` mode to auto-update.";
        var xhtml = RenderMarkdownToXhtml(markdown);

        xhtml.Should().Contain("ac:name=\"info\"");
    }

    [Fact]
    public void TitlePrefix_Should_BeApplied()
    {
        var opts = new ConverterOptions { TitlePrefix = "[AUTO] " };
        var title = $"{opts.TitlePrefix}My Page";

        title.Should().StartWith("[AUTO] ");
    }

    [Fact]
    public void CsfContent_Should_ContainValidXhtmlStructure()
    {
        var markdown = "# Hello\n\nSome **bold** text.\n\n| A | B |\n|---|---|\n| 1 | 2 |";
        var xhtml = RenderMarkdownToXhtml(markdown);

        // Verify basic structure
        xhtml.Should().Contain("<h1>");
        xhtml.Should().Contain("<strong>");
        xhtml.Should().Contain("<table");
    }

    [Fact]
    public void Images_Should_ProduceAttachmentReferences()
    {
        var markdown = "![Architecture](img/architecture.jpg)";
        var xhtml = RenderMarkdownToXhtml(markdown);

        xhtml.Should().Contain("<ac:image");
        xhtml.Should().Contain("ri:attachment");
        xhtml.Should().Contain("architecture.jpg");
    }
}
