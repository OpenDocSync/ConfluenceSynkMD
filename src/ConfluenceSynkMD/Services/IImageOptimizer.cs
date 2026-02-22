namespace ConfluenceSynkMD.Services;

/// <summary>
/// Abstraction for image optimization, enabling testability.
/// </summary>
public interface IImageOptimizer
{
    /// <summary>
    /// Checks if an image should be optimized. If so, resizes it and returns the path
    /// to the optimized version. Otherwise returns the original path.
    /// </summary>
    Task<string> OptimizeImageAsync(string inputPath, CancellationToken ct = default);
}
