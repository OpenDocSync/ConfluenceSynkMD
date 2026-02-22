using FluentAssertions;
using ConfluentSynkMD.ETL.Core;
using ConfluentSynkMD.ETL.Extract;
using ConfluentSynkMD.Models;
using ConfluentSynkMD.Services;
using NSubstitute;
using Serilog;
using NSubstitute.ExceptionExtensions;

namespace ConfluentSynkMD.Tests.ETL.Extract;

public class ConfluenceIngestionStepTests
{
    private readonly IConfluenceApiClient _api;
    private readonly ConfluenceIngestionStep _sut;

    private static readonly ConfluenceSpace TestSpace = new("space-1", "TEST", "Test Space", "hp-1");

    public ConfluenceIngestionStepTests()
    {
        _api = Substitute.For<IConfluenceApiClient>();
        var logger = Substitute.For<ILogger>();
        _sut = new ConfluenceIngestionStep(_api, logger);
    }

    private static TranslationBatchContext CreateContext(string? parentId = null) =>
        new() { Options = new SyncOptions(SyncMode.Download, "/tmp", "TEST", parentId) };

    private static ConfluencePage CreatePage(string id, string title) =>
        new(id, title, "space-1", null,
            new ConfluencePageBody(new ConfluenceStorage("storage", "<p>content</p>")),
            new ConfluenceVersion(1));

    private void SetupSpaceWithPages(params ConfluencePage[] pages)
    {
        _api.GetSpaceByKeyAsync("TEST", Arg.Any<CancellationToken>()).Returns(TestSpace);
        _api.GetPagesInSpaceAsync(TestSpace.Id, Arg.Any<CancellationToken>())
            .Returns(pages.ToAsyncEnumerable());

        foreach (var page in pages)
        {
            _api.GetPageByIdAsync(page.Id, Arg.Any<CancellationToken>()).Returns(page);
            _api.GetAttachmentsAsync(page.Id, Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<ConfluenceAttachment>());
        }
    }

    [Fact]
    public async Task ExecuteAsync_Should_PopulatePages_When_SpaceHasPages()
    {
        // Arrange
        SetupSpaceWithPages(CreatePage("p1", "Page 1"), CreatePage("p2", "Page 2"));
        var context = CreateContext();

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.ExtractedConfluencePages.Should().HaveCount(2);
        context.ResolvedSpace.Should().Be(TestSpace);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnAbort_When_NoPagesFound()
    {
        // Arrange
        _api.GetSpaceByKeyAsync("TEST", Arg.Any<CancellationToken>()).Returns(TestSpace);
        _api.GetPagesInSpaceAsync(TestSpace.Id, Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<ConfluencePage>());
        var context = CreateContext();

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Abort);
    }

    [Fact]
    public async Task ExecuteAsync_Should_FetchSubtree_When_ParentIdSet()
    {
        // Arrange
        var parent = CreatePage("parent-1", "Parent");
        var child = CreatePage("child-1", "Child");

        _api.GetSpaceByKeyAsync("TEST", Arg.Any<CancellationToken>()).Returns(TestSpace);
        // Root page is now also enriched
        _api.GetPageByIdAsync("parent-1", Arg.Any<CancellationToken>()).Returns(parent);
        _api.GetAttachmentsAsync("parent-1", Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<ConfluenceAttachment>());
        _api.GetChildPagesAsync("parent-1", Arg.Any<CancellationToken>())
            .Returns(new[] { child }.ToAsyncEnumerable());
        _api.GetChildPagesAsync("child-1", Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<ConfluencePage>());
        _api.GetPageByIdAsync("child-1", Arg.Any<CancellationToken>()).Returns(child);
        _api.GetAttachmentsAsync("child-1", Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<ConfluenceAttachment>());

        var context = CreateContext(parentId: "parent-1");

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert — root page + child = 2 pages
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.ExtractedConfluencePages.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ResolveRootPage_ForDownloadScoping()
    {
        // Arrange
        var rootPage = CreatePage("root-42", "Sync-Test");
        var child = CreatePage("child-1", "Child");

        _api.GetSpaceByKeyAsync("TEST", Arg.Any<CancellationToken>()).Returns(TestSpace);
        _api.GetPageByTitleAsync("Sync-Test", TestSpace.Id, Arg.Any<CancellationToken>())
            .Returns(rootPage);
        // Root enrichment
        _api.GetPageByIdAsync("root-42", Arg.Any<CancellationToken>()).Returns(rootPage);
        _api.GetAttachmentsAsync("root-42", Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<ConfluenceAttachment>());
        // Children
        _api.GetChildPagesAsync("root-42", Arg.Any<CancellationToken>())
            .Returns(new[] { child }.ToAsyncEnumerable());
        _api.GetChildPagesAsync("child-1", Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<ConfluencePage>());
        _api.GetPageByIdAsync("child-1", Arg.Any<CancellationToken>()).Returns(child);
        _api.GetAttachmentsAsync("child-1", Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<ConfluenceAttachment>());

        var context = new TranslationBatchContext
        {
            Options = new SyncOptions(SyncMode.Download, "/tmp", "TEST", RootPage: "Sync-Test")
        };

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert — resolved root + child = 2 pages
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.ExtractedConfluencePages.Should().HaveCount(2);
        await _api.Received(1).GetPageByTitleAsync("Sync-Test", TestSpace.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_Abort_When_RootPageNotFound()
    {
        // Arrange
        _api.GetSpaceByKeyAsync("TEST", Arg.Any<CancellationToken>()).Returns(TestSpace);
        _api.GetPageByTitleAsync("NonExistent", TestSpace.Id, Arg.Any<CancellationToken>())
            .Returns((ConfluencePage?)null);

        var context = new TranslationBatchContext
        {
            Options = new SyncOptions(SyncMode.Download, "/tmp", "TEST", RootPage: "NonExistent")
        };

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Abort);
        result.Message.Should().Contain("NonExistent");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnCriticalError_When_ApiThrows()
    {
        // Arrange
        _api.GetSpaceByKeyAsync("TEST", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var context = CreateContext();

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.CriticalError);
        result.Exception.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public void StepName_Should_ReturnConfluenceIngestion_When_Accessed()
    {
        // Act & Assert
        _sut.StepName.Should().Be("ConfluenceIngestion");
    }
}
