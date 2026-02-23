using System.Diagnostics;
using System.IO;
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

        var mmdcCommand = ResolveMmdcCommand();
        if (mmdcCommand is not null)
        {
            var pngBytes = await RenderWithMmdcAsync(mmdcCommand, mermaidSource, ct);
            _logger.Information("Rendered Mermaid diagram '{FileName}' ({Size} bytes) via local mmdc.",
                fileName, pngBytes.Length);
            return (pngBytes, fileName);
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "ConfluenceSynkMD-mermaid");
        Directory.CreateDirectory(tempDir);
        EnsureWritableForSiblingContainer(tempDir);

        var inputFile = Path.Combine(tempDir, $"{hash}.mmd");
        var outputFile = Path.Combine(tempDir, $"{hash}.png");
        var cleanupPaths = new List<string> { inputFile, outputFile };

        try
        {
            // Write Mermaid source to temp file
            await File.WriteAllTextAsync(inputFile, mermaidSource, ct);
            EnsureWritableForSiblingContainer(inputFile);

            // Determine how to invoke Mermaid CLI via Docker.
            var (command, args, dockerImage, additionalCleanupPaths) = await ResolveDockerMermaidCliAsync(tempDir, hash, ct);
            cleanupPaths.AddRange(additionalCleanupPaths);
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
            foreach (var cleanupPath in cleanupPaths)
            {
                TryDelete(cleanupPath);
            }
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

    private async Task<byte[]> RenderWithMmdcAsync(
        string mmdcCommand,
        string mermaidSource,
        CancellationToken ct)
    {
        var args = new[]
        {
            "--input",
            "-",
            "--output",
            "-",
            "--outputFormat",
            "png",
            "--backgroundColor",
            "transparent",
            "--scale",
            "2",
        };

        var psi = new ProcessStartInfo
        {
            FileName = mmdcCommand,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        var argsForLog = string.Join(" ", args.Select(EscapeArgForLog));
        _logger.Debug("Rendering Mermaid diagram via local mmdc: {Cmd} {Args}", mmdcCommand, argsForLog);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException(
                "Failed to start mermaid-cli process (mmdc). Ensure mermaid-cli is installed and available on PATH.");

        await process.StandardInput.WriteAsync(mermaidSource.AsMemory(), ct);
        process.StandardInput.Close();

        using var outputBuffer = new MemoryStream();
        var copyOutputTask = process.StandardOutput.BaseStream.CopyToAsync(outputBuffer, ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await Task.WhenAll(copyOutputTask, stderrTask);
        await process.WaitForExitAsync(ct);

        var stderr = stderrTask.Result;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Mermaid CLI rendering via mmdc failed (exit {process.ExitCode}): {stderr.Trim()}");
        }

        if (outputBuffer.Length == 0)
        {
            throw new InvalidOperationException(
                "Mermaid CLI rendering via mmdc produced no output.");
        }

        return outputBuffer.ToArray();
    }

    private static string? ResolveMmdcCommand()
    {
        var configuredCommand = Environment.GetEnvironmentVariable("MERMAID_MMDC_COMMAND");
        if (!string.IsNullOrWhiteSpace(configuredCommand))
        {
            var normalizedCommand = configuredCommand.Trim();
            if (!IsExecutableAvailable(normalizedCommand))
            {
                throw new InvalidOperationException(
                    $"MERMAID_MMDC_COMMAND is set but not executable: '{normalizedCommand}'.");
            }

            return normalizedCommand;
        }

        var defaultCommand = OperatingSystem.IsWindows() ? "mmdc.cmd" : "mmdc";
        return IsCommandAvailable(defaultCommand) ? defaultCommand : null;
    }

    /// <summary>
    /// Resolves the correct way to invoke Mermaid CLI via Docker depending on the environment.
    /// Uses Docker to run the official Mermaid CLI image.
    /// </summary>
    private static async Task<(string Command, string[] Args, string DockerImage, string[] AdditionalCleanupPaths)> ResolveDockerMermaidCliAsync(
        string tempDir, string hash, CancellationToken ct)
    {
        var runningInContainer = IsRunningInContainer();
        var additionalCleanupPaths = new List<string>();

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

        if (!IsCommandAvailable("docker"))
        {
            throw new InvalidOperationException(
                "The 'docker' executable is not available on PATH. This tool now uses the official Mermaid CLI Docker image to render Mermaid diagrams.",
                innerException: null);
        }

        var tempDirName = Path.GetFileName(tempDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var sharedSubPath = runningInContainer && !string.IsNullOrWhiteSpace(tempDirName)
            ? $"{tempDirName}/"
            : string.Empty;

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
            $"/data/{sharedSubPath}{hash}.mmd",
            "-o",
            $"/data/{sharedSubPath}{hash}.png",
            "-b",
            "transparent",
            "--scale",
            "2",
        };

        // Optional puppeteer config injection for environments that explicitly require it.
        // Enabled via MERMAID_USE_PUPPETEER_CONFIG=true.
        var usePuppeteerConfig = string.Equals(
            Environment.GetEnvironmentVariable("MERMAID_USE_PUPPETEER_CONFIG"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (runningInContainer && usePuppeteerConfig)
        {
            var puppeteerConfig = Path.Combine(tempDir, "puppeteer-config.json");
            await File.WriteAllTextAsync(
                puppeteerConfig,
                _puppeteerConfigJson,
                ct);
            EnsureWritableForSiblingContainer(puppeteerConfig);
            additionalCleanupPaths.Add(puppeteerConfig);
            dockerArgs.Add("-p");
            dockerArgs.Add($"/data/{sharedSubPath}puppeteer-config.json");
        }

        return ("docker", dockerArgs.ToArray(), dockerImage, additionalCleanupPaths.ToArray());
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

    private static bool IsExecutableAvailable(string command)
    {
        var normalizedCommand = command.Trim().Trim('"');
        if (normalizedCommand.Contains(Path.DirectorySeparatorChar) ||
            normalizedCommand.Contains(Path.AltDirectorySeparatorChar) ||
            Path.IsPathRooted(normalizedCommand))
        {
            return File.Exists(normalizedCommand);
        }

        return IsCommandAvailable(normalizedCommand);
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

    private static void EnsureWritableForSiblingContainer(string path)
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        try
        {
            if (Directory.Exists(path))
            {
                File.SetUnixFileMode(
                    path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute);
                return;
            }

            if (File.Exists(path))
            {
                File.SetUnixFileMode(
                    path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite |
                    UnixFileMode.GroupRead | UnixFileMode.GroupWrite |
                    UnixFileMode.OtherRead | UnixFileMode.OtherWrite);
            }
        }
        catch
        {
            // best effort; keep rendering flow resilient
        }
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
