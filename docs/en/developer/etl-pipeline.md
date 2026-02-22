# ETL Pipeline

The ETL (Extract-Transform-Load) pipeline is the core execution engine of ConfluentSynkMD. It orchestrates document processing through a series of composable steps.

---

## Core Interfaces

### `IPipelineStep`

Every pipeline step implements this interface:

```csharp
public interface IPipelineStep
{
    Task<PipelineResult> ExecuteAsync(
        TranslationBatchContext context, 
        CancellationToken ct);
}
```

### `TranslationBatchContext`

The shared context object that flows through the pipeline, carrying:

- `SyncOptions` — CLI options (mode, path, space key, etc.)
- `ConverterOptions` — Converter flags (heading anchors, code line numbers, etc.)
- `LayoutOptions` — Layout settings (image alignment, table width, etc.)
- `Documents` — The list of `DocumentNode` objects being processed

### `PipelineResult`

Each step returns a `PipelineResult` indicating success/failure and whether the pipeline should continue:

```csharp
public record PipelineResult(bool CanContinue, string? ErrorMessage = null);
```

---

## Pipeline Builder

The `ETLPipelineBuilder` provides a fluent API for composing pipelines:

```csharp
var pipeline = new ETLPipelineBuilder()
    .AddExtractor(extractStep)
    .AddTransformer(transformStep)
    .AddLoader(loadStep1)
    .AddLoader(loadStep2);   // Multiple loaders are supported

var result = await pipeline.ExecuteAsync(context, runner, ct);
```

Steps are executed in order: all extractors → all transformers → all loaders.

---

## Pipeline Runner

The `PipelineRunner` executes the steps sequentially. If any step returns `CanContinue = false`, the pipeline aborts:

```csharp
foreach (var step in steps)
{
    var result = await step.ExecuteAsync(context, ct);
    if (!result.CanContinue) return result;
}
```

---

## Extending the Pipeline

To add a new pipeline step:

1. Create a class implementing `IPipelineStep`
2. Register it in the DI container in `Program.cs`
3. Add it to the appropriate pipeline composition in the mode handler

```csharp
// Step implementation
public class MyCustomStep : IPipelineStep
{
    public async Task<PipelineResult> ExecuteAsync(
        TranslationBatchContext context, CancellationToken ct)
    {
        // Process context.Documents...
        return new PipelineResult(true);
    }
}

// Registration in Program.cs
builder.Services.AddTransient<MyCustomStep>();

// Add to pipeline
pipeline.AddLoader(host.Services.GetRequiredService<MyCustomStep>());
```
