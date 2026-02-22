using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace ConfluentSynkMD.Services;

/// <summary>
/// Renders Mermaid diagram source code to PNG images using
/// @mermaid-js/mermaid-cli (mmdc).
///
/// In Docker: mmdc is globally installed, called directly.
/// Locally: falls back to npx.cmd / npx for on-demand install.
/// </summary>
public sealed class MermaidRenderer : IMermaidRenderer
{
    private readonly ILogger _logger;

    public MermaidRenderer(ILogger logger)
    {
        _logger = logger.ForContext<MermaidRenderer>();
    }

    /// <summary>
    /// Renders Mermaid source to a PNG byte array.
    /// Returns the PNG bytes and a deterministic filename based on content hash.
    /// </summary>
    public async Task<(byte[] PngBytes, string FileName)> RenderToPngAsync(
        string mermaidSource, CancellationToken ct = default)
    {
        var hash = ComputeShortHash(mermaidSource);
        var fileName = $"mermaid-{hash}.png";

        var tempDir = Path.Combine(Path.GetTempPath(), "ConfluentSynkMD-mermaid");
        Directory.CreateDirectory(tempDir);

        var inputFile = Path.Combine(tempDir, $"{hash}.mmd");
        var outputFile = Path.Combine(tempDir, $"{hash}.png");

        try
        {
            // Write Mermaid source to temp file
            await File.WriteAllTextAsync(inputFile, mermaidSource, ct);

            // Create puppeteer config (no-sandbox required in Docker)
            var puppeteerConfig = Path.Combine(tempDir, "puppeteer-config.json");
            if (!File.Exists(puppeteerConfig))
            {
                await File.WriteAllTextAsync(puppeteerConfig,
                    """{"args": ["--no-sandbox", "--disable-setuid-sandbox"]}""", ct);
            }

            // Determine how to invoke mmdc:
            // 1. Docker/global install: mmdc is on PATH
            // 2. Local Windows: use npx.cmd -p @mermaid-js/mermaid-cli mmdc
            // 3. Local Unix: use npx -p @mermaid-js/mermaid-cli mmdc
            var (command, args) = ResolveMmdc(inputFile, outputFile, puppeteerConfig);

            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            _logger.Debug("Rendering Mermaid diagram '{FileName}': {Cmd} {Args}",
                fileName, command, args);

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException(
                    "Failed to start mmdc process. Is Node.js installed (or are you running in Docker)?");

            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                _logger.Error(
                    "mmdc failed (exit {Code}):\ncommand: {Cmd} {Args}\nstdout: {Out}\nstderr: {Err}",
                    process.ExitCode, command, args, stdout.Trim(), stderr.Trim());
                throw new InvalidOperationException(
                    $"mmdc rendering failed (exit {process.ExitCode}): {stderr.Trim()}");
            }

            if (!File.Exists(outputFile))
            {
                throw new FileNotFoundException(
                    $"mmdc did not produce output file: {outputFile}");
            }

            var pngBytes = await File.ReadAllBytesAsync(outputFile, ct);
            _logger.Information("Rendered Mermaid diagram '{FileName}' ({Size} bytes).",
                fileName, pngBytes.Length);

            return (pngBytes, fileName);
        }
        finally
        {
            TryDelete(inputFile);
            TryDelete(outputFile);
        }
    }

    /// <summary>
    /// Generates a deterministic filename for a Mermaid diagram based on its content.
    /// </summary>
    public static string GenerateFileName(string mermaidSource)
    {
        var hash = ComputeShortHash(mermaidSource);
        return $"mermaid-{hash}.png";
    }

    /// <summary>
    /// Resolves the correct way to invoke mmdc depending on the environment.
    /// </summary>
    private static (string Command, string Args) ResolveMmdc(
        string inputFile, string outputFile, string puppeteerConfig)
    {
        var mmdcArgs = $"-i \"{inputFile}\" -o \"{outputFile}\" -b transparent --scale 2 -p \"{puppeteerConfig}\"";

        // Check if mmdc is globally available (Docker / global npm install)
        if (IsCommandAvailable("mmdc"))
        {
            return ("mmdc", mmdcArgs);
        }

        // Fallback: use npx to install on-demand
        if (OperatingSystem.IsWindows())
        {
            return ("npx.cmd", $"-y -p @mermaid-js/mermaid-cli mmdc {mmdcArgs}");
        }

        return ("npx", $"-y -p @mermaid-js/mermaid-cli mmdc {mmdcArgs}");
    }

    private static bool IsCommandAvailable(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "where.exe" : "which",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(3000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string ComputeShortHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort cleanup */ }
    }
}
