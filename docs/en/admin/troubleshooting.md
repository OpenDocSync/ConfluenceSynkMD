# Troubleshooting

Common issues and solutions when using ConfluentSynkMD.

---

## Authentication Errors

### `401 Unauthorized`

**Cause**: Invalid or expired credentials.

**Solutions**:

- Verify `CONFLUENCE__BASEURL` includes the correct domain
- Check that `CONFLUENCE__USEREMAIL` and `CONFLUENCE__APITOKEN` are correct
- Generate a new API token at [id.atlassian.com](https://id.atlassian.com/manage-profile/security/api-tokens)
- For Bearer auth, ensure the token hasn't expired

### `403 Forbidden`

**Cause**: The account lacks permissions for the target space.

**Solutions**:

- Verify the account has read/write access to the Confluence space
- Check space permissions in Confluence: **Space Settings → Permissions**

---

## Space and Page Errors

### `Space not found`

- Verify the space key (not the display name): `--conf-space DEV`
- Check for typos — space keys are case-sensitive

### `Page not found`

- Verify the page ID: `--conf-parent-id 12345`
- Ensure the page exists and is accessible by the authenticated user

---

## Diagram Rendering Issues

### `mmdc not found` / Mermaid fails

**Cause**: mermaid-cli is not installed or not on PATH.

**Solutions**:

```bash
# Install globally
npm install -g @mermaid-js/mermaid-cli

# Verify
mmdc --version
```

### Diagrams blank or error

- Check Node.js version (requires 22+)
- In Docker, ensure Chromium dependencies are installed (handled by the provided Dockerfile)
- Try `--diagram-output-format png` (more compatible than SVG)

---

## Upload Issues

### Pages not updating

- Remove `--skip-update` to force re-upload
- Check if content hashes match (same content = skip)
- Verify `--no-write-back` is not preventing page-ID tracking

### Duplicate pages created

- Ensure page-ID write-back is enabled (don't use `--no-write-back`)
- Check for `<!-- confluence-page-id: ... -->` comments in your Markdown files
- Use `--loglevel debug` to see collision detection output

---

## Download Issues

### Missing files or wrong structure

- Verify `--conf-parent-id` or `--root-page` points to the correct root
- Check that pages were originally uploaded with `--keep-hierarchy`
- Use `--loglevel debug` to see which pages are being fetched

---

## Logging

Increase log verbosity for debugging:

```bash
--loglevel debug
```

Log levels: `debug`, `info`, `warning`, `error`, `critical`

Logs are written to:

- **Console** — real-time output
- **File** — `logs/md2conf-{date}.log` (rolling daily)
