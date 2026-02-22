# ETL-Pipeline

Die ETL-Pipeline (Extract-Transform-Load) ist die zentrale Ausführungsengine von ConfluentSynkMD.

---

## Kern-Interfaces

### `IPipelineStep`

Jeder Pipeline-Schritt implementiert dieses Interface:

```csharp
public interface IPipelineStep
{
    Task<PipelineResult> ExecuteAsync(
        TranslationBatchContext context, 
        CancellationToken ct);
}
```

### `TranslationBatchContext`

Das gemeinsame Kontextobjekt, das durch die Pipeline fließt und `SyncOptions`, `ConverterOptions`, `LayoutOptions` sowie die `Documents`-Liste enthält.

### `PipelineResult`

Jeder Schritt gibt ein `PipelineResult` zurück, das Erfolg/Fehler und die Fortsetzung der Pipeline anzeigt.

---

## Pipeline-Builder

```csharp
var pipeline = new ETLPipelineBuilder()
    .AddExtractor(extractStep)
    .AddTransformer(transformStep)
    .AddLoader(loadStep1)
    .AddLoader(loadStep2);

var result = await pipeline.ExecuteAsync(context, runner, ct);
```

---

## Pipeline erweitern

1. Klasse erstellen, die `IPipelineStep` implementiert
2. Im DI-Container in `Program.cs` registrieren
3. Zur Pipeline-Komposition im Modus-Handler hinzufügen

```csharp
public class MeinSchritt : IPipelineStep
{
    public async Task<PipelineResult> ExecuteAsync(
        TranslationBatchContext context, CancellationToken ct)
    {
        // context.Documents verarbeiten...
        return new PipelineResult(true);
    }
}
```
