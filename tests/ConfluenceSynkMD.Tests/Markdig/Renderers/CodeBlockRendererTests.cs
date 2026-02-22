using FluentAssertions;
using ConfluenceSynkMD.Configuration;

namespace ConfluenceSynkMD.Tests.Markdig.Renderers;

public class CodeBlockRendererTests
{
    [Fact]
    public void Write_StandardCodeBlock_Should_EmitCodeMacro()
    {
        var markdown = "```python\nprint('hello')\n```";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ac:name=\"code\"");
        xhtml.Should().Contain("ac:name=\"language\">py</ac:parameter>");
        xhtml.Should().Contain("print('hello')");
    }

    [Fact]
    public void Write_MermaidBlock_Should_EmitImageAndCollapsedSource()
    {
        var markdown = "```mermaid\ngraph TD\n    A-->B\n```";
        var opts = new ConverterOptions { RenderMermaid = true };
        var (xhtml, renderer) = RendererTestHelper.Render(markdown, opts);

        xhtml.Should().Contain("<ac:image>");
        xhtml.Should().Contain("ri:attachment");
        xhtml.Should().Contain("collapse\">true</ac:parameter>");
        renderer.MermaidDiagrams.Should().HaveCount(1);
    }

    [Fact]
    public void Write_MermaidBlock_WhenDisabled_Should_EmitStandardCodeMacro()
    {
        var markdown = "```mermaid\ngraph TD\n    A-->B\n```";
        var opts = new ConverterOptions { RenderMermaid = false };
        var (xhtml, renderer) = RendererTestHelper.Render(markdown, opts);

        xhtml.Should().Contain("ac:name=\"code\"");
        xhtml.Should().Contain("ac:name=\"language\">mermaid</ac:parameter>");
        renderer.MermaidDiagrams.Should().BeEmpty();
    }

    [Fact]
    public void Write_DrawioBlock_Should_RegisterInDiagramList()
    {
        var markdown = "```drawio\n<mxGraphModel>test</mxGraphModel>\n```";
        var opts = new ConverterOptions { RenderDrawio = true };
        var (xhtml, renderer) = RendererTestHelper.Render(markdown, opts);

        renderer.DrawioDiagrams.Should().HaveCount(1);
        xhtml.Should().Contain("<ac:image>");
    }

    [Fact]
    public void Write_PlantUmlBlock_Should_AcceptPumlAlias()
    {
        var markdown = "```puml\n@startuml\nBob -> Alice\n@enduml\n```";
        var opts = new ConverterOptions { RenderPlantuml = true };
        var (_, renderer) = RendererTestHelper.Render(markdown, opts);

        renderer.PlantUmlDiagrams.Should().HaveCount(1);
    }

    [Fact]
    public void Write_LatexBlock_Should_AcceptMathAlias()
    {
        var markdown = "```math\nE = mc^2\n```";
        var opts = new ConverterOptions { RenderLatex = true };
        var (_, renderer) = RendererTestHelper.Render(markdown, opts);

        renderer.LatexFormulas.Should().HaveCount(1);
    }

    [Fact]
    public void Write_LineNumbers_Should_EmitLinenumbersParameter()
    {
        var markdown = "```python\nx = 1\n```";
        var opts = new ConverterOptions { CodeLineNumbers = true };
        var (xhtml, _) = RendererTestHelper.Render(markdown, opts);

        xhtml.Should().Contain("ac:name=\"linenumbers\">true</ac:parameter>");
    }

    [Fact]
    public void Write_ForceValidLanguage_UnknownLang_Should_FallbackToNone()
    {
        var markdown = "```brainfuck\n++++++++++.\n```";
        var opts = new ConverterOptions { ForceValidLanguage = true };
        var (xhtml, _) = RendererTestHelper.Render(markdown, opts);

        xhtml.Should().Contain("ac:name=\"language\">none</ac:parameter>");
    }

    [Fact]
    public void Write_ForceValidLanguage_KnownLang_Should_Passthrough()
    {
        var markdown = "```python\nx = 1\n```";
        var opts = new ConverterOptions { ForceValidLanguage = true };
        var (xhtml, _) = RendererTestHelper.Render(markdown, opts);

        xhtml.Should().Contain("ac:name=\"language\">py</ac:parameter>");
    }

    [Fact]
    public void Write_DiagramHash_Should_BeDeterministic()
    {
        var markdown = "```mermaid\ngraph TD\n    A-->B\n```";
        var opts = new ConverterOptions { RenderMermaid = true };

        var (_, renderer1) = RendererTestHelper.Render(markdown, opts);
        var (_, renderer2) = RendererTestHelper.Render(markdown, opts);

        renderer1.MermaidDiagrams[0].FileName
            .Should().Be(renderer2.MermaidDiagrams[0].FileName);
    }
}
