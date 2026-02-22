using FluentAssertions;
using ConfluentSynkMD.Configuration;

namespace ConfluentSynkMD.Tests.Markdig.Renderers;

public class HeadingRendererTests
{
    [Fact]
    public void Write_BasicHeading_Should_EmitHTag()
    {
        var (xhtml, _) = RendererTestHelper.Render("# Hello World");

        xhtml.Should().Contain("<h1>");
        xhtml.Should().Contain("Hello World");
        xhtml.Should().Contain("</h1>");
    }

    [Fact]
    public void Write_H2_Should_EmitCorrectLevel()
    {
        var (xhtml, _) = RendererTestHelper.Render("## Subtitle");

        xhtml.Should().Contain("<h2>");
        xhtml.Should().Contain("</h2>");
    }

    [Fact]
    public void Write_HeadingAnchors_Should_InjectAnchorMacro()
    {
        var opts = new ConverterOptions { HeadingAnchors = true };
        var (xhtml, _) = RendererTestHelper.Render("## My Section", opts);

        xhtml.Should().Contain("ac:name=\"anchor\"");
        xhtml.Should().Contain("my-section");
    }

    [Fact]
    public void Write_SkipTitleHeading_Should_OmitFirstH1()
    {
        var opts = new ConverterOptions { SkipTitleHeading = true };
        var (xhtml, renderer) = RendererTestHelper.Render("# Page Title\n\nSome text\n\n## Section", opts);

        // Verify the renderer tracked that it saw the first heading
        renderer.FirstHeadingSeen.Should().BeTrue();
        // The H1 content should NOT appear as a heading (it becomes the page title)
        xhtml.Should().NotContain("Page Title</h1>");
        xhtml.Should().Contain("<h2>");
    }

    [Fact]
    public void Write_SkipTitleHeading_Should_KeepSecondH1()
    {
        var opts = new ConverterOptions { SkipTitleHeading = true };
        var (xhtml, _) = RendererTestHelper.Render("# First\n\n# Second");

        xhtml.Should().Contain("<h1>");
        xhtml.Should().Contain("Second");
    }

    [Fact]
    public void Write_SkipTitleHeading_Should_KeepH2()
    {
        var opts = new ConverterOptions { SkipTitleHeading = true };
        var (xhtml, _) = RendererTestHelper.Render("# Title\n\n## Stays");

        xhtml.Should().Contain("<h2>");
        xhtml.Should().Contain("Stays");
    }
}
