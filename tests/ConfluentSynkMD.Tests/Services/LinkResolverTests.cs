using FluentAssertions;
using ConfluentSynkMD.Services;

namespace ConfluentSynkMD.Tests.Services;

/// <summary>
/// Unit tests for <see cref="LinkResolver"/> covering TKT-002 (title resolution),
/// TKT-003 (WebUI URLs), and TKT-005 (link classification).
/// </summary>
public class LinkResolverTests
{
    private static readonly Dictionary<string, string> TestMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["guides/setup.md"] = "Installation Guide",
        ["architecture/overview.md"] = "Architecture Overview",
        ["index.md"] = "Home Page",
        ["welcome-to-ConfluentSynkMD/index.md"] = "Welcome Home",
        ["welcome-to-ConfluentSynkMD/features.md"] = "Feature Overview"
    };

    private static LinkResolver CreateResolver(
        Dictionary<string, string>? mapping = null,
        string? spaceKey = null) =>
        new(mapping ?? TestMapping, spaceKey);

    // ─── Link Classification ────────────────────────────────────────────────

    [Theory]
    [InlineData("https://google.com")]
    [InlineData("http://example.org")]
    [InlineData("mailto:test@example.com")]
    public void Resolve_ExternalUrl_Should_ReturnExternalType(string url)
    {
        var result = CreateResolver().Resolve(url, webUiMode: false);
        result.Type.Should().Be(LinkType.External);
        result.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void Resolve_AnchorLink_Should_ReturnAnchorType()
    {
        var result = CreateResolver().Resolve("#section", webUiMode: false);
        result.Type.Should().Be(LinkType.Anchor);
        result.Fragment.Should().Be("section");
    }

    [Fact]
    public void Resolve_MdLink_Should_ReturnInternalPageType()
    {
        var result = CreateResolver().Resolve("guides/setup.md", webUiMode: false);
        result.Type.Should().Be(LinkType.InternalPage);
    }

    [Fact]
    public void Resolve_RelativeNonMdFile_Should_ReturnAttachmentType()
    {
        var result = CreateResolver().Resolve("assets/diagram.png", webUiMode: false);
        result.Type.Should().Be(LinkType.Attachment);
        result.ResolvedTitle.Should().Be("diagram.png");
    }

    // ─── TKT-002: Title Resolution ──────────────────────────────────────────

    [Fact]
    public void Resolve_MdLink_InMapping_Should_UseResolvedTitle()
    {
        var result = CreateResolver().Resolve("guides/setup.md", webUiMode: false);
        result.ResolvedTitle.Should().Be("Installation Guide");
        result.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void Resolve_MdLink_NotInMapping_Should_FallbackToFilename()
    {
        var result = CreateResolver().Resolve("unknown/readme.md", webUiMode: false);
        result.ResolvedTitle.Should().Be("readme");
        result.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void Resolve_MdLink_WithLeadingDotSlash_Should_StillMatch()
    {
        var result = CreateResolver().Resolve("./guides/setup.md", webUiMode: false);
        result.ResolvedTitle.Should().Be("Installation Guide");
    }

    [Fact]
    public void Resolve_MdLink_WithFragment_Should_ExtractBoth()
    {
        var result = CreateResolver().Resolve("guides/setup.md#prerequisites", webUiMode: false);
        result.ResolvedTitle.Should().Be("Installation Guide");
        result.Fragment.Should().Be("prerequisites");
    }

    // ─── TKT-003: WebUI URL Construction ────────────────────────────────────

    [Fact]
    public void Resolve_WebUi_WithSpaceKey_Should_IncludeSpaceInUrl()
    {
        var result = CreateResolver(spaceKey: "DEV").Resolve("guides/setup.md", webUiMode: true);
        result.DisplayUrl.Should().Contain("/wiki/display/DEV/Installation%20Guide");
    }

    [Fact]
    public void Resolve_WebUi_WithoutSpaceKey_Should_OmitSpace()
    {
        var result = CreateResolver(spaceKey: null).Resolve("guides/setup.md", webUiMode: true);
        result.DisplayUrl.Should().Be("/wiki/display/Installation%20Guide");
    }

    [Fact]
    public void Resolve_WebUi_WithFragment_Should_AppendFragment()
    {
        var result = CreateResolver(spaceKey: "DOCS").Resolve("index.md#welcome", webUiMode: true);
        result.DisplayUrl.Should().Be("/wiki/display/DOCS/Home%20Page#welcome");
    }

    [Fact]
    public void Resolve_NonWebUi_Should_NotGenerateDisplayUrl()
    {
        var result = CreateResolver(spaceKey: "DEV").Resolve("guides/setup.md", webUiMode: false);
        result.DisplayUrl.Should().BeNull();
    }

    // ─── Case-Insensitive Matching ──────────────────────────────────────────

    [Fact]
    public void Resolve_CaseInsensitive_Should_MatchMapping()
    {
        var result = CreateResolver().Resolve("GUIDES/SETUP.MD", webUiMode: false);
        result.ResolvedTitle.Should().Be("Installation Guide");
        result.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void Resolve_MdLink_WithParentTraversalAndSourcePath_Should_ResolveCorrectTarget()
    {
        var result = CreateResolver().Resolve(
            "../index.md",
            webUiMode: false,
            sourcePath: "welcome-to-ConfluentSynkMD/sub/guide.md");

        result.ResolvedTitle.Should().Be("Welcome Home");
        result.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void Resolve_MdLink_WithDotSlashAndSourcePath_Should_ResolveSibling()
    {
        var result = CreateResolver().Resolve(
            "./features.md",
            webUiMode: false,
            sourcePath: "welcome-to-ConfluentSynkMD/index.md");

        result.ResolvedTitle.Should().Be("Feature Overview");
        result.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void Resolve_WebUi_WithPageIdStrategy_Should_UsePageIdUrl()
    {
        var pageIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["guides/setup.md"] = "123456"
        };

        var resolver = new LinkResolver(
            TestMapping,
            "DEV",
            pageIds,
            new ConfluenceUrlBuilder("page-id"));

        var result = resolver.Resolve("guides/setup.md#install", webUiMode: true);
        result.DisplayUrl.Should().Be("/wiki/pages/viewpage.action?pageId=123456#install");
    }

    [Fact]
    public void Resolve_WebUi_PageIdStrategyWithoutPageId_Should_FallbackToTitleUrl()
    {
        var resolver = new LinkResolver(
            TestMapping,
            "DEV",
            pageIdMapping: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            urlBuilder: new ConfluenceUrlBuilder("page-id"));

        var result = resolver.Resolve("guides/setup.md", webUiMode: true);
        result.DisplayUrl.Should().Be("/wiki/display/DEV/Installation%20Guide");
    }

    [Fact]
    public void Resolve_UnresolvedLink_Should_TriggerDiagnosticsCallback()
    {
        string? observedLink = null;
        string? observedSource = null;
        string? observedFallback = null;

        var resolver = new LinkResolver(
            TestMapping,
            "DEV",
            onUnresolvedLink: (linkPath, sourcePath, fallbackTitle) =>
            {
                observedLink = linkPath;
                observedSource = sourcePath;
                observedFallback = fallbackTitle;
            });

        var result = resolver.Resolve("unknown/path.md", webUiMode: false, sourcePath: "docs/start.md");

        result.IsResolved.Should().BeFalse();
        observedLink.Should().Be("unknown/path.md");
        observedSource.Should().Be("docs/start.md");
        observedFallback.Should().Be("path");
    }

    [Fact]
    public void Resolve_WebUiPageIdFallback_Should_TriggerDiagnosticsCallback()
    {
        string? observedLink = null;
        string? observedSource = null;

        var resolver = new LinkResolver(
            TestMapping,
            "DEV",
            pageIdMapping: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            urlBuilder: new ConfluenceUrlBuilder("page-id"),
            onWebUiPageIdFallback: (linkPath, sourcePath) =>
            {
                observedLink = linkPath;
                observedSource = sourcePath;
            });

        resolver.Resolve("guides/setup.md", webUiMode: true, sourcePath: "docs/index.md");

        observedLink.Should().Be("guides/setup.md");
        observedSource.Should().Be("docs/index.md");
    }
}
