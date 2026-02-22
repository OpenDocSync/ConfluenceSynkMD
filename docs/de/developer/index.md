# Entwicklerhandbuch

Willkommen im ConfluentSynkMD-Entwicklerhandbuch. Dieser Bereich ist für Mitwirkende und Entwickler, die die Codebasis verstehen, erweitern oder Fehler beheben möchten.

---

## Technologie-Stack

| Komponente | Technologie |
|---|---|
| Runtime | .NET 10 |
| CLI-Framework | `System.CommandLine` |
| Markdown Parser | Markdig |
| HTML Parser | AngleSharp (für XHTML → Markdown) |
| DI Container | `Microsoft.Extensions.DependencyInjection` |
| Logging | Serilog |
| Testing | xUnit + Moq |
| Diagramme | mermaid-cli (Node.js) |
| Container | Docker (Multi-Stage-Build) |

---

## Repository-Struktur

```
ConfluentSynkMD/
├── src/ConfluentSynkMD/           # Hauptanwendung
│   ├── Configuration/             # Einstellungs-Records
│   ├── ETL/                       # Extract-Transform-Load-Pipeline
│   ├── Markdig/                   # Benutzerdefinierte Markdig-Renderer
│   ├── Models/                    # Domänenmodelle
│   └── Services/                  # API-Client, Resolver, Renderer
├── tests/ConfluentSynkMD.Tests/   # Unit- & Integrationstests
├── docs/                          # MkDocs-Dokumentation (diese Seite)
└── Dockerfile                     # Multi-Stage Docker-Build
```

---

## Einstieg

```bash
git clone https://github.com/OpenDocSync/ConfluentSynkMD.git
cd ConfluentSynkMD
dotnet restore
dotnet build
dotnet test
```

---

## Weiterführend

- [Architektur](architecture.md) — ETL-Muster, Layer-Verantwortlichkeiten und Datenfluss
- [ETL-Pipeline](etl-pipeline.md) — Pipeline-Builder, Step-Interfaces und Batch-Kontext
- [Markdig-Renderer](markdig-renderers.md) — Benutzerdefinierte Renderer und Erweiterung
- [Bauen & Testen](building-testing.md) — Build, Test, Coverage und Round-Trip-Tests
- [Mitwirken](contributing.md) — Code-Stil, Commit-Konventionen und PR-Prozess
