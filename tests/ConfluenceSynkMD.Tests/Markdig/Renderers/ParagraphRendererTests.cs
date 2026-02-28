using FluentAssertions;

namespace ConfluenceSynkMD.Tests.Markdig.Renderers;

public sealed class ParagraphRendererTests
{
    [Fact]
    public void NormalParagraph_Should_RenderPTag()
    {
        const string markdown = "Hello paragraph.";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<p>");
        xhtml.Should().Contain("Hello paragraph.");
        xhtml.Should().Contain("</p>");
    }

}
