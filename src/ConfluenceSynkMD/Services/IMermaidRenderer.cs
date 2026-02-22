namespace ConfluenceSynkMD.Services;

/// <summary>
/// Abstraction for Mermaid diagram rendering, enabling testability.
/// </summary>
public interface IMermaidRenderer
{
    /// <summary>
    /// Renders Mermaid source to a PNG byte array.
    /// </summary>
    Task<(byte[] PngBytes, string FileName)> RenderToPngAsync(
        string mermaidSource, CancellationToken ct = default);
}
