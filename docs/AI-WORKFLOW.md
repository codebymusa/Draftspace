# AI workflow

- Milestone 1. Codex inspected the empty workspace, initialized Git, and chose installed .NET 10 with EF Core/Npgsql. It modeled the three entities and configured a local PostgreSQL container with a persistent volume. The Debug build and Compose configuration validation passed. Docker CLI is installed, but the Linux engine was initially offline.
- Milestone 2. Codex implemented document endpoints, seeded identity, server-side access checks, JSON validation, shares and UTF-8 import. The API uses 404 for inaccessible documents and 403 when a collaborator attempts to manage shares.

- Milestone 3. Codex built the responsive dashboard and TipTap editor with manual save. The TypeScript and Vite production build passed.
- Milestone 4. Codex connected UTF-8 import, sharing and debounced autosave, and lazy-loaded TipTap. The initial build caught a file-picker handler type mismatch. After correcting it, the production build passed.
- Milestone 5. An initial SQLite-based test run found a provider-specific sorting limitation and a transitive SQLite audit warning. With Docker available, Codex switched the suite to isolated PostgreSQL databases. All six Release tests passed. The production Docker image built and the Compose stack started.
- Milestone 6. Chrome exposed garbled glyphs, a missing save-before-back handler, and a demo-selector accessibility naming issue. Codex corrected the UI and reran a successful 390 px Chrome smoke test. After restarting both containers, the shared document retained its owner, recipient access and heading. The repository NuGet configuration isolates restores and audits from unrelated machine-level feeds.
