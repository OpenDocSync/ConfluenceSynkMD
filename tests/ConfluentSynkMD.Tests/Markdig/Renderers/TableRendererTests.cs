using FluentAssertions;
using ConfluentSynkMD.Configuration;

namespace ConfluentSynkMD.Tests.Markdig.Renderers;

public class TableRendererTests
{
    private const string SimpleTable = @"
| Col1 | Col2 |
|------|------|
| A    | B    |
| C    | D    |
";

    [Fact]
    public void Write_TableWidth_Should_EmitStyleAttribute()
    {
        var layout = new LayoutOptions { TableWidth = 800 };
        var (xhtml, _) = RendererTestHelper.Render(SimpleTable, layoutOptions: layout);

        xhtml.Should().Contain("width: 800px");
    }

    [Fact]
    public void Write_FixedMode_Should_EmitTableLayoutFixed()
    {
        var layout = new LayoutOptions { TableDisplayMode = "fixed" };
        var (xhtml, _) = RendererTestHelper.Render(SimpleTable, layoutOptions: layout);

        xhtml.Should().Contain("table-layout: fixed");
    }

    [Fact]
    public void Write_ResponsiveMode_Default_Should_EmitTableLayoutAuto()
    {
        var (xhtml, _) = RendererTestHelper.Render(SimpleTable);

        xhtml.Should().Contain("table-layout: auto");
    }

    [Fact]
    public void Write_Table_Should_EmitHeaderAndDataCells()
    {
        var (xhtml, _) = RendererTestHelper.Render(SimpleTable);

        xhtml.Should().Contain("<th>");
        xhtml.Should().Contain("<td>");
    }

    [Fact]
    public void Write_ContentAlignment_Should_EmitTextAlign()
    {
        var layout = new LayoutOptions { ContentAlignment = "center" };
        var (xhtml, _) = RendererTestHelper.Render(SimpleTable, layoutOptions: layout);

        xhtml.Should().Contain("text-align: center");
    }
}
