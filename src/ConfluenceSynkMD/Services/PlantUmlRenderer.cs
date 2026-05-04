using System.Diagnostics;
using Serilog;

namespace ConfluenceSynkMD.Services;

/// <summary>
/// Renders PlantUML diagrams to PNG or SVG images.
/// Requires PlantUML JAR or CLI to be available.
/// Configurable via PLANTUML_JAR or PLANTUML_CMD environment variables.
/// </summary>
public sealed class PlantUmlRenderer : IDiagramRenderer
{
    private readonly ILogger _logger;

    public PlantUmlRenderer(ILogger logger)
    {
        _logger = logger.ForContext<PlantUmlRenderer>();
    }

    /// <summary>
    /// Renders PlantUML source to image bytes.
    /// </summary>
    /// <param name="plantUmlSource">The PlantUML diagram source code.</param>
    /// <param name="outputFormat">Output format: "png" or "svg".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple of image bytes and the format used.</returns>
    public async Task<(byte[] ImageBytes, string Format)> RenderAsync(
        string source, string outputFormat = "png", CancellationToken ct = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ConfluenceSynkMD-plantuml");
        Directory.CreateDirectory(tempDir);

        var inputFile = Path.Combine(tempDir, $"diagram-{Guid.NewGuid():N}.puml");
        var outputFile = Path.ChangeExtension(inputFile, outputFormat);

        try
        {
            await File.WriteAllTextAsync(inputFile, source, ct);

            var (command, arguments) = BuildCommand(inputFile, outputFormat);

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
                ?? throw new InvalidOperationException("Failed to start PlantUML process.");

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(ct);
                throw new InvalidOperationException($"PlantUML failed (exit {process.ExitCode}): {stderr}");
            }

            var imageBytes = await File.ReadAllBytesAsync(outputFile, ct);
            _logger.Debug("PlantUML diagram rendered: {Size} bytes ({Format}).", imageBytes.Length, outputFormat);
            return (imageBytes, outputFormat);
        }
        finally
        {
            TryDeleteFile(inputFile);
            TryDeleteFile(outputFile);
        }
    }

    private static (string Command, string Arguments) BuildCommand(string inputFile, string outputFormat)
    {
        var formatArg = outputFormat.Equals("svg", StringComparison.OrdinalIgnoreCase) ? "-tsvg" : "-tpng";
        var perCallArgs = $"{formatArg} \"{inputFile}\"";

        // PLANTUML_CMD: bare binary name OR multi-token invocation
        // (e.g. "java -jar /opt/plantuml.jar"). Parsed via the shared resolver.
        var cmd = Environment.GetEnvironmentVariable("PLANTUML_CMD");
        if (!string.IsNullOrWhiteSpace(cmd))
        {
            var (fileName, argsPrefix) = RendererCommandResolver.Parse(cmd);
            return (fileName, RendererCommandResolver.CombineArgs(argsPrefix, perCallArgs));
        }

        // PLANTUML_JAR: convenience for the common "java -jar ..." pattern.
        var jar = Environment.GetEnvironmentVariable("PLANTUML_JAR");
        if (!string.IsNullOrEmpty(jar) && File.Exists(jar))
        {
            return ("java", $"-jar \"{jar}\" {perCallArgs}");
        }

        // Default: try plantuml as a single-token command on PATH.
        return ("plantuml", perCallArgs);
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }
}
