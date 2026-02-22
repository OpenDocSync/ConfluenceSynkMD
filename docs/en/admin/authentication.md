# Authentication

ConfluentSynkMD supports two authentication methods for connecting to Confluence Cloud. Credentials can be provided via **environment variables** or **CLI flags**.

---

## Basic Auth (Email + API Token)

This is the default and recommended method for most users.

### Setup

1. Go to [Atlassian API Token Management](https://id.atlassian.com/manage-profile/security/api-tokens)
2. Click **Create API Token**
3. Give it a descriptive name (e.g. "ConfluentSynkMD")
4. Copy the token

### Configuration

#### Environment Variables

=== "Bash"

    ```bash
    export CONFLUENCE__AUTHMODE=Basic
    export CONFLUENCE__USEREMAIL=your-email@example.com
    export CONFLUENCE__APITOKEN=your-api-token-here
    ```

=== "PowerShell"

    ```powershell
    $env:CONFLUENCE__AUTHMODE = "Basic"
    $env:CONFLUENCE__USEREMAIL = "your-email@example.com"
    $env:CONFLUENCE__APITOKEN = "your-api-token-here"
    ```

=== "CMD"

    ```cmd
    set CONFLUENCE__AUTHMODE=Basic
    set CONFLUENCE__USEREMAIL=your-email@example.com
    set CONFLUENCE__APITOKEN=your-api-token-here
    ```

#### CLI Flags

=== "Bash"

    ```bash
    --conf-auth-mode Basic \
    --conf-user-email your-email@example.com \
    --conf-api-token your-api-token-here
    ```

=== "PowerShell"

    ```powershell
    --conf-auth-mode Basic `
    --conf-user-email your-email@example.com `
    --conf-api-token your-api-token-here
    ```

=== "CMD"

    ```cmd
    --conf-auth-mode Basic ^
    --conf-user-email your-email@example.com ^
    --conf-api-token your-api-token-here
    ```

#### Docker (-e)

=== "Bash"

    ```bash
    docker run --rm -it \
      -e CONFLUENCE__AUTHMODE=Basic \
      -e CONFLUENCE__USEREMAIL=your-email@example.com \
      -e CONFLUENCE__APITOKEN=your-api-token-here \
      ...
    ```

=== "PowerShell"

    ```powershell
    docker run --rm -it `
      -e CONFLUENCE__AUTHMODE=Basic `
      -e CONFLUENCE__USEREMAIL=your-email@example.com `
      -e CONFLUENCE__APITOKEN=your-api-token-here `
      ...
    ```

=== "CMD"

    ```cmd
    docker run --rm -it ^
      -e CONFLUENCE__AUTHMODE=Basic ^
      -e CONFLUENCE__USEREMAIL=your-email@example.com ^
      -e CONFLUENCE__APITOKEN=your-api-token-here ^
      ...
    ```

!!! tip
    API tokens inherit the permissions of the Atlassian account that created them. Ensure the account has **read/write** access to the target spaces.

---

## Bearer Auth (OAuth 2.0)

Use OAuth 2.0 for automated/service account access or when API tokens are not available.

### Setup

1. Create an OAuth 2.0 app at [developer.atlassian.com](https://developer.atlassian.com/console/myapps/)
2. Configure the required scopes:
    - `read:confluence-content.all`
    - `write:confluence-content`
    - `read:confluence-space.summary`
3. Complete the OAuth 2.0 3LO flow to obtain an access token

### Configuration

#### Environment Variables

=== "Bash"

    ```bash
    export CONFLUENCE__AUTHMODE=Bearer
    export CONFLUENCE__BEARERTOKEN=your-oauth2-bearer-token-here
    ```

=== "PowerShell"

    ```powershell
    $env:CONFLUENCE__AUTHMODE = "Bearer"
    $env:CONFLUENCE__BEARERTOKEN = "your-oauth2-bearer-token-here"
    ```

=== "CMD"

    ```cmd
    set CONFLUENCE__AUTHMODE=Bearer
    set CONFLUENCE__BEARERTOKEN=your-oauth2-bearer-token-here
    ```

#### CLI Flags

=== "Bash"

    ```bash
    --conf-auth-mode Bearer \
    --conf-bearer-token your-oauth2-bearer-token-here
    ```

=== "PowerShell"

    ```powershell
    --conf-auth-mode Bearer `
    --conf-bearer-token your-oauth2-bearer-token-here
    ```

=== "CMD"

    ```cmd
    --conf-auth-mode Bearer ^
    --conf-bearer-token your-oauth2-bearer-token-here
    ```

#### Docker (-e)

=== "Bash"

    ```bash
    docker run --rm -it \
      -e CONFLUENCE__AUTHMODE=Bearer \
      -e CONFLUENCE__BEARERTOKEN=your-oauth2-bearer-token-here \
      ...
    ```

=== "PowerShell"

    ```powershell
    docker run --rm -it `
      -e CONFLUENCE__AUTHMODE=Bearer `
      -e CONFLUENCE__BEARERTOKEN=your-oauth2-bearer-token-here `
      ...
    ```

=== "CMD"

    ```cmd
    docker run --rm -it ^
      -e CONFLUENCE__AUTHMODE=Bearer ^
      -e CONFLUENCE__BEARERTOKEN=your-oauth2-bearer-token-here ^
      ...
    ```

!!! warning "Token Expiration"
    OAuth 2.0 access tokens typically expire after 1 hour. For long-running or automated processes, implement token refresh logic in your CI/CD pipeline.

---

## Choosing the Right Method

| Method | Use Case | Pros | Cons |
|---|---|---|---|
| **Basic Auth** | Interactive use, simple CI | Easy to set up, stable tokens | Tied to personal account |
| **Bearer Auth** | Service accounts, automated pipelines | Fine-grained scopes, no personal account | Tokens expire, more complex setup |

---

## Security Best Practices

- :material-check: Use CI/CD secret variables (not hardcoded)
- :material-check: Pass credentials via environment variables or CLI flags
- :material-check: Rotate API tokens regularly
- :material-close: Never commit tokens to version control
- :material-close: Never embed tokens in Docker images
