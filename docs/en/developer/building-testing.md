# Building & Testing

This page covers how to build, test, and measure code coverage for ConfluentSynkMD.

---

## Build

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Build in Release mode (used by CI default)
dotnet build --configuration Release -p:TreatWarningsAsErrors=false

# Optional strict quality gate
dotnet build --configuration Release -p:TreatWarningsAsErrors=true
```

---

## Run Tests

```bash
# Run all tests
dotnet test

# Verbose output
dotnet test --verbosity normal

# Run a specific test class
dotnet test --filter "FullyQualifiedName~MarkdownIngestionStepTests"
```

---

## Code Coverage

Generate coverage reports with Coverlet:

```bash
# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generate HTML report (requires reportgenerator tool)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:TestResults/**/coverage.cobertura.xml \
  -targetdir:TestResults/html \
  -reporttypes:Html
```

Open `TestResults/html/index.html` to view the coverage report.

---

## Round-Trip Sync Test

The project includes a Docker-based end-to-end test that validates round-trip sync fidelity:

```bash
# Build and run the sync test
docker-compose -f docker-compose.sync-test.yml build
docker-compose -f docker-compose.sync-test.yml up
```

### What It Tests

| Check | Criteria |
|---|---|
| File structure | Exact file count and directory path equality |
| Heading structure | 100% heading match across all documents |
| Content similarity | Normalized content comparison (≥ 80% threshold) |
| Attachments | Per-document image reference verification |

### Configuration

| Variable | Default | Description |
|---|---|---|
| `SYNC_TEST_SPACE` | `$CONFLUENCE_SPACE` | Confluence space key |
| `SYNC_TEST_ROOT_PAGE` | `Sync-Test` | Root page title |
| `SYNC_TEST_SOURCE_DIR` | `mkdocs-example/docs` | Source docs directory |
| `SYNC_TEST_STRICT_STRUCTURE` | `false` | Fail on structural deviations |

---

## CI Pipeline

The GitHub Actions CI pipeline runs on every push to `main` and on pull requests:

```yaml
# .github/workflows/ci.yml
- dotnet restore
- dotnet build --configuration Release -p:TreatWarningsAsErrors=${{ env.TREAT_WARNINGS_AS_ERRORS }}
- dotnet test --collect:"XPlat Code Coverage"
```

See the [CI/CD Integration](../admin/cicd.md) page for more details.
