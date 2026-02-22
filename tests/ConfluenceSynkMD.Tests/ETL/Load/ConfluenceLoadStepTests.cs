using FluentAssertions;
using ConfluenceSynkMD.ETL.Core;
using ConfluenceSynkMD.ETL.Load;
using ConfluenceSynkMD.Models;
using ConfluenceSynkMD.Services;
using NSubstitute;
using Serilog;

namespace ConfluenceSynkMD.Tests.ETL.Load;

public class ConfluenceLoadStepTests
{
    private readonly IConfluenceApiClient _api;
    private readonly ConfluenceLoadStep _sut;

    private static readonly ConfluenceSpace TestSpace = new("space-1", "TEST", "Test Space", "hp-1");
    private static readonly DocumentMetadata EmptyMeta = new();

    public ConfluenceLoadStepTests()
    {
        _api = Substitute.For<IConfluenceApiClient>();
        var logger = Substitute.For<ILogger>();
        _sut = new ConfluenceLoadStep(_api, logger);

        // Default parent-page resolution for root-parent preflight checks.
        _api.GetPageByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var pageId = callInfo.ArgAt<string>(0);
                return pageId switch
                {
                    "hp-1" => new ConfluencePage("hp-1", "Home", "space-1", null, null, new ConfluenceVersion(1)),
                    "hp-other" => new ConfluencePage("hp-other", "Home", "space-other", null, null, new ConfluenceVersion(1)),
                    _ => null
                };
            });
    }

    private static TranslationBatchContext CreateContext()
    {
        var ctx = new TranslationBatchContext
        {
            Options = new SyncOptions(SyncMode.Upload, "/tmp", "TEST")
        };
        ctx.ResolvedSpace = TestSpace;
        return ctx;
    }

    private static ConvertedDocument CreateDoc(string title, string? pageId = null) =>
        new(
            Title: title,
            Content: "<p>Hello</p>",
            Metadata: new DocumentMetadata(PageId: pageId),
            SourcePath: $"/tmp/{title.ToLowerInvariant()}.md",
            Attachments: Array.Empty<AttachmentInfo>());

    [Fact]
    public async Task ExecuteAsync_Should_CreatePage_When_NoExistingPage()
    {
        // Arrange
        var doc = CreateDoc("New Page");
        var context = CreateContext();
        context.TransformedDocuments.Add(doc);

        var createdPage = new ConfluencePage("new-1", "New Page", "space-1", "hp-1", null, new ConfluenceVersion(1));

        _api.GetPageByTitleAsync("New Page", "space-1", Arg.Any<CancellationToken>())
            .Returns((ConfluencePage?)null);
        _api.CreatePageAsync("New Page", "<p>Hello</p>", "hp-1", "space-1", Arg.Any<CancellationToken>())
            .Returns(createdPage);

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.LoadedCount.Should().Be(1);
        await _api.Received(1).CreatePageAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_UpdatePage_When_PageIdInMetadata()
    {
        // Arrange
        var doc = CreateDoc("Existing Page", pageId: "existing-1");
        var context = CreateContext();
        context.TransformedDocuments.Add(doc);

        var existingPage = new ConfluencePage("existing-1", "Existing Page", "space-1", "hp-1", null, new ConfluenceVersion(3));
        var updatedPage = new ConfluencePage("existing-1", "Existing Page", "space-1", "hp-1", null, new ConfluenceVersion(4));

        _api.GetPageByIdAsync("existing-1", Arg.Any<CancellationToken>()).Returns(existingPage);
        _api.UpdatePageAsync("existing-1", "Existing Page", "<p>Hello</p>", 4, Arg.Any<CancellationToken>())
            .Returns(updatedPage);

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.LoadedCount.Should().Be(1);
        await _api.Received(1).UpdatePageAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Is<int>(4), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnWarning_When_PartialFailure()
    {
        // Arrange
        var doc1 = CreateDoc("Good Page");
        var doc2 = CreateDoc("Bad Page");
        var context = CreateContext();
        context.TransformedDocuments.Add(doc1);
        context.TransformedDocuments.Add(doc2);

        var createdPage = new ConfluencePage("p1", "Good Page", "space-1", "hp-1", null, new ConfluenceVersion(1));

        _api.GetPageByTitleAsync("Good Page", "space-1", Arg.Any<CancellationToken>())
            .Returns((ConfluencePage?)null);
        _api.CreatePageAsync("Good Page", Arg.Any<string>(), Arg.Any<string>(), "space-1", Arg.Any<CancellationToken>())
            .Returns(createdPage);

        _api.GetPageByTitleAsync("Bad Page", "space-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ConfluencePage?>(new HttpRequestException("API error")));

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Warning);
        context.LoadedCount.Should().Be(1);
        context.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnCriticalError_When_AllFail()
    {
        // Arrange
        var doc = CreateDoc("Failing Page");
        var context = CreateContext();
        context.TransformedDocuments.Add(doc);

        _api.GetPageByTitleAsync("Failing Page", "space-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ConfluencePage?>(new HttpRequestException("API down")));

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.CriticalError);
        context.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ResolveSpace_When_NotAlreadyCached()
    {
        // Arrange
        var context = new TranslationBatchContext
        {
            Options = new SyncOptions(SyncMode.Upload, "/tmp", "TEST")
        };
        // ResolvedSpace intentionally null
        context.TransformedDocuments.Add(CreateDoc("Page"));

        var createdPage = new ConfluencePage("p1", "Page", "space-1", "hp-1", null, new ConfluenceVersion(1));

        _api.GetSpaceByKeyAsync("TEST", Arg.Any<CancellationToken>()).Returns(TestSpace);
        _api.GetPageByTitleAsync("Page", "space-1", Arg.Any<CancellationToken>())
            .Returns((ConfluencePage?)null);
        _api.CreatePageAsync("Page", Arg.Any<string>(), "hp-1", "space-1", Arg.Any<CancellationToken>())
            .Returns(createdPage);

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.ResolvedSpace.Should().Be(TestSpace);
        await _api.Received(1).GetSpaceByKeyAsync("TEST", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_SyncLabels_When_TagsPresent()
    {
        // Arrange
        var metadata = new DocumentMetadata(Tags: ["team-alpha", "release-v2"]);
        var doc = new ConvertedDocument(
            Title: "Tagged Page", Content: "<p>Hello</p>", Metadata: metadata,
            SourcePath: "/tmp/tagged.md", Attachments: Array.Empty<AttachmentInfo>());

        var context = CreateContext();
        context.TransformedDocuments.Add(doc);

        var createdPage = new ConfluencePage("p1", "Tagged Page", "space-1", "hp-1", null, new ConfluenceVersion(1));

        _api.GetPageByTitleAsync("Tagged Page", "space-1", Arg.Any<CancellationToken>())
            .Returns((ConfluencePage?)null);
        _api.CreatePageAsync("Tagged Page", Arg.Any<string>(), "hp-1", "space-1", Arg.Any<CancellationToken>())
            .Returns(createdPage);
        _api.GetPageLabelsAsync("p1", Arg.Any<CancellationToken>())
            .Returns(new List<string>().AsReadOnly() as IReadOnlyList<string>);

        // Act
        await _sut.ExecuteAsync(context);

        // Assert
        await _api.Received(1).AddPageLabelsAsync("p1",
            Arg.Is<IEnumerable<string>>(l => l.Count() == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_SetContentProperties_When_PropertiesPresent()
    {
        // Arrange
        var props = new Dictionary<string, object> { ["custom-key"] = "custom-value" };
        var metadata = new DocumentMetadata(Properties: props);
        var doc = new ConvertedDocument(
            Title: "Props Page", Content: "<p>Hello</p>", Metadata: metadata,
            SourcePath: "/tmp/props.md", Attachments: Array.Empty<AttachmentInfo>());

        var context = CreateContext();
        context.TransformedDocuments.Add(doc);

        var createdPage = new ConfluencePage("p2", "Props Page", "space-1", "hp-1", null, new ConfluenceVersion(1));

        _api.GetPageByTitleAsync("Props Page", "space-1", Arg.Any<CancellationToken>())
            .Returns((ConfluencePage?)null);
        _api.CreatePageAsync("Props Page", Arg.Any<string>(), "hp-1", "space-1", Arg.Any<CancellationToken>())
            .Returns(createdPage);

        // Act
        await _sut.ExecuteAsync(context);

        // Assert
        await _api.Received(1).SetContentPropertyAsync("p2", "custom-key", "custom-value", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_SkipUpdate_When_ContentUnchanged()
    {
        // Arrange
        var doc = CreateDoc("Unchanged Page");
        var context = new TranslationBatchContext
        {
            Options = new SyncOptions(SyncMode.Upload, "/tmp", "TEST", SkipUpdate: true)
        };
        context.ResolvedSpace = TestSpace;
        context.TransformedDocuments.Add(doc);

        var existingBody = new ConfluencePageBody(new ConfluenceStorage("storage", "<p>Hello</p>"));
        var existingPage = new ConfluencePage("p3", "Unchanged Page", "space-1", "hp-1", existingBody, new ConfluenceVersion(2));

        _api.GetPageByTitleAsync("Unchanged Page", "space-1", Arg.Any<CancellationToken>())
            .Returns(existingPage);
        // Ancestor verification mock: page's ParentId is hp-1 = rootParentId → traceable
        _api.GetPageByIdAsync("p3", Arg.Any<CancellationToken>())
            .Returns(existingPage);

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert — should NOT call UpdatePageAsync since content is identical
        result.Status.Should().Be(PipelineResultStatus.Success);
        await _api.DidNotReceive().UpdatePageAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_UsePerDocumentSpace_When_SpaceKeyInMetadata()
    {
        // Arrange
        var otherSpace = new ConfluenceSpace("space-other", "OTHER", "Other Space", "hp-other");
        var metadata = new DocumentMetadata(SpaceKey: "OTHER");
        var doc = new ConvertedDocument(
            Title: "Cross-Space Page", Content: "<p>Hello</p>", Metadata: metadata,
            SourcePath: "/tmp/cross.md", Attachments: Array.Empty<AttachmentInfo>());

        var context = CreateContext(); // global space = "TEST"
        context.TransformedDocuments.Add(doc);

        var createdPage = new ConfluencePage("p-cross", "Cross-Space Page", "space-other", "hp-other", null, new ConfluenceVersion(1));

        _api.GetSpaceByKeyAsync("OTHER", Arg.Any<CancellationToken>())
            .Returns(otherSpace);
        _api.GetPageByTitleAsync("Cross-Space Page", "space-other", Arg.Any<CancellationToken>())
            .Returns((ConfluencePage?)null);
        _api.CreatePageAsync("Cross-Space Page", Arg.Any<string>(), "hp-other", "space-other", Arg.Any<CancellationToken>())
            .Returns(createdPage);

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Success);
        context.LoadedCount.Should().Be(1);

        // Verify page was created in the OTHER space, not TEST
        await _api.Received(1).CreatePageAsync(
            "Cross-Space Page", Arg.Any<string>(), "hp-other", "space-other", Arg.Any<CancellationToken>());

        // Verify SpaceKeyCache was populated for WriteBackStep
        context.SpaceKeyCache.Should().ContainKey("/tmp/cross.md");
        context.SpaceKeyCache["/tmp/cross.md"].Should().Be("OTHER");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnCriticalError_When_DuplicateTitle()
    {
        // Arrange — two documents with the same title but different source paths
        var context = CreateContext();
        context.TransformedDocuments.Add(CreateDoc("Shared Title"));
        context.TransformedDocuments.Add(new ConvertedDocument(
            Title: "Shared Title",
            Content: "<p>Other</p>",
            Metadata: new DocumentMetadata(),
            SourcePath: "/tmp/other-file.md",
            Attachments: Array.Empty<AttachmentInfo>()));

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert — dedup throws inside the outer try, caught as CriticalError
        result.Status.Should().Be(PipelineResultStatus.CriticalError);
        result.Message.Should().Contain("Duplicate page title");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnCriticalError_When_ConfiguredParentPageNotFound()
    {
        // Arrange
        var context = new TranslationBatchContext
        {
            Options = new SyncOptions(
                SyncMode.Upload,
                "/tmp",
                "TEST",
                ConfluenceParentId: "missing-parent")
        };
        context.ResolvedSpace = TestSpace;
        context.TransformedDocuments.Add(CreateDoc("Any Page"));

        _api.GetPageByIdAsync("missing-parent", Arg.Any<CancellationToken>())
            .Returns((ConfluencePage?)null);

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.CriticalError);
        result.Message.Should().Contain("was not found");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnCriticalError_When_ConfiguredParentInDifferentSpace()
    {
        // Arrange
        var context = new TranslationBatchContext
        {
            Options = new SyncOptions(
                SyncMode.Upload,
                "/tmp",
                "TEST",
                ConfluenceParentId: "foreign-parent")
        };
        context.ResolvedSpace = TestSpace;
        context.TransformedDocuments.Add(CreateDoc("Any Page"));

        var foreignParent = new ConfluencePage(
            "foreign-parent",
            "Foreign Parent",
            "space-foreign",
            null,
            null,
            new ConfluenceVersion(1));

        _api.GetPageByIdAsync("foreign-parent", Arg.Any<CancellationToken>())
            .Returns(foreignParent);

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.CriticalError);
        result.Message.Should().Contain("belongs to space");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Abort_When_RootPageResolutionIsAmbiguous()
    {
        // Arrange
        var context = new TranslationBatchContext
        {
            Options = new SyncOptions(
                SyncMode.Upload,
                "/tmp",
                "TEST",
                ConfluenceParentId: null,
                RootPage: "Docs Root")
        };
        context.ResolvedSpace = TestSpace;
        context.TransformedDocuments.Add(CreateDoc("Any Page"));

        _api.GetOrCreatePageUnderParentAsync(
                "Docs Root",
                "hp-1",
                "space-1",
                Arg.Any<CancellationToken>())
            .Returns(new ConfluenceRootPageResolution(
                Page: null,
                Status: ConfluenceRootPageResolutionStatus.Ambiguous,
                TotalMatches: 3,
                MatchesUnderParent: 0));

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Status.Should().Be(PipelineResultStatus.Abort);
        result.Message.Should().Contain("ambiguous");
        await _api.DidNotReceive().CreatePageAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
