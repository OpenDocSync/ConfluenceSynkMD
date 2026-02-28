using FluentAssertions;

namespace ConfluenceSynkMD.Tests.Markdig.Renderers;

public sealed class HtmlBlockRendererTests
{
    [Fact]
    public void DetailsBlock_Should_RenderExpandMacro()
    {
        const string markdown = """
<details markdown="1">
<summary>More Details</summary>
Inner content
</details>
""";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ac:name=\"expand\"");
        xhtml.Should().Contain("ac:name=\"title\">More Details");
        xhtml.Should().Contain("Inner content");
    }

    [Fact]
    public void DateInput_Should_ConvertToTimeElement()
    {
        const string markdown = "<input type=\"date\" value=\"2026-02-28\"/>";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<time datetime=\"2026-02-28\"/>");
    }

}
