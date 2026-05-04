using ConfluenceSynkMD.Services;
using Serilog;

namespace ConfluenceSynkMD.Commands;

/// <summary>
/// `confluencesynkmd doctor [--renderers-only]` — runtime self-test.
/// Renders a small canary diagram via each external-process renderer
/// (Mermaid, Draw.io, PlantUML, LaTeX) and probes the configured Confluence
/// instance via <see cref="IConfluenceHealthCheck"/>.
///
/// Exit codes:
///   0 — every check passed.
///   1 — at least one check failed.
///
/// The release-container.yml smoke step calls this command with
/// <c>--renderers-only</c> against the freshly-built image to gate the push.
/// Plain <c>doctor</c> (no flag) is for end-users who want to verify their
/// container + auth before doing a real upload.
/// </summary>
public sealed class DoctorCommand
{
    private readonly IMermaidRenderer _mermaid;
    private readonly DrawioRenderer _drawio;
    private readonly PlantUmlRenderer _plantuml;
    private readonly ILatexRenderer _latex;
    private readonly IConfluenceHealthCheck _healthCheck;
    private readonly TextWriter _stdout;
    private readonly ILogger _logger;

    public DoctorCommand(
        IMermaidRenderer mermaid,
        DrawioRenderer drawio,
        PlantUmlRenderer plantuml,
        ILatexRenderer latex,
        IConfluenceHealthCheck healthCheck,
        TextWriter stdout,
        ILogger logger)
    {
        _mermaid = mermaid;
        _drawio = drawio;
        _plantuml = plantuml;
        _latex = latex;
        _healthCheck = healthCheck;
        _stdout = stdout;
        _logger = logger.ForContext<DoctorCommand>();
    }

    public async Task<int> RunAsync(bool renderersOnly, CancellationToken ct = default)
    {
        var results = new List<(string Name, bool Ok, string? Detail)>();

        // Each renderer's public API has a slightly different shape; the
        // canary lambdas adapt them all to "produce a non-empty byte[]"
        // which is all the helper actually checks.
        results.Add(await RunRendererCanaryAsync("Mermaid", async () =>
            (await _mermaid.RenderToPngAsync(MermaidCanary, ct)).PngBytes));
        results.Add(await RunRendererCanaryAsync("Draw.io", async () =>
            (await _drawio.RenderAsync(DrawioCanary, "png", ct)).ImageBytes));
        results.Add(await RunRendererCanaryAsync("PlantUML", async () =>
            (await _plantuml.RenderAsync(PlantumlCanary, "png", ct)).ImageBytes));
        results.Add(await RunRendererCanaryAsync("LaTeX", async () =>
            (await _latex.RenderAsync(LatexCanary, ct)).ImageBytes));

        if (!renderersOnly)
        {
            var health = await _healthCheck.CheckAsync(ct);
            var ok = health.Status == HealthCheckStatus.Healthy;
            var detail = string.IsNullOrEmpty(health.Detail) ? health.Message : $"{health.Message} {health.Detail}";
            results.Add(("Confluence auth", ok, detail));
        }

        // Print checklist
        _stdout.WriteLine();
        _stdout.WriteLine("ConfluenceSynkMD doctor — runtime self-test");
        _stdout.WriteLine(new string('-', 47));
        foreach (var (name, ok, detail) in results)
        {
            var marker = ok ? "[ OK ]" : "[FAIL]";
            _stdout.WriteLine($"  {marker}  {name}");
            if (!ok && !string.IsNullOrEmpty(detail))
            {
                _stdout.WriteLine($"           {Indent(detail, "           ")}");
            }
        }
        _stdout.WriteLine();

        return results.All(r => r.Ok) ? 0 : 1;
    }

    private async Task<(string Name, bool Ok, string? Detail)> RunRendererCanaryAsync(
        string name,
        Func<Task<byte[]>> render)
    {
        try
        {
            var bytes = await render();
            if (bytes is null || bytes.Length == 0)
            {
                return (name, false, "Renderer returned 0 bytes.");
            }
            return (name, true, null);
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Renderer canary {Renderer} failed.", name);
            return (name, false, ex.Message);
        }
    }

    private static string Indent(string text, string indent)
    {
        // Indent every line after the first so multi-line errors line up under
        // the marker column.
        var lines = text.Split('\n');
        return string.Join('\n' + indent, lines.Select(l => l.TrimEnd('\r')));
    }

    // ── Renderer canary inputs ──────────────────────────────────────────────
    // Kept tiny so the doctor command runs in seconds. Each input exercises
    // the renderer's full pipeline (parse → spawn external process → read
    // back image bytes) without producing visually meaningful output — we
    // only assert the renderer doesn't blow up.

    private const string MermaidCanary =
        "graph TD\n  A-->B";

    private const string PlantumlCanary =
        "@startuml\nA -> B\n@enduml";

    // Real drawio-desktop will silently exit 0 without producing output when
    // the input mxfile is missing the page-level attributes the editor would
    // normally write (dx/dy/grid/pageWidth/pageHeight). This canary mirrors
    // the exact shape diagrams.net writes for an empty new page.
    private const string DrawioCanary =
        "<mxfile host=\"app.diagrams.net\" version=\"22.0.0\">" +
        "<diagram name=\"Page-1\" id=\"doctor\">" +
        "<mxGraphModel dx=\"800\" dy=\"600\" grid=\"1\" gridSize=\"10\" guides=\"1\" " +
        "tooltips=\"1\" connect=\"1\" arrows=\"1\" fold=\"1\" page=\"1\" pageScale=\"1\" " +
        "pageWidth=\"850\" pageHeight=\"1100\" math=\"0\" shadow=\"0\">" +
        "<root>" +
        "<mxCell id=\"0\"/><mxCell id=\"1\" parent=\"0\"/>" +
        "<mxCell id=\"2\" value=\"doctor\" style=\"rounded=0;whiteSpace=wrap;html=1;\" " +
        "vertex=\"1\" parent=\"1\">" +
        "<mxGeometry x=\"40\" y=\"40\" width=\"120\" height=\"60\" as=\"geometry\"/>" +
        "</mxCell>" +
        "</root></mxGraphModel></diagram></mxfile>";

    private const string LatexCanary = @"\frac{1}{2}";
}
