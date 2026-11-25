# AI workflow

- Milestone 1. Codex inspected the empty workspace, initialized Git, and chose installed .NET 10 with EF Core/Npgsql. It modeled the three entities and configured a local PostgreSQL container with a persistent volume. The Debug build and Compose configuration validation passed. Docker CLI is installed, but the Linux engine was initially offline.
- Milestone 2. Codex implemented document endpoints, seeded identity, server-side access checks, JSON validation, shares and UTF-8 import. The API uses 404 for inaccessible documents and 403 when a collaborator attempts to manage shares.

- Milestone 3. Codex built the responsive dashboard and TipTap editor with manual save. The TypeScript and Vite production build passed.
- Milestone 4. Codex connected UTF-8 import, sharing and debounced autosave, and lazy-loaded TipTap. The initial build caught a file-picker handler type mismatch. After correcting it, the production build passed.
