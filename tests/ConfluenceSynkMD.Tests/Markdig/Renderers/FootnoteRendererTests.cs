using FluentAssertions;

namespace ConfluenceSynkMD.Tests.Markdig.Renderers;

public sealed class FootnoteRendererTests
{
    [Fact]
    public void FootnoteReferenceAndDefinition_Should_RenderAnchorsAndLinks()
    {
        const string markdown = "Reference[^note]\n\n[^note]: Footnote text.";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("footnote-ref-^note");
        xhtml.Should().Contain("footnote-def-^note");
        xhtml.Should().Contain("<ac:link ac:anchor=\"footnote-def-^note\">");
        xhtml.Should().Contain("<![CDATA[↩]]>");
    }

    [Fact]
    public void MultipleReferences_Should_RenderIndexedBackLinks()
    {
        const string markdown = "A[^n] and again[^n]\n\n[^n]: Shared footnote.";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("footnote-ref-^n-2");
        xhtml.Should().Contain("<![CDATA[↩²]]>");
    }
}
