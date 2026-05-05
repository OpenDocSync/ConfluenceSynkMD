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

            // Use ArgumentList instead of Arguments string. Each call adds one
            // argv token verbatim — no shell-style quote/whitespace parsing,
            // no risk of literal `"` characters leaking into the argv that
            // drawio receives. The previous string-form caused drawio's
            // commander parser to see `"/tmp/...drawio"` (with embedded quotes)
            // as the input path and fail with "Error: input file/directory not found".
            var psi = new ProcessStartInfo
            {
                FileName = resolved.Value.FileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            // Forward any tokens from DRAWIO_CMD's args prefix (e.g. when an
            // operator overrides the env var with a multi-token wrapper).
            if (!string.IsNullOrEmpty(resolved.Value.ArgsPrefix))
            {
                foreach (var token in SplitTokens(resolved.Value.ArgsPrefix))
                    psi.ArgumentList.Add(token);
            }

            psi.ArgumentList.Add("--export");
            psi.ArgumentList.Add("--format");
            psi.ArgumentList.Add(outputFormat);
            psi.ArgumentList.Add("--output");
            psi.ArgumentList.Add(outputFile);
            psi.ArgumentList.Add(inputFile);

            _logger.Debug(
                "Draw.io invocation: {FileName} {Args}",
                psi.FileName,
                string.Join(' ', psi.ArgumentList));

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

    private static IEnumerable<string> SplitTokens(string input)
    {
        // Reuse RendererCommandResolver's tokenizer indirectly by parsing the
        // string as a fake "cmd args" pair. The first token comes back as
        // FileName and the rest as ArgsPrefix; we only need the rest split
        // back into tokens, so we synthesize a pseudo-prefix and split it.
        if (string.IsNullOrWhiteSpace(input))
            return Array.Empty<string>();

        // Cheap shell-style split: respect double-quoted spans, drop the
        // grouping quotes. Same rules as RendererCommandResolver.Parse uses.
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuote = false;
        foreach (var c in input)
        {
            if (c == '"') { inQuote = !inQuote; continue; }
            if (char.IsWhiteSpace(c) && !inQuote)
            {
                if (current.Length > 0) { result.Add(current.ToString()); current.Clear(); }
                continue;
            }
            current.Append(c);
        }
        if (current.Length > 0) result.Add(current.ToString());
        return result;
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }
}
