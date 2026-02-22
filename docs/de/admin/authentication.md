# Authentifizierung

ConfluentSynkMD unterstützt zwei Authentifizierungsmethoden für die Verbindung mit Confluence Cloud. Zugangsdaten können über **Umgebungsvariablen** oder **CLI-Flags** bereitgestellt werden.

---

## Basic Auth (E-Mail + API-Token)

Dies ist die Standard- und empfohlene Methode.

### Einrichtung

1. Gehen Sie zu [Atlassian API-Token-Verwaltung](https://id.atlassian.com/manage-profile/security/api-tokens)
2. Klicken Sie auf **API-Token erstellen**
3. Geben Sie einen beschreibenden Namen ein (z.B. „ConfluentSynkMD")
4. Kopieren Sie den Token

### Konfiguration

#### Umgebungsvariablen

=== "Bash"

    ```bash
    export CONFLUENCE__AUTHMODE=Basic
    export CONFLUENCE__USEREMAIL=ihre-email@example.com
    export CONFLUENCE__APITOKEN=ihr-api-token
    ```

=== "PowerShell"

    ```powershell
    $env:CONFLUENCE__AUTHMODE = "Basic"
    $env:CONFLUENCE__USEREMAIL = "ihre-email@example.com"
    $env:CONFLUENCE__APITOKEN = "ihr-api-token"
    ```

=== "CMD"

    ```cmd
    set CONFLUENCE__AUTHMODE=Basic
    set CONFLUENCE__USEREMAIL=ihre-email@example.com
    set CONFLUENCE__APITOKEN=ihr-api-token
    ```

#### CLI-Flags

=== "Bash"

    ```bash
    --conf-auth-mode Basic \
    --conf-user-email ihre-email@example.com \
    --conf-api-token ihr-api-token
    ```

=== "PowerShell"

    ```powershell
    --conf-auth-mode Basic `
    --conf-user-email ihre-email@example.com `
    --conf-api-token ihr-api-token
    ```

=== "CMD"

    ```cmd
    --conf-auth-mode Basic ^
    --conf-user-email ihre-email@example.com ^
    --conf-api-token ihr-api-token
    ```

#### Docker (-e)

=== "Bash"

    ```bash
    docker run --rm -it \
      -e CONFLUENCE__AUTHMODE=Basic \
      -e CONFLUENCE__USEREMAIL=ihre-email@example.com \
      -e CONFLUENCE__APITOKEN=ihr-api-token \
      ...
    ```

=== "PowerShell"

    ```powershell
    docker run --rm -it `
      -e CONFLUENCE__AUTHMODE=Basic `
      -e CONFLUENCE__USEREMAIL=ihre-email@example.com `
      -e CONFLUENCE__APITOKEN=ihr-api-token `
      ...
    ```

=== "CMD"

    ```cmd
    docker run --rm -it ^
      -e CONFLUENCE__AUTHMODE=Basic ^
      -e CONFLUENCE__USEREMAIL=ihre-email@example.com ^
      -e CONFLUENCE__APITOKEN=ihr-api-token ^
      ...
    ```

!!! tip
    API-Tokens erben die Berechtigungen des Atlassian-Accounts. Stellen Sie sicher, dass der Account **Lese-/Schreibzugriff** auf die Ziel-Spaces hat.

---

## Bearer Auth (OAuth 2.0)

Verwenden Sie OAuth 2.0 für automatisierte/Service-Account-Zugriffe.

### Einrichtung

1. OAuth 2.0 App erstellen unter [developer.atlassian.com](https://developer.atlassian.com/console/myapps/)
2. Erforderliche Scopes konfigurieren:
    - `read:confluence-content.all`
    - `write:confluence-content`
    - `read:confluence-space.summary`
3. OAuth 2.0 3LO-Flow durchführen

### Konfiguration

#### Umgebungsvariablen

=== "Bash"

    ```bash
    export CONFLUENCE__AUTHMODE=Bearer
    export CONFLUENCE__BEARERTOKEN=ihr-oauth2-bearer-token
    ```

=== "PowerShell"

    ```powershell
    $env:CONFLUENCE__AUTHMODE = "Bearer"
    $env:CONFLUENCE__BEARERTOKEN = "ihr-oauth2-bearer-token"
    ```

=== "CMD"

    ```cmd
    set CONFLUENCE__AUTHMODE=Bearer
    set CONFLUENCE__BEARERTOKEN=ihr-oauth2-bearer-token
    ```

#### CLI-Flags

=== "Bash"

    ```bash
    --conf-auth-mode Bearer \
    --conf-bearer-token ihr-oauth2-bearer-token
    ```

=== "PowerShell"

    ```powershell
    --conf-auth-mode Bearer `
    --conf-bearer-token ihr-oauth2-bearer-token
    ```

=== "CMD"

    ```cmd
    --conf-auth-mode Bearer ^
    --conf-bearer-token ihr-oauth2-bearer-token
    ```

#### Docker (-e)

=== "Bash"

    ```bash
    docker run --rm -it \
      -e CONFLUENCE__AUTHMODE=Bearer \
      -e CONFLUENCE__BEARERTOKEN=ihr-oauth2-bearer-token \
      ...
    ```

=== "PowerShell"

    ```powershell
    docker run --rm -it `
      -e CONFLUENCE__AUTHMODE=Bearer `
      -e CONFLUENCE__BEARERTOKEN=ihr-oauth2-bearer-token `
      ...
    ```

=== "CMD"

    ```cmd
    docker run --rm -it ^
      -e CONFLUENCE__AUTHMODE=Bearer ^
      -e CONFLUENCE__BEARERTOKEN=ihr-oauth2-bearer-token ^
      ...
    ```

!!! warning "Token-Ablauf"
    OAuth 2.0 Access Tokens laufen typischerweise nach 1 Stunde ab. Implementieren Sie Token-Refresh-Logik in Ihrer CI/CD-Pipeline.

---

## Auswahl der richtigen Methode

| Methode | Anwendungsfall | Vorteile | Nachteile |
|---|---|---|---|
| **Basic Auth** | Interaktive Nutzung, einfache CI | Einfach einzurichten, stabile Tokens | An persönlichen Account gebunden |
| **Bearer Auth** | Service-Accounts, automatisierte Pipelines | Feingranulare Scopes, kein persönlicher Account | Tokens laufen ab, komplexere Einrichtung |

---

## Sicherheitsempfehlungen

- :material-check: CI/CD-Geheimvariablen verwenden (nicht hartcodiert)
- :material-check: Zugangsdaten über Umgebungsvariablen oder CLI-Flags übergeben
- :material-check: API-Tokens regelmäßig rotieren
- :material-close: Tokens niemals in der Versionsverwaltung committen
- :material-close: Tokens niemals in Docker-Images einbetten
