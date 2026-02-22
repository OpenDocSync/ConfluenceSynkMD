using System.CommandLine;
using System.CommandLine.Parsing;
using ConfluentSynkMD.Configuration;
using ConfluentSynkMD.ETL.Core;
using ConfluentSynkMD.ETL.Extract;
using ConfluentSynkMD.ETL.Load;
using ConfluentSynkMD.ETL.Transform;
using ConfluentSynkMD.Models;
using ConfluentSynkMD.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;



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

// --- CLI Definition (System.CommandLine 2.0.3 API) --------------------

var rootCommand = new RootCommand("ConfluentSynkMD: Markdown to Confluence Synchronization Tool");

// -- Core options ------------------------------------------------------------

var modeOption = new Option<SyncMode>("--mode");
modeOption.Description = "Synchronization direction: Upload, Download, or LocalExport.";
modeOption.Required = true;

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

var localOption = new Option<bool>("--local");
localOption.Description = "Only produce local CSF output without API calls.";

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

// -- Register all options ----------------------------------------------------

rootCommand.Options.Add(modeOption);
rootCommand.Options.Add(pathOption);
rootCommand.Options.Add(spaceOption);
rootCommand.Options.Add(parentIdOption);
rootCommand.Options.Add(rootPageOption);
rootCommand.Options.Add(keepHierarchyOption);
rootCommand.Options.Add(skipHierarchyOption);
rootCommand.Options.Add(skipUpdateOption);
rootCommand.Options.Add(localOption);
rootCommand.Options.Add(noWriteBackOption);
rootCommand.Options.Add(logLevelOption);
rootCommand.Options.Add(apiVersionOption);
rootCommand.Options.Add(headersOption);
rootCommand.Options.Add(confBaseUrlOption);
rootCommand.Options.Add(confAuthModeOption);
rootCommand.Options.Add(confUserEmailOption);
rootCommand.Options.Add(confApiTokenOption);
rootCommand.Options.Add(confBearerTokenOption);
rootCommand.Options.Add(headingAnchorsOption);
rootCommand.Options.Add(forceValidUrlOption);
rootCommand.Options.Add(skipTitleHeadingOption);
rootCommand.Options.Add(preferRasterOption);
rootCommand.Options.Add(renderDrawioOption);
rootCommand.Options.Add(renderMermaidOption);
rootCommand.Options.Add(noRenderMermaidOption);
rootCommand.Options.Add(renderPlantumlOption);
rootCommand.Options.Add(renderLatexOption);
rootCommand.Options.Add(diagramFormatOption);
rootCommand.Options.Add(webUiLinksOption);
rootCommand.Options.Add(webUiLinkStrategyOption);
rootCommand.Options.Add(usePanelOption);
rootCommand.Options.Add(forceValidLanguageOption);
rootCommand.Options.Add(codeLineNumbersOption);
rootCommand.Options.Add(debugLineMarkersOption);
rootCommand.Options.Add(titlePrefixOption);
rootCommand.Options.Add(generatedByOption);
rootCommand.Options.Add(imageAlignmentOption);
rootCommand.Options.Add(imageMaxWidthOption);
rootCommand.Options.Add(tableWidthOption);
rootCommand.Options.Add(tableDisplayModeOption);
rootCommand.Options.Add(contentAlignmentOption);

// -- Action handler ----------------------------------------------------------

rootCommand.SetAction(async (parseResult, ct) =>
{
    var mode = parseResult.GetValue(modeOption);
    var path = parseResult.GetValue(pathOption)!;
    var space = parseResult.GetValue(spaceOption)!;
    var parentId = parseResult.GetValue(parentIdOption);
    var isLocal = parseResult.GetValue(localOption);

    // Resolve effective mode: --local flag overrides to LocalExport
    var effectiveMode = isLocal ? SyncMode.LocalExport : mode;

    // Resolve hierarchy: --skip-hierarchy inverts --keep-hierarchy
    var keepHierarchy = parseResult.GetValue(keepHierarchyOption)
                        && !parseResult.GetValue(skipHierarchyOption);

    var options = new SyncOptions(
        effectiveMode, path, space, parentId,
        RootPage: parseResult.GetValue(rootPageOption),
        KeepHierarchy: keepHierarchy,
        SkipUpdate: parseResult.GetValue(skipUpdateOption),
        LocalOnly: isLocal,
        NoWriteBack: parseResult.GetValue(noWriteBackOption),
        LogLevel: parseResult.GetValue(logLevelOption)!);

    // Apply CLI log level to Serilog
    logLevelSwitch.MinimumLevel = MapLogLevel(options.LogLevel);

    var runId = Guid.NewGuid().ToString("N");
    using var runIdScope = LogContext.PushProperty("RunId", runId);
    using var modeScope = LogContext.PushProperty("Mode", effectiveMode.ToString());
    using var spaceScope = LogContext.PushProperty("Space", space);
    using var pathScope = LogContext.PushProperty("Path", path);
    var runLogger = Log.ForContext("SourceContext", "ConfluentSynkMD.Run");

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
    if (ConfluenceCredentialPolicy.RequiresCredentials(effectiveMode))
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

    if (effectiveMode == SyncMode.Upload)
    {
        pipeline
            .AddExtractor(host.Services.GetRequiredService<MarkdownIngestionStep>())
            .AddTransformer(host.Services.GetRequiredService<ConfluenceXhtmlTransformStep>())
            .AddLoader(host.Services.GetRequiredService<ConfluenceLoadStep>())
            .AddLoader(host.Services.GetRequiredService<WriteBackStep>());
    }
    else if (effectiveMode == SyncMode.LocalExport)
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
        Environment.ExitCode = 1;
    }
});

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

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
