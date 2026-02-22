# Security Policy

## Supported Versions

At the moment, only the latest state of the `main` branch is supported with
security fixes.

| Version | Supported |
| :--- | :---: |
| `main` | ✅ |
| older tags / branches | ❌ |

## Reporting a Vulnerability

Please do not open public issues for security vulnerabilities.

Use one of the following channels:

1. GitHub Security Advisories / private vulnerability report for this repository
2. Contact the maintainer via the private channel documented in repository metadata

When reporting, include:

- Affected version / commit
- Steps to reproduce
- Expected vs. observed behavior
- Potential impact
- Suggested fix (if available)

We will acknowledge reports as quickly as possible and provide status updates.

## Security Best Practices for Contributors

- Never commit credentials, tokens, API keys, or personal secrets
- Use `.env` locally and keep real secrets out of git
- Prefer placeholders in examples (for example `YOUR_SPACE_KEY`, `YOUR_PAGE_ID`)
- If you find accidentally committed credentials, report immediately using the channels above
