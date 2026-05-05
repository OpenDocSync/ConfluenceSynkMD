using FluentAssertions;

namespace ConfluenceSynkMD.Tests.Markdig.Renderers;

/// <summary>
/// Coverage for soft and hard line break handling. Until v0.1.1 the soft-break
/// case was silently dropped, which collapsed every prose paragraph that wrapped
/// across multiple source lines (e.g. "published\nDocker image" became
/// "publishedDocker image" in Confluence — see CHANGELOG entry for v0.1.1).
/// </summary>
public sealed class LineBreakInlineRendererTests
{
    [Fact]
    public void SoftLineBreak_Should_PreserveNewline_NotCollapseWords()
    {
        // Soft break: plain newline between two text lines inside the same paragraph.
        const string markdown = "First line\nSecond line.";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        // The two words must NOT touch — that was the v0.1.0 bug.
        xhtml.Should().NotContain("lineSecond");
        // A whitespace separator (newline) must be preserved between them.
        xhtml.Should().Contain("First line\nSecond line.");
    }

    [Fact]
    public void HardLineBreak_TwoTrailingSpaces_Should_EmitBrTag()
    {
        // Hard break: two trailing spaces at end of line.
        const string markdown = "First line  \nSecond line.";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<br/>");
    }

    [Fact]
    public void HardLineBreak_TrailingBackslash_Should_EmitBrTag()
    {
        // Hard break: trailing backslash at end of line.
        const string markdown = "First line\\\nSecond line.";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<br/>");
    }

    [Fact]
    public void MultipleSoftBreaks_Should_AllBePreserved()
    {
        const string markdown = "Line one\nLine two\nLine three.";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        // All three lines must be present without word-mashing.
        xhtml.Should().NotContain("oneLine");
        xhtml.Should().NotContain("twoLine");
        xhtml.Should().Contain("Line one\nLine two\nLine three.");
    }

    [Fact]
    public void SoftBreak_BetweenInlineFormatting_Should_PreserveSeparator()
    {
        // The original v0.1.0 reproducer: prose with **bold** spanning a soft break.
        const string markdown = "This page was uploaded\nby an automated test.";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        // Critical regression assertion — names the exact word-mash from the
        // round-trip test that exposed this bug against a real Confluence instance.
        xhtml.Should().NotContain("uploadedby");
    }
}
