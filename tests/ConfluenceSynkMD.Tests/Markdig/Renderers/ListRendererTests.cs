using FluentAssertions;

namespace ConfluenceSynkMD.Tests.Markdig.Renderers;

public sealed class ListRendererTests
{
    [Fact]
    public void UnorderedList_Should_RenderUlAndLi()
    {
        const string markdown = "- one\n- two";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<ul>");
        xhtml.Should().Contain("<li>");
        xhtml.Should().Contain("one");
        xhtml.Should().Contain("two");
    }

    [Fact]
    public void OrderedList_Should_RenderOlAndLi()
    {
        const string markdown = "1. first\n2. second";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<ol>");
        xhtml.Should().Contain("first");
        xhtml.Should().Contain("second");
    }

    [Fact]
    public void TaskList_Should_RenderConfluenceTaskMacros()
    {
        const string markdown = "- [x] done\n- [ ] todo";

        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<ac:task-list>");
        xhtml.Should().Contain("<ac:task-status>complete</ac:task-status>");
        xhtml.Should().Contain("<ac:task-status>incomplete</ac:task-status>");
        xhtml.Should().Contain("done");
        xhtml.Should().Contain("todo");
    }
}
