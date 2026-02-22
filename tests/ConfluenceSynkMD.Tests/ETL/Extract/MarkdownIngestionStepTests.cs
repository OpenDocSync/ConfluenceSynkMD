using FluentAssertions;
using ConfluenceSynkMD.ETL.Core;
using ConfluenceSynkMD.ETL.Extract;
using ConfluenceSynkMD.Models;
using ConfluenceSynkMD.Services;
using NSubstitute;
using Serilog;

namespace ConfluenceSynkMD.Tests.ETL.Extract;

/// <summary>
/// Tests for <see cref="MarkdownIngestionStep"/>.
/// Uses real filesystem via temp directories (HierarchyResolver is sealed, cannot be mocked).
/// </summary>
public class MarkdownIngestionStepTests : IDisposable
{
    private readonly MarkdownIngestionStep _sut;
    private readonly string _tempDir;

    public MarkdownIngestionStepTests()
    {
        var fmLogger = Substitute.For<ILogger>();
        var hrLogger = Substitute.For<ILogger>();
        var stepLogger = Substitute.For<ILogger>();

        var parser = new FrontmatterParser(fmLogger);
        var resolver = new HierarchyResolver(parser, hrLogger);
        _sut = new MarkdownIngestionStep(resolver, stepLogger);

        _tempDir = Path.Combine(Path.GetTempPath(), $"md2conf-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private TranslationBatchContext CreateContext(string? path = null) =>
        new() { Options = new SyncOptions(SyncMode.Upload, path ?? _tempDir, "TEST") };

    [Fact]
    public async Task ExecuteAsync_Should_PopulateContext_When_FilesExist()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "page1.md"), "# Page 1\n\nContent 1.");
        File.WriteAllText(Path.Combine(_tempDir, "page2.md"), "# Page 2\n\nContent 2.");
        var context = CreateContext();

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.ExtractedDocumentNodes.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnAbort_When_NoFilesFound()
    {
        // Arrange — empty temp dir
        var context = CreateContext();

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Abort);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnCriticalError_When_DirectoryMissing()
    {
        // Arrange
        var context = CreateContext(Path.Combine(_tempDir, "nonexistent"));

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.CriticalError);
        result.Exception.Should().BeOfType<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_Should_FlattenHierarchy_When_NestedTree()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "index.md"), "# Root");
        var sub = Path.Combine(_tempDir, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "child.md"), "# Child");
        var context = CreateContext();

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.ExtractedDocumentNodes.Should().HaveCount(2,
            because: "root index.md + sub/child.md should both be flattened");
    }

    [Fact]
    public void StepName_Should_ReturnMarkdownIngestion_When_Accessed()
    {
        // Act & Assert
        _sut.StepName.Should().Be("MarkdownIngestion");
    }
}
