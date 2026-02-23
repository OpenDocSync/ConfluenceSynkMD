using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace ConfluenceSynkMD.Services;

/// <summary>
/// Renders Mermaid diagram source code to PNG images using
/// the official Mermaid CLI Docker container (ghcr.io/mermaid-js/mermaid-cli/mermaid-cli).
///
/// Requires the Docker engine to be accessible (either locally or via mapped /var/run/docker.sock).
/// </summary>
public sealed class MermaidRenderer : IMermaidRenderer
{
    private const string _puppeteerConfigJson = """{"args": ["--no-sandbox", "--disable-setuid-sandbox"]}""";
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

        var tempDir = Path.Combine(Path.GetTempPath(), "ConfluenceSynkMD-mermaid");
        Directory.CreateDirectory(tempDir);

        var inputFile = Path.Combine(tempDir, $"{hash}.mmd");
        var outputFile = Path.Combine(tempDir, $"{hash}.png");

        try
        {
            // Write Mermaid source to temp file
            await File.WriteAllTextAsync(inputFile, mermaidSource, ct);

            // Determine how to invoke Mermaid CLI via Docker.
            var (command, args, dockerImage) = await ResolveDockerMermaidCliAsync(tempDir, hash, ct);
            var argsForLog = string.Join(" ", args.Select(EscapeArgForLog));
            _logger.Debug("Using Mermaid Docker image: {DockerImage}", dockerImage);

            var psi = new ProcessStartInfo
            {
                FileName = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }

            _logger.Debug("Rendering Mermaid diagram '{FileName}': {Cmd} {Args}",
                fileName, command, argsForLog);

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException(
                    "Failed to start docker process. Ensure docker is installed and available on PATH or the docker socket is mounted.");

            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                _logger.Error(
                    "Mermaid CLI docker run failed (exit {Code}):\ncommand: {Cmd} {Args}\nstdout: {Out}\nstderr: {Err}",
                    process.ExitCode, command, argsForLog, stdout.Trim(), stderr.Trim());

                var dockerHint = BuildDockerFailureHint(stderr);
                throw new InvalidOperationException(
                    $"Mermaid CLI rendering failed (exit {process.ExitCode}): {stderr.Trim()}{dockerHint}");
            }

            if (!File.Exists(outputFile))
            {
                throw new FileNotFoundException(
                    $"Docker mermaid-cli did not produce output file: {outputFile}. Check volume mounts.");
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
    /// Resolves the correct way to invoke Mermaid CLI via Docker depending on the environment.
    /// Uses Docker to run the official Mermaid CLI image.
    /// </summary>
    private static async Task<(string Command, string[] Args, string DockerImage)> ResolveDockerMermaidCliAsync(
        string tempDir, string hash, CancellationToken ct)
    {
        var runningInContainer = IsRunningInContainer();

        // In a Docker-in-Docker scenario via docker-compose, the container's /tmp path
        // might not be the same as the host's volume path.
        // We use an environment variable to allow mapping a named volume or host path.
        var dockerVolume = Environment.GetEnvironmentVariable("MERMAID_DOCKER_VOLUME");
        if (string.IsNullOrWhiteSpace(dockerVolume))
        {
            if (runningInContainer)
            {
                throw new InvalidOperationException(
                    "MERMAID_DOCKER_VOLUME must be set when running inside a container. " +
                    "Set MERMAID_DOCKER_VOLUME to the host-visible shared temp directory that is mounted into this container, and set TMPDIR to the corresponding in-container mount path so both refer to the same physical directory. " +
                    "For example: MERMAID_DOCKER_VOLUME=/host/path/temp and TMPDIR=/app/mermaid_temp (where /host/path/temp is mounted into the container at /app/mermaid_temp). " +
                    "See the Docker section in README.md or docs/en/admin/docker.md for a working setup.",
                    innerException: null);
            }

            dockerVolume = tempDir;
        }

        var dockerImage = Environment.GetEnvironmentVariable("MERMAID_DOCKER_IMAGE") ?? "ghcr.io/mermaid-js/mermaid-cli/mermaid-cli";
        if (string.IsNullOrWhiteSpace(dockerImage) || dockerImage.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(
                "MERMAID_DOCKER_IMAGE is invalid. Provide a Docker image reference without whitespace.",
                innerException: null);
        }

        // Map the volume to /data in the mermaid container
        // -i /data/{hash}.mmd -o /data/{hash}.png
        var dockerArgs = new List<string>
        {
            "run",
            "--rm",
            "-v",
            $"{dockerVolume}:/data",
            dockerImage,
            "-i",
            $"/data/{hash}.mmd",
            "-o",
            $"/data/{hash}.png",
            "-b",
            "transparent",
            "--scale",
            "2",
        };

        // When running in container, we might also want to pass puppeteer config
        if (runningInContainer)
        {
            var puppeteerConfig = Path.Combine(tempDir, "puppeteer-config.json");
            await File.WriteAllTextAsync(
                puppeteerConfig,
                _puppeteerConfigJson,
                ct);
            dockerArgs.Add("-p");
            dockerArgs.Add("/data/puppeteer-config.json");
        }

        if (IsCommandAvailable("docker"))
        {
            return ("docker", dockerArgs.ToArray(), dockerImage);
        }

        throw new InvalidOperationException(
            "The 'docker' executable is not available on PATH. This tool now uses the official Mermaid CLI Docker image to render Mermaid diagrams.",
            innerException: null);
    }

    private static bool IsRunningInContainer()
    {
        var value = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
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

    private static string EscapeArgForLog(string arg)
    {
        if (arg is null)
        {
            return "\"\"";
        }

        if (arg.Length == 0 || arg.Contains('"') || arg.Contains(' '))
        {
            var escaped = arg.Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }

        return arg;
    }

    private static string BuildDockerFailureHint(string stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr))
        {
            return string.Empty;
        }

        var normalized = stderr.ToLowerInvariant();
        if (normalized.Contains("cannot connect to the docker daemon") ||
            normalized.Contains("is the docker daemon running") ||
            normalized.Contains("permission denied while trying to connect") ||
            normalized.Contains("got permission denied"))
        {
            return " Ensure the Docker daemon is running and this process can access /var/run/docker.sock. For non-root containers, pass --group-add <docker-gid> (see docs/en/admin/docker.md).";
        }

        return string.Empty;
    }
}
