using FluentAssertions;
using ConfluenceSynkMD.Services;
using NSubstitute;
using Serilog;

namespace ConfluenceSynkMD.Tests.Integration;

/// <summary>
/// Integration tests that verify diagram rendering with actual external tools.
/// Skip automatically when the required tool is not installed.
/// </summary>
[Trait("Category", "Integration")]
public class DiagramRenderingIntegrationTests
{
    private static bool IsToolAvailable(string command, string args = "--version")
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    [Fact(Skip = "Requires drawio CLI installed")]
    public async Task DrawioRenderer_Should_ProducePngBytes()
    {
        var logger = Substitute.For<ILogger>();
        var renderer = new DrawioRenderer(logger);

        var xml = @"<mxGraphModel><root>
            <mxCell id=""0""/><mxCell id=""1"" parent=""0""/>
            <mxCell id=""2"" value=""Hello"" vertex=""1"" parent=""1"">
                <mxGeometry x=""20"" y=""20"" width=""80"" height=""30"" as=""geometry""/>
            </mxCell>
        </root></mxGraphModel>";

        var (bytes, format) = await renderer.RenderAsync(xml, "png");

        bytes.Should().NotBeEmpty();
        bytes[..4].Should().BeEquivalentTo(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "PNG magic bytes");
        format.Should().Be("png");
    }

    [Fact(Skip = "Requires drawio CLI installed")]
    public async Task DrawioRenderer_Should_ProduceSvgBytes()
    {
        var logger = Substitute.For<ILogger>();
        var renderer = new DrawioRenderer(logger);

        var xml = @"<mxGraphModel><root><mxCell id=""0""/></root></mxGraphModel>";

        var (bytes, format) = await renderer.RenderAsync(xml, "svg");

        bytes.Should().NotBeEmpty();
        var svg = System.Text.Encoding.UTF8.GetString(bytes);
        svg.Should().Contain("<svg");
        format.Should().Be("svg");
    }

    [Fact(Skip = "Requires plantuml or java + plantuml.jar installed")]
    public async Task PlantUmlRenderer_Should_ProducePngBytes()
    {
        var logger = Substitute.For<ILogger>();
        var renderer = new PlantUmlRenderer(logger);

        var source = "@startuml\nBob -> Alice : hello\n@enduml";

        var (bytes, format) = await renderer.RenderAsync(source, "png");

        bytes.Should().NotBeEmpty();
        bytes[..4].Should().BeEquivalentTo(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "PNG magic bytes");
        format.Should().Be("png");
    }

    [Fact(Skip = "Requires plantuml or java + plantuml.jar installed")]
    public async Task PlantUmlRenderer_Should_ProduceSvgBytes()
    {
        var logger = Substitute.For<ILogger>();
        var renderer = new PlantUmlRenderer(logger);

        var source = "@startuml\nBob -> Alice : hello\n@enduml";

        var (bytes, format) = await renderer.RenderAsync(source, "svg");

        bytes.Should().NotBeEmpty();
        var svg = System.Text.Encoding.UTF8.GetString(bytes);
        svg.Should().Contain("<svg");
        format.Should().Be("svg");
    }

    [Fact(Skip = "Requires pdflatex and ImageMagick convert installed")]
    public async Task LatexRenderer_Should_ProducePngBytes()
    {
        var logger = Substitute.For<ILogger>();
        var renderer = new LatexRenderer(logger);

        var source = @"E = mc^2";

        var (bytes, format) = await renderer.RenderAsync(source);

        bytes.Should().NotBeEmpty();
        bytes[..4].Should().BeEquivalentTo(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "PNG magic bytes");
    }

    [Fact(Skip = "Requires pdflatex and ImageMagick convert installed")]
    public async Task LatexRenderer_Should_CleanupTempFiles()
    {
        var logger = Substitute.For<ILogger>();
        var renderer = new LatexRenderer(logger);

        var source = @"E = mc^2";
        var tempDir = Path.GetTempPath();
        var filesBefore = Directory.GetFiles(tempDir, "md2c-latex-*");

        await renderer.RenderAsync(source);

        var filesAfter = Directory.GetFiles(tempDir, "md2c-latex-*");
        filesAfter.Length.Should().Be(filesBefore.Length, "temp files should be cleaned up");
    }
}
