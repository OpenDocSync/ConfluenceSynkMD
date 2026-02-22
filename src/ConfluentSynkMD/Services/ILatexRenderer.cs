namespace ConfluentSynkMD.Services;

/// <summary>
/// Abstraction for LaTeX formula rendering, enabling testability.
/// </summary>
public interface ILatexRenderer
{
    /// <summary>
    /// Renders LaTeX source to a PNG image.
    /// </summary>
    Task<(byte[] ImageBytes, string Format)> RenderAsync(
        string latexSource, CancellationToken ct = default);
}
