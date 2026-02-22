using FluentAssertions;
using Markdig;
using ConfluentSynkMD.Configuration;
using ConfluentSynkMD.Markdig;
using ConfluentSynkMD.Services;

namespace ConfluentSynkMD.Tests.Markdig.Renderers;

/// <summary>
/// Helper to render markdown through <see cref="ConfluenceRenderer"/> for testing.
/// </summary>
internal static class RendererTestHelper
{
    /// <summary>
    /// Renders a markdown string through the full Markdig + ConfluenceRenderer pipeline
    /// and returns the resulting Confluence Storage Format XHTML.
    /// </summary>
    public static (string Xhtml, ConfluenceRenderer Renderer) Render(
        string markdown,
        ConverterOptions? converterOptions = null,
        LayoutOptions? layoutOptions = null,
        Dictionary<string, string>? pageTitleMapping = null,
        string? spaceKey = null,
        Dictionary<string, string>? pageIdMapping = null,
        string? sourceDocumentPath = null)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        using var writer = new StringWriter();
        var renderer = new ConfluenceRenderer(writer)
        {
            ConverterOptions = converterOptions ?? new ConverterOptions(),
            LayoutOptions = layoutOptions ?? new LayoutOptions(),
            CurrentDocumentPath = sourceDocumentPath
        };

        // Inject LinkResolver if mapping or spaceKey is provided (TKT-005)
        if (pageTitleMapping is not null || spaceKey is not null)
        {
            renderer.LinkResolver = new LinkResolver(
                pageTitleMapping ?? new Dictionary<string, string>(),
                spaceKey,
                pageIdMapping,
                new ConfluenceUrlBuilder(renderer.ConverterOptions.WebUiLinkStrategy));
        }

        var document = global::Markdig.Markdown.Parse(markdown, pipeline);
        renderer.Render(document);
        writer.Flush();

        return (writer.ToString(), renderer);
    }
}

