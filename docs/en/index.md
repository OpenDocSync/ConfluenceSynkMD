# ConfluenceSynkMD

**Bidirectional Markdown ↔ Confluence synchronization for teams.**

ConfluenceSynkMD is a .NET 10 CLI tool that syncs Markdown documentation with Atlassian Confluence Cloud. Write in Markdown, publish to Confluence, download changes back — fully automated and round-trip safe.

---

## ✨ Highlights

- **Bidirectional Sync** — Upload Markdown → Confluence, Download Confluence → Markdown, or Local Export
- **Hierarchical Pages** — Preserves your directory structure as a parent–child page tree
- **Diagram Rendering** — Mermaid, Draw.io, PlantUML, and LaTeX → image attachments
- **Image Optimization** — Automatic downscaling and compression before upload
- **GitHub Alerts** — `[!NOTE]`, `[!WARNING]`, `[!TIP]` → Confluence macros
- **Frontmatter Support** — YAML frontmatter for titles, labels, space key overrides
- **Round-Trip Fidelity** — Page-ID write-back and source-path metadata for exact reconstruction

---

## 📖 Choose Your Path

<div class="grid cards" markdown>

-   :material-rocket-launch:{ .lg .middle } **Quick Start**

    ---

    Get up and running in under 5 minutes.

    [:octicons-arrow-right-24: Quick Start](quickstart.md)

-   :material-account:{ .lg .middle } **User Guide**

    ---

    Learn how to upload, download, and manage your docs.

    [:octicons-arrow-right-24: User Guide](user/index.md)

-   :material-code-braces:{ .lg .middle } **Developer Guide**

    ---

    Understand the architecture and contribute to the project.

    [:octicons-arrow-right-24: Developer Guide](developer/index.md)

-   :material-server:{ .lg .middle } **Admin Guide**

    ---

    Deploy, configure, and maintain ConfluenceSynkMD.

    [:octicons-arrow-right-24: Admin Guide](admin/index.md)

-   :material-book-open-variant:{ .lg .middle } **Reference**

    ---

    Complete CLI flags, environment variables, and schemas.

    [:octicons-arrow-right-24: Reference](reference/cli.md)

</div>

---

## License

ConfluenceSynkMD is licensed under the [MIT License](https://github.com/OpenDocSync/ConfluenceSynkMD/blob/main/LICENSE).
