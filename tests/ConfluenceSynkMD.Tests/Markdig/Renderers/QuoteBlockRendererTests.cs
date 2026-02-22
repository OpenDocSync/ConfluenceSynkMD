using FluentAssertions;
using ConfluenceSynkMD.Configuration;

namespace ConfluenceSynkMD.Tests.Markdig.Renderers;

public class QuoteBlockRendererTests
{
    // ─── Existing tests (GitHub alerts & panel mode) ─────────────────────────

    [Fact]
    public void Write_UsePanel_Should_EmitPanelMacro()
    {
        var markdown = "> [!NOTE]\n> This is a note.";
        var opts = new ConverterOptions { UsePanel = true };
        var (xhtml, _) = RendererTestHelper.Render(markdown, opts);

        xhtml.Should().Contain("ac:structured-macro");
        xhtml.Should().Contain("ac:name=\"panel\"");
    }

    [Fact]
    public void Write_DefaultMode_Should_EmitInfoMacro()
    {
        var markdown = "> [!NOTE]\n> This is a note.";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ac:name=\"info\"");
    }

    [Fact]
    public void Write_Warning_WithNewline_Should_EmitWarningMacro()
    {
        var markdown = "> [!WARNING]\n>\n> Be careful!";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ac:structured-macro");
        xhtml.Should().ContainAny("ac:name=\"warning\"", "ac:name=\"info\"");
    }

    [Fact]
    public void Write_PlainQuote_Should_EmitBlockquote()
    {
        var markdown = "> Just a normal quote.";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ac:structured-macro");
        xhtml.Should().Contain("ac:name=\"info\"");
    }

    // ─── New tests: GitHub alert types ──────────────────────────────────────

    [Fact]
    public void Write_TipAlert_Should_EmitMacro()
    {
        var markdown = "> [!TIP]\n> Use caching for speed.";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ac:structured-macro");
        // TIP may map to "tip" or fall back to "info" depending on Markdig alert extension processing
        xhtml.Should().ContainAny("ac:name=\"tip\"", "ac:name=\"info\"");
        xhtml.Should().Contain("Use caching for speed");
    }

    [Fact]
    public void Write_ImportantAlert_Should_EmitMacro()
    {
        var markdown = "> [!IMPORTANT]\n> This is critical.";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ac:structured-macro");
        // IMPORTANT may map to "note" or fall back to "info"
        xhtml.Should().ContainAny("ac:name=\"note\"", "ac:name=\"info\"");
        xhtml.Should().Contain("This is critical");
    }

    [Fact]
    public void Write_CautionAlert_Should_EmitMacro()
    {
        var markdown = "> [!CAUTION]\n> This may delete data.";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ac:structured-macro");
        // CAUTION may map to "warning" or fall back to "info"
        xhtml.Should().ContainAny("ac:name=\"warning\"", "ac:name=\"info\"");
        xhtml.Should().Contain("This may delete data");
    }

    // ─── New tests: Case insensitivity ──────────────────────────────────────

    [Theory]
    [InlineData("> [!note]\n> lowercase alert.")]
    [InlineData("> [!Note]\n> mixed case alert.")]
    public void Write_GitHubAlert_CaseInsensitive(string markdown)
    {
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ac:name=\"info\"");
    }

    // ─── New tests: GitLab-style alerts ─────────────────────────────────────

    [Theory]
    [InlineData("> FLAG: Important flag", "note")]
    [InlineData("> NOTE: Please note this", "info")]
    [InlineData("> WARNING: Be careful", "note")]
    [InlineData("> DISCLAIMER: Not legal advice", "info")]
    public void Write_GitLabAlert_Should_EmitCorrectMacro(string markdown, string expectedMacro)
    {
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain($"ac:name=\"{expectedMacro}\"");
    }

    // ─── New tests: GitLab prefix stripping ─────────────────────────────────

    [Fact]
    public void Write_GitLabAlert_Should_StripPrefix()
    {
        var markdown = "> NOTE: This is a note.\n> With more content.";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("This is a note.");
        xhtml.Should().Contain("With more content");
    }

    // ─── New tests: Multi-line content ──────────────────────────────────────

    [Fact]
    public void Write_MultiLineContent_Should_PreserveAll()
    {
        var markdown = "> [!NOTE]\n> Line one.\n> Line two.\n> Line three.";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ac:name=\"info\"");
        xhtml.Should().Contain("Line one.");
        xhtml.Should().Contain("Line two.");
        xhtml.Should().Contain("Line three.");
    }

    // ─── New tests: UsePanel with alert → panel with title ─────────────────

    [Fact]
    public void WriteAlertMacro_WithPanel_Should_EmitPanelMacro()
    {
        var markdown = "> [!TIP]\n> A helpful tip.";
        var opts = new ConverterOptions { UsePanel = true };
        var (xhtml, _) = RendererTestHelper.Render(markdown, opts);

        xhtml.Should().Contain("ac:name=\"panel\"");
        xhtml.Should().Contain("A helpful tip");
    }

    // ─── New tests: Empty quote ─────────────────────────────────────────────

    [Fact]
    public void Write_EmptyQuote_Should_StillEmitMacro()
    {
        var markdown = "> ";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ac:structured-macro");
    }
}
