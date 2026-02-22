namespace ConfluenceSynkMD.Services;

/// <summary>
/// Abstraction for diagram renderers (Draw.io, PlantUML) that produce images
/// from textual source, enabling testability.
/// </summary>
public interface IDiagramRenderer
{
    /// <summary>
    /// Renders diagram source to image bytes.
    /// </summary>
    Task<(byte[] ImageBytes, string Format)> RenderAsync(
        string source, string outputFormat = "png", CancellationToken ct = default);
}
