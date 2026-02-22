using System.Diagnostics;
using Serilog;

namespace ConfluentSynkMD.Services;

/// <summary>
/// Renders Draw.io (diagrams.net) XML content to PNG or SVG images.
/// Requires the Draw.io Desktop app or drawio-export CLI tool to be available.
/// </summary>
public sealed class DrawioRenderer : IDiagramRenderer
{
    private readonly ILogger _logger;

    public DrawioRenderer(ILogger logger)
    {
        _logger = logger.ForContext<DrawioRenderer>();
    }

    /// <summary>
    /// Renders Draw.io XML source to image bytes.
    /// </summary>
    /// <param name="drawioXml">The Draw.io XML content.</param>
    /// <param name="outputFormat">Output format: "png" or "svg".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple of image bytes and the format used.</returns>
    public async Task<(byte[] ImageBytes, string Format)> RenderAsync(
        string source, string outputFormat = "png", CancellationToken ct = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ConfluentSynkMD-drawio");
        Directory.CreateDirectory(tempDir);

        var inputFile = Path.Combine(tempDir, $"diagram-{Guid.NewGuid():N}.drawio");
        var outputFile = Path.ChangeExtension(inputFile, outputFormat);

        try
        {
            await File.WriteAllTextAsync(inputFile, source, ct);

            // Try drawio CLI (drawio-export or drawio desktop in headless mode)
            var drawioCmd = FindDrawioCommand();
            if (drawioCmd is null)
            {
                throw new InvalidOperationException(
                    "Draw.io CLI not found. Install 'drawio-desktop' or set DRAWIO_CMD environment variable.");
            }

            var psi = new ProcessStartInfo
            {
                FileName = drawioCmd,
                Arguments = $"--export --format {outputFormat} --output \"{outputFile}\" \"{inputFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start Draw.io process.");

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(ct);
                throw new InvalidOperationException($"Draw.io export failed (exit {process.ExitCode}): {stderr}");
            }

            var imageBytes = await File.ReadAllBytesAsync(outputFile, ct);
            _logger.Debug("Draw.io diagram rendered: {Size} bytes ({Format}).", imageBytes.Length, outputFormat);
            return (imageBytes, outputFormat);
        }
        finally
        {
            TryDeleteFile(inputFile);
            TryDeleteFile(outputFile);
        }
    }

    /// <summary>
    /// Renders a .drawio file to image bytes.
    /// </summary>
    public async Task<(byte[] ImageBytes, string Format)> RenderFileAsync(
        string filePath, string outputFormat = "png", CancellationToken ct = default)
    {
        var xml = await File.ReadAllTextAsync(filePath, ct);
        return await RenderAsync(xml, outputFormat, ct);
    }

    private static string? FindDrawioCommand()
    {
        // Check environment variable first
        var envCmd = Environment.GetEnvironmentVariable("DRAWIO_CMD");
        if (!string.IsNullOrEmpty(envCmd) && File.Exists(envCmd))
            return envCmd;

        // Common paths
        var candidates = new[]
        {
            "drawio",
            @"C:\Program Files\draw.io\draw.io.exe",
            @"/Applications/draw.io.app/Contents/MacOS/draw.io",
            @"/usr/bin/drawio",
        };

        foreach (var candidate in candidates)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "--help",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var p = Process.Start(psi);
                if (p is not null)
                {
                    p.Kill();
                    return candidate;
                }
            }
            catch
            {
                // Not found, try next
            }
        }

        return null;
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }
}
