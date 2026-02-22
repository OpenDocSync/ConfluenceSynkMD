# Contributing

Thank you for your interest in contributing to ConfluentSynkMD! This guide will help you get started.

---

## Getting Started

1. **Fork** the repository on GitHub
2. **Clone** your fork locally:

    ```bash
    git clone https://github.com/YOUR_USERNAME/ConfluentSynkMD.git
    cd ConfluentSynkMD
    ```

3. **Create a branch** for your change:

    ```bash
    git checkout -b feature/my-feature
    ```

---

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/) — for Mermaid diagram rendering
- Optional: [Docker](https://www.docker.com/) — for containerized testing

### Build & Test

```bash
dotnet restore
dotnet build
dotnet test
```

---

## Code Style

This project uses `.editorconfig` for consistent formatting. Your IDE should pick up the rules automatically.

- **PascalCase** for public members
- **_camelCase** for private fields
- Use `var` only when the type is obvious; prefer explicit types otherwise
- Always use braces for control blocks
- Prefer file-scoped namespaces
- Prefer block-bodied members over expression-bodied members
- Primary constructors are allowed
- Prefer collection expressions where applicable
- **Nullable reference types** are enabled project-wide
- Keep .NET Analyzer warnings clean — CI treats warnings as errors

Formatting defaults from `.editorconfig`:

- `*.cs`: UTF-8 BOM, CRLF, 4 spaces, final newline, trim trailing whitespace
- `*.md`, `*.yml`, `*.yaml`, `*.json`: UTF-8, LF, 2 spaces, final newline, trim trailing whitespace

---

## Making Changes

1. **Write tests** for any new functionality or bug fixes
2. **Ensure all tests pass** before submitting
3. **Keep commits focused** — one logical change per commit
4. **Use Conventional Commits**:

    ```
    feat: add support for custom macros
    fix: handle empty frontmatter gracefully
    docs: update CLI reference table
    refactor: extract link resolution into service
    ```

---

## Pull Request Process

1. Update documentation if your change affects CLI options, configuration, or behavior
2. Fill out the PR template with description, related issues, and testing details
3. Ensure CI passes — build, tests, and code analysis
4. Request a review — a maintainer will review your PR
5. PRs require at least **one approving review** before merging

---

## Reporting Bugs

Use [GitHub Issues](https://github.com/YOUR_USERNAME/ConfluentSynkMD/issues) with the **Bug Report** template. Include:

- Steps to reproduce
- Expected vs. actual behavior
- OS, .NET version, ConfluentSynkMD version

---

## Suggesting Features

Open an issue using the **Feature Request** template. Describe:

- The problem you're trying to solve
- Your proposed solution
- Alternatives you've considered

---

## Code of Conduct

By participating, you agree to abide by our [Code of Conduct](https://github.com/YOUR_USERNAME/ConfluentSynkMD/blob/main/CODE_OF_CONDUCT.md).

## Questions?

See [SUPPORT.md](https://github.com/YOUR_USERNAME/ConfluentSynkMD/blob/main/SUPPORT.md) for ways to get help.
