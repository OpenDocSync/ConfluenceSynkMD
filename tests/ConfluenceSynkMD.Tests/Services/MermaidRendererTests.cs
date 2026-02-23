using ConfluenceSynkMD.Services;
using FluentAssertions;
using NSubstitute;
using Serilog;

namespace ConfluenceSynkMD.Tests.Services;

public sealed class MermaidRendererTests
{
    private static readonly Lock _envLock = new();

    [Fact]
    public void GenerateFileName_IsDeterministic()
    {
        const string source = "graph TD\n  A --> B";

        var first = MermaidRenderer.GenerateFileName(source);
        var second = MermaidRenderer.GenerateFileName(source);

        first.Should().Be(second);
        first.Should().MatchRegex("^mermaid-[a-f0-9]{8}\\.png$");
    }

    [Fact]
    public void GenerateFileName_DiffersForDifferentSource()
    {
        var first = MermaidRenderer.GenerateFileName("graph TD\n  A --> B");
        var second = MermaidRenderer.GenerateFileName("graph TD\n  A --> C");

        first.Should().NotBe(second);
    }

    [Fact]
    public async Task RenderToPngAsync_WithoutDockerOnPath_ThrowsClearErrorAndCleansTempFiles()
    {
        const string source = "graph TD\n  A --> B";
        var logger = Substitute.For<ILogger>();
        logger.ForContext<MermaidRenderer>().Returns(logger);
        var sut = new MermaidRenderer(logger);

        var outputFileName = MermaidRenderer.GenerateFileName(source);
        var hash = outputFileName["mermaid-".Length..^".png".Length];
        var tempDir = Path.Combine(Path.GetTempPath(), "ConfluenceSynkMD-mermaid");
        var inputFile = Path.Combine(tempDir, $"{hash}.mmd");
        var outputFile = Path.Combine(tempDir, $"{hash}.png");

        if (File.Exists(inputFile)) File.Delete(inputFile);
        if (File.Exists(outputFile)) File.Delete(outputFile);

        InvalidOperationException? exception;

        lock (_envLock)
        {
            var originalPath = Environment.GetEnvironmentVariable("PATH");
            try
            {
                Environment.SetEnvironmentVariable("PATH", string.Empty);
                exception = Assert.ThrowsAsync<InvalidOperationException>(() => sut.RenderToPngAsync(source))
                    .GetAwaiter()
                    .GetResult();
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", originalPath);
            }
        }

        exception.Should().NotBeNull();
        exception!.Message.Should().Contain("'docker' executable is not available on PATH");

        File.Exists(inputFile).Should().BeFalse();
        File.Exists(outputFile).Should().BeFalse();
    }

    [Fact]
    public async Task RenderToPngAsync_WithLocalMmdcOnPath_RendersWithoutDocker()
    {
        const string source = "graph TD\n  A --> B";
        var logger = Substitute.For<ILogger>();
        logger.ForContext<MermaidRenderer>().Returns(logger);
        var sut = new MermaidRenderer(logger);

        var outputFileName = MermaidRenderer.GenerateFileName(source);
        var hash = outputFileName["mermaid-".Length..^".png".Length];
        var tempDir = Path.Combine(Path.GetTempPath(), "ConfluenceSynkMD-mermaid");
        var inputFile = Path.Combine(tempDir, $"{hash}.mmd");
        var outputFile = Path.Combine(tempDir, $"{hash}.png");

        if (File.Exists(inputFile)) File.Delete(inputFile);
        if (File.Exists(outputFile)) File.Delete(outputFile);

        lock (_envLock)
        {
            var originalPath = Environment.GetEnvironmentVariable("PATH");
            var fakeToolDir = Path.Combine(Path.GetTempPath(), "ConfluenceSynkMD-fake-mmdc");
            Directory.CreateDirectory(fakeToolDir);

            var fakeExecutablePath = OperatingSystem.IsWindows()
                ? Path.Combine(fakeToolDir, "mmdc.cmd")
                : Path.Combine(fakeToolDir, "mmdc");

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    File.WriteAllText(fakeExecutablePath,
                        "@echo off\r\npowershell -NoProfile -Command \"$b=[byte[]](137,80,78,71,13,10,26,10); [Console]::OpenStandardOutput().Write($b,0,$b.Length)\"\r\nexit /b 0\r\n");
                }
                else
                {
                    File.WriteAllText(fakeExecutablePath,
                        "#!/bin/sh\nprintf '\\211PNG\\r\\n\\032\\n'\n");
                    File.SetUnixFileMode(
                        fakeExecutablePath,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                        UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                }

                Environment.SetEnvironmentVariable(
                    "PATH",
                    string.IsNullOrEmpty(originalPath)
                        ? fakeToolDir
                        : $"{fakeToolDir}{Path.PathSeparator}{originalPath}");

                var result = sut.RenderToPngAsync(source).GetAwaiter().GetResult();

                result.FileName.Should().Be(outputFileName);
                result.PngBytes.Length.Should().BeGreaterThanOrEqualTo(8);
                result.PngBytes.Take(4).Should().Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

                File.Exists(inputFile).Should().BeFalse();
                File.Exists(outputFile).Should().BeFalse();
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", originalPath);
                if (File.Exists(fakeExecutablePath)) File.Delete(fakeExecutablePath);
                if (Directory.Exists(fakeToolDir)) Directory.Delete(fakeToolDir, true);
            }
        }
    }

    [Fact]
    public async Task RenderToPngAsync_WithPuppeteerConfigEnabledAndDockerMissing_CleansPuppeteerConfigTempFile()
    {
        const string source = "graph TD\n  A --> B";
        var logger = Substitute.For<ILogger>();
        logger.ForContext<MermaidRenderer>().Returns(logger);
        var sut = new MermaidRenderer(logger);

        var outputFileName = MermaidRenderer.GenerateFileName(source);
        var hash = outputFileName["mermaid-".Length..^".png".Length];
        var tempDir = Path.Combine(Path.GetTempPath(), "ConfluenceSynkMD-mermaid");
        var inputFile = Path.Combine(tempDir, $"{hash}.mmd");
        var outputFile = Path.Combine(tempDir, $"{hash}.png");
        var puppeteerConfig = Path.Combine(tempDir, "puppeteer-config.json");

        if (File.Exists(inputFile)) File.Delete(inputFile);
        if (File.Exists(outputFile)) File.Delete(outputFile);
        if (File.Exists(puppeteerConfig)) File.Delete(puppeteerConfig);

        InvalidOperationException? exception;

        lock (_envLock)
        {
            var originalPath = Environment.GetEnvironmentVariable("PATH");
            var originalRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            var originalUsePuppeteerConfig = Environment.GetEnvironmentVariable("MERMAID_USE_PUPPETEER_CONFIG");
            var originalDockerVolume = Environment.GetEnvironmentVariable("MERMAID_DOCKER_VOLUME");

            try
            {
                Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");
                Environment.SetEnvironmentVariable("MERMAID_USE_PUPPETEER_CONFIG", "true");
                Environment.SetEnvironmentVariable("MERMAID_DOCKER_VOLUME", tempDir);
                Environment.SetEnvironmentVariable("PATH", string.Empty);

                exception = Assert.ThrowsAsync<InvalidOperationException>(() => sut.RenderToPngAsync(source))
                    .GetAwaiter()
                    .GetResult();
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", originalPath);
                Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", originalRunningInContainer);
                Environment.SetEnvironmentVariable("MERMAID_USE_PUPPETEER_CONFIG", originalUsePuppeteerConfig);
                Environment.SetEnvironmentVariable("MERMAID_DOCKER_VOLUME", originalDockerVolume);
            }
        }

        exception.Should().NotBeNull();
        exception!.Message.Should().Contain("'docker' executable is not available on PATH");

        File.Exists(inputFile).Should().BeFalse();
        File.Exists(outputFile).Should().BeFalse();
        File.Exists(puppeteerConfig).Should().BeFalse();
    }
}
