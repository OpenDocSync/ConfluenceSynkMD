# Admin-Handbuch

Willkommen im ConfluenceSynkMD Admin-Handbuch. Dieser Bereich deckt Installation, Deployment, Konfiguration und betriebliche Themen ab.

---

## Deployment-Optionen

| Option | Geeignet für | Voraussetzungen |
|---|---|---|
| **Lokal (.NET SDK)** | Entwicklung, Tests | .NET 10 SDK, Node.js 22+ |
| **Docker** | Produktion, CI/CD | Docker |
| **CI-Pipeline** | Automatisierte Sync | GitHub Actions |

---

## Bereiche

- [Installation](installation.md) — .NET SDK installieren und aus Quellcode bauen
- [Docker-Deployment](docker.md) — Mit Docker bauen und ausführen
- [Konfiguration & Umgebung](configuration.md) — Umgebungsvariablen und CLI-Credential-Flags
- [Authentifizierung](authentication.md) — Basic Auth vs. Bearer Auth
- [CI/CD-Integration](cicd.md) — Automatisierung mit GitHub Actions
- [Fehlerbehebung](troubleshooting.md) — Häufige Probleme und Lösungen
