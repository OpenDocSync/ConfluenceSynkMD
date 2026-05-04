using System.Text.Json;
using ConfluenceSynkMD.Configuration;
using ConfluenceSynkMD.Services;
using Microsoft.Extensions.Options;
using Serilog;

namespace ConfluenceSynkMD.Commands;

/// <summary>
/// `confluencesynkmd init [--non-interactive] [--config-out PATH]` —
/// first-run wizard that helps a user produce a working Confluence
/// configuration without editing env vars by hand.
///
/// Behavior:
///   1. Read existing CONFLUENCE__* env vars. If they cover the required
///      auth shape (Basic = email + token; Bearer = bearer token), skip
///      prompting and go straight to validation.
///   2. If anything is missing AND <c>--non-interactive</c> was passed,
///      exit 1 with a single line listing the missing env vars.
///   3. If anything is missing AND stdin is redirected (no TTY), exit 1
///      with the same message — refusing to prompt where prompting would
///      hang.
///   4. Otherwise prompt the user, in order, for: Base URL, Auth mode,
///      Email, API token (or Bearer token).
///   5. Run <see cref="IConfluenceHealthCheck"/> against the resolved
///      settings. If unhealthy, surface the message and exit 1 without
///      writing a file.
///   6. Write <c>.confluencesynkmd.json</c> with the same shape
///      <see cref="ConfluenceSettings"/> binds from. JSON, not YAML — so
///      <c>Microsoft.Extensions.Configuration.AddJsonFile</c> can read it
///      natively without an extra package.
///
/// Exit codes: 0 on healthy + file written; 1 on any failure (missing
/// config, refused-to-prompt, validation error, write error).
/// </summary>
public sealed class InitCommand
{
    private readonly IOptions<ConfluenceSettings> _settings;
    private readonly IConfluenceHealthCheck _healthCheck;
    private readonly TextReader _stdin;
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;
    private readonly bool _stdinIsRedirected;
    private readonly Func<string, string, Task> _writeFile;
    private readonly ILogger _logger;

    public InitCommand(
        IOptions<ConfluenceSettings> settings,
        IConfluenceHealthCheck healthCheck,
        TextReader stdin,
        TextWriter stdout,
        TextWriter stderr,
        bool stdinIsRedirected,
        Func<string, string, Task> writeFile,
        ILogger logger)
    {
        _settings = settings;
        _healthCheck = healthCheck;
        _stdin = stdin;
        _stdout = stdout;
        _stderr = stderr;
        _stdinIsRedirected = stdinIsRedirected;
        _writeFile = writeFile;
        _logger = logger.ForContext<InitCommand>();
    }

    public async Task<int> RunAsync(bool nonInteractive, string? configOut, CancellationToken ct = default)
    {
        var s = _settings.Value;

        // Identify what's missing for the chosen auth mode.
        var isBearer = s.AuthMode.Equals("Bearer", StringComparison.OrdinalIgnoreCase);
        var missing = CollectMissing(s, isBearer);

        if (missing.Count > 0)
        {
            // Non-interactive or no-TTY: refuse to prompt; print the env-var list.
            if (nonInteractive || _stdinIsRedirected)
            {
                _stderr.WriteLine("Confluence configuration is incomplete. Set the following before running:");
                foreach (var v in missing) _stderr.WriteLine($"  {v}");
                _stderr.WriteLine();
                _stderr.WriteLine("Or re-run `init` interactively (with -it under docker run).");
                return 1;
            }

            // Interactive: prompt for whatever's missing.
            await PromptForMissingAsync(s, isBearer, missing, ct);
        }

        // Validate against Confluence.
        _stdout.WriteLine();
        _stdout.WriteLine("Validating against Confluence...");
        var health = await _healthCheck.CheckAsync(ct);
        if (health.Status != HealthCheckStatus.Healthy)
        {
            _stderr.WriteLine($"Validation failed: {health.Message}");
            if (!string.IsNullOrEmpty(health.Detail))
                _stderr.WriteLine(health.Detail);
            return 1;
        }

        // Write the resolved config. The shape matches ConfluenceSettings so
        // ConfigurationBuilder.AddJsonFile(path) picks it up unchanged.
        var path = configOut ?? ".confluencesynkmd.json";
        try
        {
            var json = SerializeSettings(s);
            await _writeFile(path, json);
            _stdout.WriteLine($"OK — configuration written to {path}.");
            _stdout.WriteLine($"  v2 endpoint reachable. {health.Detail}");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Failed to write config file at {Path}", path);
            _stderr.WriteLine($"Failed to write {path}: {ex.Message}");
            return 1;
        }
    }

    private static List<string> CollectMissing(ConfluenceSettings s, bool isBearer)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(s.BaseUrl))
            missing.Add("CONFLUENCE__BASEURL");

        if (isBearer)
        {
            if (string.IsNullOrWhiteSpace(s.BearerToken))
                missing.Add("CONFLUENCE__BEARERTOKEN");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(s.UserEmail))
                missing.Add("CONFLUENCE__USEREMAIL");
            if (string.IsNullOrWhiteSpace(s.ApiToken))
                missing.Add("CONFLUENCE__APITOKEN");
        }
        return missing;
    }

    private async Task PromptForMissingAsync(
        ConfluenceSettings s, bool isBearer, IReadOnlyList<string> missing, CancellationToken ct)
    {
        _stdout.WriteLine("ConfluenceSynkMD init — first-run wizard");
        _stdout.WriteLine("(Press Enter to accept the default in [brackets], blank values rejected.)");
        _stdout.WriteLine();

        if (missing.Contains("CONFLUENCE__BASEURL"))
            s.BaseUrl = await PromptAsync("Confluence Base URL (e.g. https://yoursite.atlassian.net)", required: true, ct);

        if (isBearer)
        {
            if (missing.Contains("CONFLUENCE__BEARERTOKEN"))
                s.BearerToken = await PromptAsync("OAuth 2.0 Bearer token", required: true, ct);
        }
        else
        {
            if (missing.Contains("CONFLUENCE__USEREMAIL"))
                s.UserEmail = await PromptAsync("Atlassian account email", required: true, ct);
            if (missing.Contains("CONFLUENCE__APITOKEN"))
                s.ApiToken = await PromptAsync("Atlassian API token (visible in this session)", required: true, ct);
        }
    }

    private async Task<string> PromptAsync(string label, bool required, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            _stdout.Write($"{label}: ");
            await _stdout.FlushAsync(ct);
            var line = await _stdin.ReadLineAsync(ct);
            if (line is null)
                throw new IOException("stdin closed before input received.");
            line = line.Trim();
            if (!required || line.Length > 0)
                return line;
            _stdout.WriteLine("  Value required. Please try again.");
        }
    }

    private static string SerializeSettings(ConfluenceSettings s)
    {
        // Shape matches `ConfluenceSettings.SectionName` (= "Confluence") so
        // `ConfigurationBuilder.AddJsonFile(path)` binds it directly.
        var payload = new
        {
            Confluence = new
            {
                s.BaseUrl,
                s.AuthMode,
                s.UserEmail,
                s.ApiToken,
                s.BearerToken,
                s.ApiPath,
                s.ApiVersion,
            }
        };
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
        return JsonSerializer.Serialize(payload, options);
    }
}
