# Bauen & Testen

Anleitung zum Bauen, Testen und Messen der Codeabdeckung.

---

## Build

```bash
dotnet restore
dotnet build
dotnet build --configuration Release -p:TreatWarningsAsErrors=false

# Optionaler strikter Quality-Gate
dotnet build --configuration Release -p:TreatWarningsAsErrors=true
```

---

## Tests ausführen

```bash
dotnet test
dotnet test --verbosity normal
dotnet test --filter "FullyQualifiedName~MarkdownIngestionStepTests"
```

---

## Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:TestResults/**/coverage.cobertura.xml \
  -targetdir:TestResults/html \
  -reporttypes:Html
```

---

## Round-Trip-Sync-Test

```bash
docker-compose -f docker-compose.sync-test.yml build
docker-compose -f docker-compose.sync-test.yml up
```

### Was getestet wird

| Prüfung | Kriterium |
|---|---|
| Dateistruktur | Exakte Dateianzahl und Verzeichnispfad-Gleichheit |
| Überschriften-Struktur | 100% Überschriften-Übereinstimmung |
| Inhaltsähnlichkeit | Normalisierter Vergleich (≥ 80% Schwellenwert) |
| Anhänge | Bildverweise pro Dokument |

---

## CI-Pipeline

Die GitHub Actions CI-Pipeline läuft bei jedem Push auf `main` und bei Pull Requests. Siehe [CI/CD-Integration](../admin/cicd.md) für Details.
