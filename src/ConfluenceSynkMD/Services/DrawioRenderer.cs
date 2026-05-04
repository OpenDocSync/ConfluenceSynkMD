using System.Diagnostics;
using Serilog;

namespace ConfluenceSynkMD.Services;

/// <summary>
/// Renders Draw.io (diagrams.net) XML content to PNG or SVG images.
/// Requires drawio-desktop on PATH (or via <c>DRAWIO_CMD</c>). On Linux it
/// runs headless under Xvfb — the published Docker image starts an Xvfb-once
/// instance via <c>entrypoint.sh</c> and sets <c>DISPLAY=:99</c>;
/// outside Docker, set <c>DRAWIO_CMD="xvfb-run -a drawio --no-sandbox --disable-gpu"</c>
/// (multi-token values are parsed by <see cref="RendererCommandResolver"/>).
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
        var tempDir = Path.Combine(Path.GetTempPath(), "ConfluenceSynkMD-drawio");
        Directory.CreateDirectory(tempDir);

        var inputFile = Path.Combine(tempDir, $"diagram-{Guid.NewGuid():N}.drawio");
        var outputFile = Path.ChangeExtension(inputFile, outputFormat);

        try
        {
            await File.WriteAllTextAsync(inputFile, source, ct);

            // Resolve the drawio invocation — supports DRAWIO_CMD with extra args
            // (e.g. "drawio --no-sandbox --disable-gpu") via the shared resolver.
            var resolved = ResolveDrawioCommand();
            if (resolved is null)
            {
                throw new InvalidOperationException(
                    "Draw.io CLI not found. Install 'drawio-desktop' or set DRAWIO_CMD environment variable.");
            }

            var perCallArgs = $"--export --format {outputFormat} --output \"{outputFile}\" \"{inputFile}\"";

            var psi = new ProcessStartInfo
            {
                FileName = resolved.Value.FileName,
                Arguments = RendererCommandResolver.CombineArgs(resolved.Value.ArgsPrefix, perCallArgs),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = StartDrawioProcess(psi);

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(ct);
                throw new InvalidOperationException($"Draw.io export failed (exit {process.ExitCode}): {stderr}");
            }

            // drawio-desktop sometimes exits 0 without producing the output file
            // — typically when the input mxfile is missing page-level metadata
            // the editor would normally write (dx/dy/pageWidth/pageHeight). Surface
            // an actionable error rather than a bare FileNotFoundException.
            if (!File.Exists(outputFile))
            {
                var stdout = await process.StandardOutput.ReadToEndAsync(ct);
                var stderr = await process.StandardError.ReadToEndAsync(ct);
                throw new InvalidOperationException(
                    $"Draw.io exited 0 but produced no output at '{outputFile}'. " +
                    "This usually means the input mxfile is missing required page-level attributes. " +
                    $"stdout: {stdout.Trim()}; stderr: {stderr.Trim()}");
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

    private static Process StartDrawioProcess(ProcessStartInfo psi)
    {
        try
        {
            return Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start Draw.io process.");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // Process.Start throws Win32Exception when the binary is missing or
            // unreachable (e.g. DRAWIO_CMD points at a path that does not exist
            // on this machine, or PATH is empty). Translate to the renderer's
            // documented "Draw.io CLI not found" contract so callers get the
            // same error shape regardless of OS-level failure mode.
            throw new InvalidOperationException(
                $"Draw.io CLI not found at '{psi.FileName}'. Install 'drawio-desktop' or set DRAWIO_CMD to a valid binary.",
                ex);
        }
    }

    private static (string FileName, string ArgsPrefix)? ResolveDrawioCommand()
    {
        // Honor DRAWIO_CMD first. Accepts either a bare binary name
        // ("drawio") or a multi-token invocation
        // ("drawio --no-sandbox --disable-gpu"), parsed via the shared resolver.
        var envCmd = Environment.GetEnvironmentVariable("DRAWIO_CMD");
        if (!string.IsNullOrWhiteSpace(envCmd))
        {
            return RendererCommandResolver.Parse(envCmd);
        }

        // Fall back to common installation paths (single-binary, no extra args).
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
                    return (candidate, string.Empty);
                }
            }
            catch
            {
                // Not found, try next.
            }
        }

        return null;
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }
}
