using System.Diagnostics;
using Serilog;

namespace ConfluentSynkMD.Services;

/// <summary>
/// Renders LaTeX formulas to PNG images.
/// Can use either a LaTeX distribution (pdflatex + convert) or the
/// Confluence math-inline/math-display macro as fallback.
/// </summary>
public sealed class LatexRenderer : ILatexRenderer
{
    private readonly ILogger _logger;

    public LatexRenderer(ILogger logger)
    {
        _logger = logger.ForContext<LatexRenderer>();
    }

    /// <summary>
    /// Renders LaTeX source to a PNG image.
    /// </summary>
    /// <param name="latexSource">The LaTeX formula or document fragment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple of image bytes and the format used.</returns>
    public async Task<(byte[] ImageBytes, string Format)> RenderAsync(
        string latexSource, CancellationToken ct = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ConfluentSynkMD-latex");
        Directory.CreateDirectory(tempDir);

        var texFile = Path.Combine(tempDir, $"formula-{Guid.NewGuid():N}.tex");
        var pdfFile = Path.ChangeExtension(texFile, ".pdf");
        var pngFile = Path.ChangeExtension(texFile, ".png");

        try
        {
            // Wrap formula in minimal LaTeX document
            var texContent = WrapInDocument(latexSource);
            await File.WriteAllTextAsync(texFile, texContent, ct);

            // Step 1: Compile LaTeX to PDF
            await RunProcessAsync("pdflatex",
                $"-interaction=nonstopmode -output-directory=\"{tempDir}\" \"{texFile}\"", ct);

            // Step 2: Convert PDF to PNG using ImageMagick or Ghostscript
            if (File.Exists(pdfFile))
            {
                await RunProcessAsync("convert",
                    $"-density 300 \"{pdfFile}\" -trim -quality 100 \"{pngFile}\"", ct);
            }

            if (!File.Exists(pngFile))
            {
                throw new InvalidOperationException(
                    "LaTeX rendering failed. Ensure pdflatex and ImageMagick are installed.");
            }

            var imageBytes = await File.ReadAllBytesAsync(pngFile, ct);
            _logger.Debug("LaTeX formula rendered: {Size} bytes.", imageBytes.Length);
            return (imageBytes, "png");
        }
        finally
        {
            TryDeleteFile(texFile);
            TryDeleteFile(pdfFile);
            TryDeleteFile(pngFile);
            TryDeleteFile(Path.ChangeExtension(texFile, ".aux"));
            TryDeleteFile(Path.ChangeExtension(texFile, ".log"));
        }
    }

    /// <summary>
    /// Generates a Confluence math macro (inline or block) as an alternative to image rendering.
    /// Use this when LaTeX tools are not available.
    /// </summary>
    /// <param name="latexSource">The LaTeX formula.</param>
    /// <param name="isBlock">Whether this is a block (display) formula.</param>
    /// <returns>Confluence Storage Format XHTML for the math macro.</returns>
    public static string GenerateMathMacro(string latexSource, bool isBlock = true)
    {
        var macroName = isBlock ? "mathblock" : "mathinline";
        var escaped = latexSource
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        return $"<ac:structured-macro ac:name=\"{macroName}\">" +
               $"<ac:plain-text-body><![CDATA[{escaped}]]></ac:plain-text-body>" +
               "</ac:structured-macro>";
    }

    private static string WrapInDocument(string formula)
    {
        return @"\documentclass[border=2pt]{standalone}
\usepackage{amsmath,amssymb,amsfonts}
\begin{document}
$" + formula + @"$
\end{document}";
    }

    private static async Task RunProcessAsync(string command, string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start {command} process.");

        await process.WaitForExitAsync(ct);
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }
}
