using FluentAssertions;
using ConfluentSynkMD.ETL.Core;
using ConfluentSynkMD.ETL.Load;
using ConfluentSynkMD.Models;
using ConfluentSynkMD.Services;
using NSubstitute;
using Serilog;

namespace ConfluentSynkMD.Tests.ETL.Load;

/// <summary>
/// Tests for <see cref="FileSystemLoadStep"/>.
/// Uses real temp directories to verify actual filesystem writes.
/// </summary>
public class FileSystemLoadStepTests : IDisposable
{
    private readonly FileSystemLoadStep _sut;
    private readonly IConfluenceApiClient _api;
    private readonly string _tempDir;

    public FileSystemLoadStepTests()
    {
        _api = Substitute.For<IConfluenceApiClient>();
        var slugGenerator = new SlugGenerator();
        var logger = Substitute.For<ILogger>();
        _sut = new FileSystemLoadStep(slugGenerator, _api, logger);

        _tempDir = Path.Combine(Path.GetTempPath(), $"md2conf-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private TranslationBatchContext CreateContext() =>
        new() { Options = new SyncOptions(SyncMode.Download, _tempDir, "TEST") };

    private static ConvertedDocument CreateDoc(
        string title, string content = "# Test\n\nContent.",
        bool hasChildren = false, string? parentPageId = null) =>
        new(
            Title: title,
            Content: content,
            Metadata: new DocumentMetadata(),
            SourcePath: $"/confluence/{title}",
            Attachments: Array.Empty<AttachmentInfo>(),
            HasChildren: hasChildren,
            ParentPageId: parentPageId);

    [Fact]
    public async Task ExecuteAsync_Should_WriteFiles_When_DocumentsPresent()
    {
        // Arrange
        var context = CreateContext();
        context.TransformedDocuments.Add(CreateDoc("My Page"));

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.LoadedCount.Should().Be(1);

        var expectedFile = Path.Combine(_tempDir, "my-page.md");
        File.Exists(expectedFile).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_Should_CreateSubdir_When_PageHasChildren()
    {
        // Arrange — provide OriginalFilename so the root-skip logic doesn't apply
        var context = CreateContext();
        var doc = CreateDoc("Parent Page", hasChildren: true) with { OriginalFilename = "parent-page.md" };
        context.TransformedDocuments.Add(doc);

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);

        var expectedDir = Path.Combine(_tempDir, "parent-page");
        Directory.Exists(expectedDir).Should().BeTrue();

        var indexFile = Path.Combine(expectedDir, "index.md");
        File.Exists(indexFile).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_Should_HandleCollision_When_DuplicateSlug()
    {
        // Arrange
        var context = CreateContext();
        context.TransformedDocuments.Add(CreateDoc("Duplicate", content: "Content A"));
        context.TransformedDocuments.Add(CreateDoc("Duplicate", content: "Content B"));

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.LoadedCount.Should().Be(2);

        File.Exists(Path.Combine(_tempDir, "duplicate.md")).Should().BeTrue();
        File.Exists(Path.Combine(_tempDir, "duplicate-1.md")).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_Should_DownloadAttachments_When_Present()
    {
        // Arrange
        var attachmentContent = new MemoryStream("fake-image-data"u8.ToArray());
        var doc = new ConvertedDocument(
            Title: "With Image",
            Content: "# Image Page",
            Metadata: new DocumentMetadata(),
            SourcePath: "/confluence/with-image",
            Attachments: [new AttachmentInfo("test.png", "/download/test.png", "image/png")]);

        var context = CreateContext();
        context.TransformedDocuments.Add(doc);

        _api.DownloadAttachmentAsync("/download/test.png", Arg.Any<CancellationToken>())
            .Returns(attachmentContent);

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        var imgPath = Path.Combine(_tempDir, "img", "test.png");
        File.Exists(imgPath).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnSuccess_When_AllSaved()
    {
        // Arrange
        var context = CreateContext();
        context.TransformedDocuments.Add(CreateDoc("Page A"));
        context.TransformedDocuments.Add(CreateDoc("Page B"));
        context.TransformedDocuments.Add(CreateDoc("Page C"));

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.LoadedCount.Should().Be(3);
        context.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseSourcePath_When_Available()
    {
        // Arrange
        var doc = new ConvertedDocument(
            Title: "Diagrams",
            Content: "# Diagrams\n\nMermaid content.",
            Metadata: new DocumentMetadata(),
            SourcePath: "",
            Attachments: Array.Empty<AttachmentInfo>(),
            OriginalSourcePath: "features/diagrams.md");

        var context = CreateContext();
        context.TransformedDocuments.Add(doc);

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.LoadedCount.Should().Be(1);

        var expectedFile = Path.Combine(_tempDir, "features", "diagrams.md");
        File.Exists(expectedFile).Should().BeTrue("source-path should determine exact file location");
        File.ReadAllText(expectedFile).Should().Contain("Diagrams");
    }

    [Fact]
    public async Task ExecuteAsync_Should_FallbackToSlug_When_NoSourcePath()
    {
        // Arrange — no OriginalSourcePath set
        var context = CreateContext();
        context.TransformedDocuments.Add(CreateDoc("Legacy Page"));

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        File.Exists(Path.Combine(_tempDir, "legacy-page.md")).Should().BeTrue(
            "slug fallback should be used when no source-path metadata is available");
    }
}
