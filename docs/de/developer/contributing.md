# Mitwirken

Vielen Dank für Ihr Interesse an ConfluenceSynkMD! Diese Anleitung hilft Ihnen beim Einstieg.

---

## Erste Schritte

1. **Forken** Sie das Repository auf GitHub
2. **Klonen** Sie Ihren Fork lokal:

    ```bash
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    cd ConfluenceSynkMD
    ```

3. **Branch erstellen**:

    ```bash
    git checkout -b feature/mein-feature
    ```

---

## Entwicklungsumgebung

### Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [@mermaid-js/mermaid-cli](https://www.npmjs.com/package/@mermaid-js/mermaid-cli) lokal im `PATH` (z. B. `npm install -g @mermaid-js/mermaid-cli`) für Mermaid-Rendering
- Optional: [Docker](https://www.docker.com/)

### Bauen & Testen

```bash
dotnet restore
dotnet build
dotnet test
```

---

## Code-Stil

- Dieses Projekt nutzt `.editorconfig` für konsistente Formatierung. Ihre IDE übernimmt die Regeln in der Regel automatisch.

- **PascalCase** für öffentliche Mitglieder
- **_camelCase** für private Felder
- `var` nur verwenden, wenn der Typ offensichtlich ist; sonst explizite Typen bevorzugen
- Klammern für Kontrollblöcke immer verwenden
- File-scoped Namespaces bevorzugen
- Block-Bodies statt Expression-Bodies bevorzugen
- Primary Constructors sind erlaubt
- Collection Expressions bevorzugen, wenn passend
- **Nullable Reference Types** sind projektweit aktiviert
- .NET Analyzer-Warnungen sauber halten

Formatierungsstandards aus `.editorconfig`:

- `*.cs`: UTF-8 BOM, CRLF, 4 Leerzeichen, abschließender Zeilenumbruch, keine nachgestellten Leerzeichen
- `*.md`, `*.yml`, `*.yaml`, `*.json`: UTF-8, LF, 2 Leerzeichen, abschließender Zeilenumbruch, keine nachgestellten Leerzeichen

---

## Änderungen vornehmen

1. **Tests schreiben** für jede neue Funktionalität
2. **Alle Tests bestehen lassen** vor dem Einreichen
3. **Commits fokussiert halten** — eine logische Änderung pro Commit
4. **Conventional Commits verwenden**:

    ```
    feat: Support für benutzerdefinierte Makros hinzufügen
    fix: Leeres Frontmatter korrekt behandeln
    docs: CLI-Referenztabelle aktualisieren
    ```

---

## Pull-Request-Prozess

1. Dokumentation aktualisieren bei Änderungen an CLI-Optionen oder Verhalten
2. PR-Template ausfüllen
3. CI muss erfolgreich durchlaufen
4. Review anfordern
5. Mindestens **ein genehmigendes Review** erforderlich

---

## Verhaltenskodex

Durch Ihre Teilnahme stimmen Sie unserem [Verhaltenskodex](https://github.com/OpenDocSync/ConfluenceSynkMD/blob/main/CODE_OF_CONDUCT.md) zu.
