using ConfluenceSynkMD.Commands;
using ConfluenceSynkMD.Configuration;
using ConfluenceSynkMD.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Serilog;

namespace ConfluenceSynkMD.Tests.Commands;

/// <summary>
/// Unit tests for <see cref="InitCommand"/>. Cover the four control branches
/// (env-vars-complete-skip-prompts, non-interactive-missing-fail,
/// stdin-redirected-fail, interactive-prompt-and-write) plus the
/// validation-fails-no-write path.
/// </summary>
public class InitCommandTests
{
    [Fact]
    public async Task Skips_prompts_when_all_basic_env_vars_already_set_and_writes_config_on_healthy()
    {
        var fs = new InMemoryFs();
        var (init, _) = NewInit(
            fs,
            settings: NewSettings(),
            stdin: new StringReader(""), // no input expected
            stdinIsRedirected: false,
            health: HealthCheckStatus.Healthy);

        var exit = await init.RunAsync(nonInteractive: false, configOut: null);

        exit.Should().Be(0);
        fs.Files.Should().ContainKey(".confluencesynkmd.json");
        fs.Files[".confluencesynkmd.json"].Should().Contain("\"BaseUrl\": \"https://example.atlassian.net\"");
        fs.Files[".confluencesynkmd.json"].Should().Contain("\"AuthMode\": \"Basic\"");
    }

    [Fact]
    public async Task Refuses_to_prompt_when_non_interactive_and_required_vars_missing()
    {
        var fs = new InMemoryFs();
        var (init, captured) = NewInit(
            fs,
            settings: NewSettings(apiToken: string.Empty),
            stdin: new StringReader("any-value\n"),
            stdinIsRedirected: false,
            health: HealthCheckStatus.Healthy);

        var exit = await init.RunAsync(nonInteractive: true, configOut: null);

        exit.Should().Be(1);
        fs.Files.Should().BeEmpty();
        captured.Stderr.ToString().Should().Contain("CONFLUENCE__APITOKEN");
    }

    [Fact]
    public async Task Refuses_to_prompt_when_stdin_is_redirected_and_required_vars_missing()
    {
        var fs = new InMemoryFs();
        var (init, captured) = NewInit(
            fs,
            settings: NewSettings(apiToken: string.Empty),
            stdin: new StringReader(""),
            stdinIsRedirected: true,
            health: HealthCheckStatus.Healthy);

        var exit = await init.RunAsync(nonInteractive: false, configOut: null);

        exit.Should().Be(1);
        fs.Files.Should().BeEmpty();
        captured.Stderr.ToString().Should().Contain("CONFLUENCE__APITOKEN");
        captured.Stderr.ToString().Should().Contain("interactively");
    }

    [Fact]
    public async Task Prompts_for_missing_values_and_writes_config_after_validation()
    {
        var fs = new InMemoryFs();
        // Provide token via prompt; everything else is already in env.
        var input = "supplied-token\n";
        var (init, _) = NewInit(
            fs,
            settings: NewSettings(apiToken: string.Empty),
            stdin: new StringReader(input),
            stdinIsRedirected: false,
            health: HealthCheckStatus.Healthy);

        var exit = await init.RunAsync(nonInteractive: false, configOut: null);

        exit.Should().Be(0);
        fs.Files[".confluencesynkmd.json"].Should().Contain("supplied-token");
    }

    [Fact]
    public async Task Re_prompts_when_user_enters_blank_for_required_field()
    {
        var fs = new InMemoryFs();
        // First answer is blank, second answer is a real token.
        var input = "\nreal-token\n";
        var (init, captured) = NewInit(
            fs,
            settings: NewSettings(apiToken: string.Empty),
            stdin: new StringReader(input),
            stdinIsRedirected: false,
            health: HealthCheckStatus.Healthy);

        var exit = await init.RunAsync(nonInteractive: false, configOut: null);

        exit.Should().Be(0);
        captured.Stdout.ToString().Should().Contain("Value required.");
        fs.Files[".confluencesynkmd.json"].Should().Contain("real-token");
    }

    [Fact]
    public async Task Exits_1_when_validation_fails_and_does_not_write_config()
    {
        var fs = new InMemoryFs();
        var (init, captured) = NewInit(
            fs,
            settings: NewSettings(),
            stdin: new StringReader(""),
            stdinIsRedirected: false,
            health: HealthCheckStatus.AuthError,
            healthMessage: "Auth rejected (401).");

        var exit = await init.RunAsync(nonInteractive: false, configOut: null);

        exit.Should().Be(1);
        fs.Files.Should().BeEmpty();
        captured.Stderr.ToString().Should().Contain("Validation failed");
        captured.Stderr.ToString().Should().Contain("401");
    }

    [Fact]
    public async Task Honors_config_out_path()
    {
        var fs = new InMemoryFs();
        var (init, _) = NewInit(
            fs,
            settings: NewSettings(),
            stdin: new StringReader(""),
            stdinIsRedirected: false,
            health: HealthCheckStatus.Healthy);

        var exit = await init.RunAsync(nonInteractive: false, configOut: "/tmp/custom.json");

        exit.Should().Be(0);
        fs.Files.Should().ContainKey("/tmp/custom.json");
        fs.Files.Should().NotContainKey(".confluencesynkmd.json");
    }

    [Fact]
    public async Task Bearer_mode_only_requires_BaseUrl_and_BearerToken()
    {
        var fs = new InMemoryFs();
        var (init, _) = NewInit(
            fs,
            settings: NewSettings(
                authMode: "Bearer",
                userEmail: string.Empty,
                apiToken: string.Empty,
                bearerToken: "bearer-xyz"),
            stdin: new StringReader(""),
            stdinIsRedirected: false,
            health: HealthCheckStatus.Healthy);

        var exit = await init.RunAsync(nonInteractive: false, configOut: null);

        exit.Should().Be(0);
        fs.Files[".confluencesynkmd.json"].Should().Contain("bearer-xyz");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IOptions<ConfluenceSettings> NewSettings(
        string baseUrl = "https://example.atlassian.net",
        string userEmail = "user@example.com",
        string apiToken = "token-xyz",
        string apiPath = "/wiki",
        string apiVersion = "v2",
        string authMode = "Basic",
        string? bearerToken = null)
    {
        return Options.Create(new ConfluenceSettings
        {
            BaseUrl = baseUrl,
            UserEmail = userEmail,
            ApiToken = apiToken,
            ApiPath = apiPath,
            ApiVersion = apiVersion,
            AuthMode = authMode,
            BearerToken = bearerToken ?? string.Empty,
        });
    }

    private static (InitCommand init, CapturedStreams captured) NewInit(
        InMemoryFs fs,
        IOptions<ConfluenceSettings> settings,
        TextReader stdin,
        bool stdinIsRedirected,
        HealthCheckStatus health,
        string healthMessage = "OK")
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var probe = Substitute.For<IConfluenceHealthCheck>();
        probe.CheckAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new HealthCheckResult
        {
            Status = health,
            Message = healthMessage,
            Detail = "stub probe response",
        }));
        var logger = Substitute.For<ILogger>();
        logger.ForContext<InitCommand>().Returns(logger);

        var init = new InitCommand(
            settings,
            probe,
            stdin,
            stdout,
            stderr,
            stdinIsRedirected,
            fs.WriteAsync,
            logger);
        return (init, new CapturedStreams(stdout, stderr));
    }

    private sealed class InMemoryFs
    {
        public Dictionary<string, string> Files { get; } = new();

        public Task WriteAsync(string path, string contents)
        {
            Files[path] = contents;
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedStreams(StringWriter Stdout, StringWriter Stderr);
}
