using ConfluenceSynkMD.ETL.Core;
using ConfluenceSynkMD.Models;
using FluentAssertions;

namespace ConfluenceSynkMD.Tests.ETL.Core;

public sealed class TranslationBatchContextTests
{
    [Fact]
    public void NewContext_Should_InitializeCollectionsAndDefaults()
    {
        var options = new SyncOptions(SyncMode.Upload, "./docs", "DEV");

        var sut = new TranslationBatchContext
        {
            Options = options
        };

        sut.Options.Should().Be(options);
        sut.ConverterOptions.Should().NotBeNull();
        sut.LayoutOptions.Should().NotBeNull();
        sut.ExtractedDocumentNodes.Should().BeEmpty();
        sut.ExtractedConfluencePages.Should().BeEmpty();
        sut.TransformedDocuments.Should().BeEmpty();
        sut.PageIdCache.Should().BeEmpty();
        sut.SpaceKeyCache.Should().BeEmpty();
        sut.StepResults.Should().BeEmpty();
        sut.UnresolvedLinkSamples.Should().BeEmpty();
        sut.WebUiPageIdFallbackSamples.Should().BeEmpty();
        sut.LoadedCount.Should().Be(0);
        sut.FailedCount.Should().Be(0);
        sut.UnresolvedLinkFallbackCount.Should().Be(0);
        sut.WebUiPageIdFallbackCount.Should().Be(0);
        sut.ResolvedSpace.Should().BeNull();
    }

    [Fact]
    public void ContextState_Should_BeMutableAcrossPipelinePhases()
    {
        var sut = new TranslationBatchContext
        {
            Options = new SyncOptions(SyncMode.Download, "./sync", "DOCS")
        };

        sut.PageIdCache["a.md"] = "123";
        sut.SpaceKeyCache["a.md"] = "DOCS";
        sut.UnresolvedLinkSamples.Add("source=a.md link=b.md fallback=b");
        sut.WebUiPageIdFallbackSamples.Add("source=a.md link=b.md");
        sut.UnresolvedLinkFallbackCount++;
        sut.WebUiPageIdFallbackCount++;
        sut.LoadedCount = 7;
        sut.FailedCount = 1;

        sut.PageIdCache.Should().ContainKey("a.md").WhoseValue.Should().Be("123");
        sut.SpaceKeyCache.Should().ContainKey("a.md").WhoseValue.Should().Be("DOCS");
        sut.UnresolvedLinkSamples.Should().HaveCount(1);
        sut.WebUiPageIdFallbackSamples.Should().HaveCount(1);
        sut.UnresolvedLinkFallbackCount.Should().Be(1);
        sut.WebUiPageIdFallbackCount.Should().Be(1);
        sut.LoadedCount.Should().Be(7);
        sut.FailedCount.Should().Be(1);
    }
}