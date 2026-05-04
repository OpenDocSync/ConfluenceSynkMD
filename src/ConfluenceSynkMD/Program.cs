using System.CommandLine;
using System.CommandLine.Parsing;
using ConfluenceSynkMD.Commands;
using ConfluenceSynkMD.Configuration;
using ConfluenceSynkMD.ETL.Core;
using ConfluenceSynkMD.ETL.Extract;
using ConfluenceSynkMD.ETL.Load;
using ConfluenceSynkMD.ETL.Transform;
using ConfluenceSynkMD.Models;
using ConfluenceSynkMD.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;



// --- Pre-parse migration check (v0.1.0 breaking change) ---------------------
// `--mode Upload|Download|Local` was replaced by `upload|download|local` subcommands.
// If a user runs the old syntax, surface a clear migration message instead of
// the bare "Unrecognized option --mode" from System.CommandLine.
var migrationHint = CliMigrationCheck.CheckLegacyModeFlag(args);
if (migrationHint is not null)
{
    Console.Error.WriteLine(migrationHint.Message);
    return 2;
}

// --- Build Host -------------------------------------------------------------

var builder = Host.CreateApplicationBuilder(args);

// Configuration: bind Confluence settings from env vars (CONFLUENCE__*)
builder.Services.Configure<ConfluenceSettings>(
    builder.Configuration.GetSection(ConfluenceSettings.SectionName));

// Serilog (default level, overridden by CLI --loglevel at runtime via switch)
var logLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
builder.Services.AddSerilog(config => config
    .MinimumLevel.ControlledBy(logLevelSwitch)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] [run:{RunId}] {Message:lj}{NewLine}{Exception}",
        formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .WriteTo.File(
        formatter: new JsonFormatter(renderMessage: true),
        path: "logs/md2conf-.json",
        rollingInterval: RollingInterval.Day));
builder.Services.AddSingleton<Serilog.ILogger>(_ => Log.Logger);

// --- Register Services ------------------------------------------------------

// Confluence API client (typed HttpClient)
builder.Services.AddHttpClient<IConfluenceApiClient, ConfluenceApiClient>();

// Confluence health check (separate typed HttpClient — independent connection
// pool and request headers from the main API client; consumed by `doctor`).
builder.Services.AddHttpClient<IConfluenceHealthCheck, ConfluenceHealthCheck>();

// Doctor command (resolves all four renderers + the health check).
builder.Services.AddTransient<DoctorCommand>(sp => new DoctorCommand(
    sp.GetRequiredService<IMermaidRenderer>(),
    sp.GetRequiredService<DrawioRenderer>(),
    sp.GetRequiredService<PlantUmlRenderer>(),
    sp.GetRequiredService<ILatexRenderer>(),
    sp.GetRequiredService<IConfluenceHealthCheck>(),
    Console.Out,
    sp.GetRequiredService<Serilog.ILogger>()));

// Shared services
builder.Services.AddSingleton<FrontmatterParser>();
builder.Services.AddSingleton<SlugGenerator>();
builder.Services.AddSingleton<HierarchyResolver>();
builder.Services.AddSingleton<MermaidRenderer>();
builder.Services.AddSingleton<IMermaidRenderer>(sp => sp.GetRequiredService<MermaidRenderer>());
builder.Services.AddSingleton<ImageOptimizer>();
builder.Services.AddSingleton<IImageOptimizer>(sp => sp.GetRequiredService<ImageOptimizer>());

// Diagram renderers (optional, used when respective --render-* flags are enabled)
builder.Services.AddSingleton<DrawioRenderer>();
builder.Services.AddSingleton<PlantUmlRenderer>();
builder.Services.AddSingleton<ILatexRenderer, LatexRenderer>();

// ETL Core
builder.Services.AddSingleton<PipelineRunner>();

// ETL Extract steps
builder.Services.AddTransient<MarkdownIngestionStep>();
builder.Services.AddTransient<ConfluenceIngestionStep>();

// ETL Transform steps (factory needed: two IDiagramRenderer params resolved by concrete type)
builder.Services.AddTransient<ConfluenceXhtmlTransformStep>(sp => new ConfluenceXhtmlTransformStep(
    sp.GetRequiredService<IMermaidRenderer>(),
    sp.GetRequiredService<DrawioRenderer>(),
    sp.GetRequiredService<PlantUmlRenderer>(),
    sp.GetRequiredService<ILatexRenderer>(),
    sp.GetRequiredService<IImageOptimizer>(),
    sp.GetRequiredService<Serilog.ILogger>()));
builder.Services.AddTransient<MarkdownTransformStep>();

// ETL Load steps
builder.Services.AddTransient<ConfluenceLoadStep>();
builder.Services.AddTransient<WriteBackStep>();
builder.Services.AddTransient<FileSystemLoadStep>();
builder.Services.AddTransient<LocalOnlyLoadStep>();

var host = builder.Build();

// --- CLI Definition (System.CommandLine 2.0.3 API) --------------------------
//
// v0.1.0 shape: subcommand tree with three top-level verbs that mirror the old
// SyncMode enum values. `--mode` and `--local` flags were removed in this
// release; the migration message above catches users on the old syntax.

var rootCommand = new RootCommand("ConfluenceSynkMD: Markdown to Confluence Synchronization Tool");

// -- Core options ------------------------------------------------------------

var pathOption = new Option<string>("--path");
pathOption.Description = "Local filesystem path to a Markdown repository root or subfolder.";
pathOption.Required = true;

var spaceOption = new Option<string>("--conf-space");
spaceOption.Description = "Confluence Space Key (e.g. 'DEV', 'DOCS').";
spaceOption.Required = true;

var parentIdOption = new Option<string?>("--conf-parent-id");
parentIdOption.Description = "Optional parent page ID for subtree operations.";

// -- Sync control options ----------------------------------------------------

var rootPageOption = new Option<string?>("--root-page");
rootPageOption.Description = "Root page title to upload under (alternative to --conf-parent-id).";

var keepHierarchyOption = new Option<bool>("--keep-hierarchy");
keepHierarchyOption.Description = "Preserve local directory hierarchy in Confluence.";
keepHierarchyOption.DefaultValueFactory = _ => true;

var skipHierarchyOption = new Option<bool>("--skip-hierarchy");
skipHierarchyOption.Description = "Flatten all pages under the root (inverse of --keep-hierarchy).";

var skipUpdateOption = new Option<bool>("--skip-update");
skipUpdateOption.Description = "Skip uploading pages whose content has not changed.";

var noWriteBackOption = new Option<bool>("--no-write-back");
noWriteBackOption.Description = "Do not write Confluence Page-ID back into Markdown frontmatter after upload.";

var logLevelOption = new Option<string>("--loglevel");
logLevelOption.Description = "Logging verbosity: debug, info, warning, error, critical.";
logLevelOption.DefaultValueFactory = _ => "info";

var apiVersionOption = new Option<string>("--api-version");
apiVersionOption.Description = "Confluence REST API version: v1 or v2 (default v2 for Cloud).";
apiVersionOption.DefaultValueFactory = _ => "v2";

var headersOption = new Option<string[]>("--headers");
headersOption.Description = "Custom HTTP headers in KEY=VALUE format (can specify multiple).";
headersOption.Arity = ArgumentArity.ZeroOrMore;

// -- Credential options (override environment variables) -------------------------

var confBaseUrlOption = new Option<string?>("--conf-base-url");
confBaseUrlOption.Description = "Confluence Cloud base URL (overrides CONFLUENCE__BASEURL).";

var confAuthModeOption = new Option<string?>("--conf-auth-mode");
confAuthModeOption.Description = "Authentication mode: Basic or Bearer (overrides CONFLUENCE__AUTHMODE).";

var confUserEmailOption = new Option<string?>("--conf-user-email");
confUserEmailOption.Description = "User email for Basic auth (overrides CONFLUENCE__USEREMAIL).";

var confApiTokenOption = new Option<string?>("--conf-api-token");
confApiTokenOption.Description = "API token for Basic auth (overrides CONFLUENCE__APITOKEN).";

var confBearerTokenOption = new Option<string?>("--conf-bearer-token");
confBearerTokenOption.Description = "Bearer token for OAuth 2.0 auth (overrides CONFLUENCE__BEARERTOKEN).";

// -- Converter options -------------------------------------------------------

var headingAnchorsOption = new Option<bool>("--heading-anchors");
headingAnchorsOption.Description = "Inject anchor macros before headings for deep-linking.";

var forceValidUrlOption = new Option<bool>("--force-valid-url");
forceValidUrlOption.Description = "Sanitize and escape invalid URLs.";

var skipTitleHeadingOption = new Option<bool>("--skip-title-heading");
skipTitleHeadingOption.Description = "Omit the first H1 heading (used as page title).";

var preferRasterOption = new Option<bool>("--prefer-raster");
preferRasterOption.Description = "Prefer raster images over vector (e.g. PNG over SVG).";

var renderDrawioOption = new Option<bool>("--render-drawio");
renderDrawioOption.Description = "Render Draw.io code blocks as image attachments.";

var renderMermaidOption = new Option<bool>("--render-mermaid");
renderMermaidOption.Description = "Render Mermaid code blocks as image attachments.";
renderMermaidOption.DefaultValueFactory = _ => true;

var noRenderMermaidOption = new Option<bool>("--no-render-mermaid");
noRenderMermaidOption.Description = "Disable Mermaid rendering (overrides --render-mermaid default).";

var renderPlantumlOption = new Option<bool>("--render-plantuml");
renderPlantumlOption.Description = "Render PlantUML code blocks as image attachments.";

var renderLatexOption = new Option<bool>("--render-latex");
renderLatexOption.Description = "Render LaTeX code blocks as image attachments.";

var diagramFormatOption = new Option<string>("--diagram-output-format");
diagramFormatOption.Description = "Output format for rendered diagrams: png or svg.";
diagramFormatOption.DefaultValueFactory = _ => "png";

var webUiLinksOption = new Option<bool>("--webui-links");
webUiLinksOption.Description = "Render internal links as Confluence Web UI URLs.";

var webUiLinkStrategyOption = new Option<string>("--webui-link-strategy");
webUiLinkStrategyOption.Description = "Web UI link strategy: space-title or page-id.";
webUiLinkStrategyOption.DefaultValueFactory = _ => "space-title";

var usePanelOption = new Option<bool>("--use-panel");
usePanelOption.Description = "Use panel macro instead of info/note/warning for alerts.";

var forceValidLanguageOption = new Option<bool>("--force-valid-language");
forceValidLanguageOption.Description = "Validate code block languages against Confluence-supported set.";

var codeLineNumbersOption = new Option<bool>("--code-line-numbers");
codeLineNumbersOption.Description = "Show line numbers in Confluence code block macros.";
codeLineNumbersOption.Aliases.Add("--line-numbers"); // backward compat

var debugLineMarkersOption = new Option<bool>("--debug-line-markers");
debugLineMarkersOption.Description = "Include source line numbers in error messages for debugging conversion failures.";

var titlePrefixOption = new Option<string?>("--title-prefix");
titlePrefixOption.Description = "Prefix prepended to all page titles (e.g. '[AUTO] ').";

var generatedByOption = new Option<string?>("--generated-by");
generatedByOption.Description = "Tool identifier for generated-by marker. Set to empty to disable.";
generatedByOption.DefaultValueFactory = _ => "MARKDOWN";

// -- Layout options ----------------------------------------------------------

var imageAlignmentOption = new Option<string?>("--layout-image-alignment");
imageAlignmentOption.Description = "Image alignment: center, left, right, or None.";

var imageMaxWidthOption = new Option<int?>("--layout-image-max-width");
imageMaxWidthOption.Description = "Maximum width for images in pixels.";

var tableWidthOption = new Option<int?>("--layout-table-width");
tableWidthOption.Description = "Table width in pixels.";

var tableDisplayModeOption = new Option<string>("--layout-table-display-mode");
tableDisplayModeOption.Description = "Table display mode: responsive or fixed.";
tableDisplayModeOption.DefaultValueFactory = _ => "responsive";

var contentAlignmentOption = new Option<string?>("--layout-alignment");
contentAlignmentOption.Description = "Content alignment: center, left, right, or None.";

// -- Helper: register every option on a subcommand ---------------------------
// Each subcommand accepts the same surface — keeps the per-mode CLI parity
// the regression test R1 covers, and avoids per-subcommand option drift.

void AddSharedOptions(Command cmd)
{
    cmd.Options.Add(pathOption);
    cmd.Options.Add(spaceOption);
    cmd.Options.Add(parentIdOption);
    cmd.Options.Add(rootPageOption);
    cmd.Options.Add(keepHierarchyOption);
    cmd.Options.Add(skipHierarchyOption);
    cmd.Options.Add(skipUpdateOption);
    cmd.Options.Add(noWriteBackOption);
    cmd.Options.Add(logLevelOption);
    cmd.Options.Add(apiVersionOption);
    cmd.Options.Add(headersOption);
    cmd.Options.Add(confBaseUrlOption);
    cmd.Options.Add(confAuthModeOption);
    cmd.Options.Add(confUserEmailOption);
    cmd.Options.Add(confApiTokenOption);
    cmd.Options.Add(confBearerTokenOption);
    cmd.Options.Add(headingAnchorsOption);
    cmd.Options.Add(forceValidUrlOption);
    cmd.Options.Add(skipTitleHeadingOption);
    cmd.Options.Add(preferRasterOption);
    cmd.Options.Add(renderDrawioOption);
    cmd.Options.Add(renderMermaidOption);
    cmd.Options.Add(noRenderMermaidOption);
    cmd.Options.Add(renderPlantumlOption);
    cmd.Options.Add(renderLatexOption);
    cmd.Options.Add(diagramFormatOption);
    cmd.Options.Add(webUiLinksOption);
    cmd.Options.Add(webUiLinkStrategyOption);
    cmd.Options.Add(usePanelOption);
    cmd.Options.Add(forceValidLanguageOption);
    cmd.Options.Add(codeLineNumbersOption);
    cmd.Options.Add(debugLineMarkersOption);
    cmd.Options.Add(titlePrefixOption);
    cmd.Options.Add(generatedByOption);
    cmd.Options.Add(imageAlignmentOption);
    cmd.Options.Add(imageMaxWidthOption);
    cmd.Options.Add(tableWidthOption);
    cmd.Options.Add(tableDisplayModeOption);
    cmd.Options.Add(contentAlignmentOption);
}

// -- Subcommands -------------------------------------------------------------

var uploadCommand = new Command("upload", "Upload Markdown documents to Confluence.");
AddSharedOptions(uploadCommand);
uploadCommand.SetAction((parseResult, ct) => RunPipelineAsync(parseResult, ct, SyncMode.Upload, localOnly: false));

var downloadCommand = new Command("download", "Download Confluence pages back into Markdown.");
AddSharedOptions(downloadCommand);
downloadCommand.SetAction((parseResult, ct) => RunPipelineAsync(parseResult, ct, SyncMode.Download, localOnly: false));

var localCommand = new Command("local", "Produce local Confluence Storage Format output without API calls.");
AddSharedOptions(localCommand);
localCommand.SetAction((parseResult, ct) => RunPipelineAsync(parseResult, ct, SyncMode.LocalExport, localOnly: true));

// `doctor` does not share the sync-pipeline options — its surface is just
// `--renderers-only`. It runs renderer canaries and the Confluence health
// probe, then prints a green/red checklist. The release-container.yml smoke
// step calls this with `--renderers-only` against the freshly-built image.
var renderersOnlyOption = new Option<bool>("--renderers-only");
renderersOnlyOption.Description = "Skip the Confluence auth probe; only validate renderer health.";

var doctorCommand = new Command("doctor", "Runtime self-test: render canaries via every renderer and probe Confluence auth.");
doctorCommand.Options.Add(renderersOnlyOption);
doctorCommand.SetAction(async (parseResult, ct) =>
{
    var renderersOnly = parseResult.GetValue(renderersOnlyOption);
    var doctor = host.Services.GetRequiredService<DoctorCommand>();
    return await doctor.RunAsync(renderersOnly, ct);
});

rootCommand.Subcommands.Add(uploadCommand);
rootCommand.Subcommands.Add(downloadCommand);
rootCommand.Subcommands.Add(localCommand);
rootCommand.Subcommands.Add(doctorCommand);

// -- Pipeline runner (shared by all three subcommands) -----------------------

async Task<int> RunPipelineAsync(ParseResult parseResult, CancellationToken ct, SyncMode mode, bool localOnly)
{
    var path = parseResult.GetValue(pathOption)!;
    var space = parseResult.GetValue(spaceOption)!;
    var parentId = parseResult.GetValue(parentIdOption);

    // Resolve hierarchy: --skip-hierarchy inverts --keep-hierarchy
    var keepHierarchy = parseResult.GetValue(keepHierarchyOption)
                        && !parseResult.GetValue(skipHierarchyOption);

    var options = new SyncOptions(
        mode, path, space, parentId,
        RootPage: parseResult.GetValue(rootPageOption),
        KeepHierarchy: keepHierarchy,
        SkipUpdate: parseResult.GetValue(skipUpdateOption),
        LocalOnly: localOnly,
        NoWriteBack: parseResult.GetValue(noWriteBackOption),
        LogLevel: parseResult.GetValue(logLevelOption)!);

    // Apply CLI log level to Serilog
    logLevelSwitch.MinimumLevel = MapLogLevel(options.LogLevel);

    var runId = Guid.NewGuid().ToString("N");
    using var runIdScope = LogContext.PushProperty("RunId", runId);
    using var modeScope = LogContext.PushProperty("Mode", mode.ToString());
    using var spaceScope = LogContext.PushProperty("Space", space);
    using var pathScope = LogContext.PushProperty("Path", path);
    var runLogger = Log.ForContext("SourceContext", "ConfluenceSynkMD.Run");

    runLogger.Information("PipelineStarted");

    // Apply CLI overrides to ConfluenceSettings (api-version, headers, credentials)
    var confluenceSettings = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ConfluenceSettings>>().Value;
    confluenceSettings.ApiVersion = parseResult.GetValue(apiVersionOption)!;
    var headerValues = parseResult.GetValue(headersOption);
    if (headerValues is not null)
    {
        foreach (var header in headerValues)
        {
            var eqIndex = header.IndexOf('=');
            if (eqIndex > 0)
                confluenceSettings.CustomHeaders[header[..eqIndex].Trim()] = header[(eqIndex + 1)..].Trim();
        }
    }

    // Apply CLI credential overrides (CLI > Environment > Defaults)
    var cliBaseUrl = parseResult.GetValue(confBaseUrlOption);
    if (cliBaseUrl is not null) confluenceSettings.BaseUrl = cliBaseUrl;

    var cliAuthMode = parseResult.GetValue(confAuthModeOption);
    if (cliAuthMode is not null) confluenceSettings.AuthMode = cliAuthMode;

    var cliEmail = parseResult.GetValue(confUserEmailOption);
    if (cliEmail is not null) confluenceSettings.UserEmail = cliEmail;

    var cliToken = parseResult.GetValue(confApiTokenOption);
    if (cliToken is not null) confluenceSettings.ApiToken = cliToken;

    var cliBearer = parseResult.GetValue(confBearerTokenOption);
    if (cliBearer is not null) confluenceSettings.BearerToken = cliBearer;

    // Fail-fast validation (only for modes that perform Confluence API calls)
    if (ConfluenceCredentialPolicy.RequiresCredentials(mode))
        ConfluenceSettingsValidator.ValidateOrThrow(confluenceSettings);

    var converterOptions = new ConverterOptions
    {
        HeadingAnchors = parseResult.GetValue(headingAnchorsOption),
        ForceValidUrl = parseResult.GetValue(forceValidUrlOption),
        SkipTitleHeading = parseResult.GetValue(skipTitleHeadingOption),
        PreferRaster = parseResult.GetValue(preferRasterOption),
        RenderDrawio = parseResult.GetValue(renderDrawioOption),
        RenderMermaid = parseResult.GetValue(renderMermaidOption) && !parseResult.GetValue(noRenderMermaidOption),
        RenderPlantuml = parseResult.GetValue(renderPlantumlOption),
        RenderLatex = parseResult.GetValue(renderLatexOption),
        DiagramOutputFormat = parseResult.GetValue(diagramFormatOption)!,
        WebUiLinks = parseResult.GetValue(webUiLinksOption),
        WebUiLinkStrategy = parseResult.GetValue(webUiLinkStrategyOption)!,
        UsePanel = parseResult.GetValue(usePanelOption),
        ForceValidLanguage = parseResult.GetValue(forceValidLanguageOption),
        CodeLineNumbers = parseResult.GetValue(codeLineNumbersOption),
        DebugLineMarkers = parseResult.GetValue(debugLineMarkersOption),
        TitlePrefix = parseResult.GetValue(titlePrefixOption),
        GeneratedBy = parseResult.GetValue(generatedByOption),
    };

    var layoutOptions = new LayoutOptions
    {
        ImageAlignment = parseResult.GetValue(imageAlignmentOption),
        ImageMaxWidth = parseResult.GetValue(imageMaxWidthOption),
        TableWidth = parseResult.GetValue(tableWidthOption),
        TableDisplayMode = parseResult.GetValue(tableDisplayModeOption)!,
        ContentAlignment = parseResult.GetValue(contentAlignmentOption),
    };

    var runner = host.Services.GetRequiredService<PipelineRunner>();

    // Build context
    var context = new TranslationBatchContext
    {
        Options = options,
        ConverterOptions = converterOptions,
        LayoutOptions = layoutOptions,
    };

    // Build pipeline based on mode
    var pipeline = new ETLPipelineBuilder();

    if (mode == SyncMode.Upload)
    {
        pipeline
            .AddExtractor(host.Services.GetRequiredService<MarkdownIngestionStep>())
            .AddTransformer(host.Services.GetRequiredService<ConfluenceXhtmlTransformStep>())
            .AddLoader(host.Services.GetRequiredService<ConfluenceLoadStep>())
            .AddLoader(host.Services.GetRequiredService<WriteBackStep>());
    }
    else if (mode == SyncMode.LocalExport)
    {
        pipeline
            .AddExtractor(host.Services.GetRequiredService<MarkdownIngestionStep>())
            .AddTransformer(host.Services.GetRequiredService<ConfluenceXhtmlTransformStep>())
            .AddLoader(host.Services.GetRequiredService<LocalOnlyLoadStep>());
    }
    else
    {
        pipeline
            .AddExtractor(host.Services.GetRequiredService<ConfluenceIngestionStep>())
            .AddTransformer(host.Services.GetRequiredService<MarkdownTransformStep>())
            .AddLoader(host.Services.GetRequiredService<FileSystemLoadStep>());
    }

    // Execute pipeline
    var result = await pipeline.ExecuteAsync(context, runner, ct);

    runLogger.Information("PipelineCompleted: CanContinue={CanContinue}", result.CanContinue);

    // Set exit code based on result
    if (!result.CanContinue)
    {
        runLogger.Error("PipelineFailed");
        return 1;
    }

    return 0;
}

// -- Helper: map CLI log level string to Serilog LogEventLevel ---------------

static LogEventLevel MapLogLevel(string level) => level.ToLowerInvariant() switch
{
    "debug" or "verbose" => LogEventLevel.Debug,
    "info" or "information" => LogEventLevel.Information,
    "warning" or "warn" => LogEventLevel.Warning,
    "error" => LogEventLevel.Error,
    "critical" or "fatal" => LogEventLevel.Fatal,
    _ => LogEventLevel.Information
};

var rootParseResult = rootCommand.Parse(args);
return await rootParseResult.InvokeAsync();
