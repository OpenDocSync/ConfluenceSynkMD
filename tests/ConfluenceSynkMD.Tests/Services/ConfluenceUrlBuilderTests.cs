using ConfluenceSynkMD.Services;
using FluentAssertions;

namespace ConfluenceSynkMD.Tests.Services;

public sealed class ConfluenceUrlBuilderTests
{
    [Fact]
    public void Constructor_Should_DefaultToSpaceTitleStrategy_When_Null()
    {
        var sut = new ConfluenceUrlBuilder();

        sut.Strategy.Should().Be(ConfluenceUrlBuilder.StrategySpaceTitle);
    }

    [Fact]
    public void Constructor_Should_NormalizeStrategy_When_ValueHasWhitespaceAndCase()
    {
        var sut = new ConfluenceUrlBuilder("  PAGE-ID  ");

        sut.Strategy.Should().Be(ConfluenceUrlBuilder.StrategyPageId);
    }

    [Fact]
    public void BuildPageUrl_Should_UsePageIdUrl_When_PageIdStrategyAndPageIdPresent()
    {
        var sut = new ConfluenceUrlBuilder(ConfluenceUrlBuilder.StrategyPageId);

        var result = sut.BuildPageUrl("Ignored Title", "DEV", "section-1", "12345");

        result.Should().Be("/wiki/pages/viewpage.action?pageId=12345#section-1");
    }

    [Fact]
    public void BuildPageUrl_Should_EncodePageId_When_ContainsReservedCharacters()
    {
        var sut = new ConfluenceUrlBuilder(ConfluenceUrlBuilder.StrategyPageId);

        var result = sut.BuildPageUrl("Ignored", "DEV", null, "abc 123/42");

        result.Should().Be("/wiki/pages/viewpage.action?pageId=abc%20123%2F42");
    }

    [Fact]
    public void BuildPageUrl_Should_FallbackToSpaceTitle_When_PageIdMissing()
    {
        var sut = new ConfluenceUrlBuilder(ConfluenceUrlBuilder.StrategyPageId);

        var result = sut.BuildPageUrl("Install Guide", "DOCS", null, null);

        result.Should().Be("/wiki/display/DOCS/Install%20Guide");
    }

    [Fact]
    public void BuildPageUrl_Should_UseSpaceAndTitle_When_SpaceKeyPresent()
    {
        var sut = new ConfluenceUrlBuilder();

        var result = sut.BuildPageUrl("API & Usage", "DEV TEAM", null, null);

        result.Should().Be("/wiki/display/DEV%20TEAM/API%20%26%20Usage");
    }

    [Fact]
    public void BuildPageUrl_Should_OmitSpaceSegment_When_SpaceKeyMissing()
    {
        var sut = new ConfluenceUrlBuilder();

        var result = sut.BuildPageUrl("Home", null, "top", null);

        result.Should().Be("/wiki/display/Home#top");
    }
}