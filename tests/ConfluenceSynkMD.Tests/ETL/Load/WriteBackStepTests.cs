using FluentAssertions;
using ConfluenceSynkMD.ETL.Core;
using ConfluenceSynkMD.ETL.Load;
using ConfluenceSynkMD.Models;
using NSubstitute;
using Serilog;

namespace ConfluenceSynkMD.Tests.ETL.Load;

public class WriteBackStepTests : IDisposable
{
    private readonly WriteBackStep _sut;
    private readonly List<string> _tempFiles = new();

    public WriteBackStepTests()
    {
        var logger = Substitute.For<ILogger>();
        _sut = new WriteBackStep(logger);
    }

    public void Dispose()
    {
        foreach (var f in _tempFiles)
        {
            if (File.Exists(f)) File.Delete(f);
        }
        GC.SuppressFinalize(this);
    }

    private string CreateTempMarkdown(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    private static TranslationBatchContext CreateContext(bool noWriteBack = false) =>
        new()
        {
            Options = new SyncOptions(SyncMode.Upload, "/tmp", "GLOBAL", NoWriteBack: noWriteBack)
        };

    [Fact]
    public async Task ExecuteAsync_Should_WriteGlobalSpaceKey_When_NoCacheEntry()
    {
        // Arrange
        var filePath = CreateTempMarkdown("# Hello\n\nSome content.");
        var context = CreateContext();
        var doc = new ConvertedDocument(
            Title: "Hello", Content: "<p>Hello</p>", Metadata: new DocumentMetadata(),
            SourcePath: filePath, Attachments: Array.Empty<AttachmentInfo>());
        context.TransformedDocuments.Add(doc);
        context.PageIdCache[filePath] = "12345";
        // No SpaceKeyCache entry → should fall back to global

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        var written = await File.ReadAllTextAsync(filePath);
        written.Should().Contain("<!-- confluence-page-id: 12345 -->");
        written.Should().Contain("<!-- confluence-space-key: GLOBAL -->");
    }

    [Fact]
    public async Task ExecuteAsync_Should_WritePerDocumentSpaceKey_When_CacheEntryExists()
    {
        // Arrange
        var filePath = CreateTempMarkdown("# Hello\n\nSome content.");
        var context = CreateContext();
        var doc = new ConvertedDocument(
            Title: "Hello", Content: "<p>Hello</p>",
            Metadata: new DocumentMetadata(SpaceKey: "TEAM"),
            SourcePath: filePath, Attachments: Array.Empty<AttachmentInfo>());
        context.TransformedDocuments.Add(doc);
        context.PageIdCache[filePath] = "67890";
        context.SpaceKeyCache[filePath] = "TEAM"; // simulates ConfluenceLoadStep caching

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        var written = await File.ReadAllTextAsync(filePath);
        written.Should().Contain("<!-- confluence-page-id: 67890 -->");
        written.Should().Contain("<!-- confluence-space-key: TEAM -->");
        written.Should().NotContain("GLOBAL");
    }

    [Fact]
    public async Task ExecuteAsync_Should_SkipWriteBack_When_NoWriteBackEnabled()
    {
        // Arrange
        var context = CreateContext(noWriteBack: true);

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        result.ItemsProcessed.Should().Be(0);
    }
}
