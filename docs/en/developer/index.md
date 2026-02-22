# Developer Guide

Welcome to the ConfluentSynkMD Developer Guide. This section is for contributors and developers who want to understand the codebase, extend functionality, or fix bugs.

---

## Tech Stack

| Component | Technology |
|---|---|
| Runtime | .NET 10 |
| CLI Framework | `System.CommandLine` |
| Markdown Parser | Markdig |
| HTML Parser | AngleSharp (for XHTML → Markdown) |
| DI Container | `Microsoft.Extensions.DependencyInjection` |
| Logging | Serilog |
| Testing | xUnit + Moq |
| Diagrams | mermaid-cli (Node.js) |
| Container | Docker (multi-stage build) |

---

## Repository Structure

```
ConfluentSynkMD/
├── src/ConfluentSynkMD/           # Main application
│   ├── Configuration/             # Settings records
│   ├── ETL/                       # Extract-Transform-Load pipeline
│   │   ├── Core/                  # Pipeline runner, step interfaces
│   │   ├── Extract/               # Markdown & Confluence ingestion
│   │   ├── Transform/             # MD→XHTML and XHTML→MD conversion
│   │   └── Load/                  # Upload, download, local export
│   ├── Markdig/                   # Custom Markdig renderers
│   ├── Models/                    # Domain models
│   └── Services/                  # API client, resolvers, renderers
├── tests/ConfluentSynkMD.Tests/   # Unit & integration tests
├── docs/                          # MkDocs documentation (this site)
├── Dockerfile                     # Multi-stage Docker build
└── .env.example                   # Env var template
```

---

## Getting Started

```bash
# Clone and build
git clone https://github.com/OpenDocSync/ConfluentSynkMD.git
cd ConfluentSynkMD
dotnet restore
dotnet build

# Run tests
dotnet test
```

---

## Dive Deeper

- [Architecture](architecture.md) — ETL pattern, layer responsibilities, and data flow
- [ETL Pipeline](etl-pipeline.md) — Pipeline builder, step interfaces, and batch context
- [Markdig Renderers](markdig-renderers.md) — Custom renderers and how to add new ones
- [Building & Testing](building-testing.md) — Build, test, coverage, and round-trip testing
- [Contributing](contributing.md) — Code style, commit conventions, and PR process
