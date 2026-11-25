# DraftSpace architecture

React and TipTap communicate with one ASP.NET Core API. EF Core uses PostgreSQL for documents, TipTap JSON, seeded users and sharing grants. A named Docker volume persists data. The demo initializes an empty database at startup.

## Implementation plan

1. Scaffold the repository and database.
2. Build document endpoints with server-side access control.
3. Build the dashboard and rich text editor.
4. Connect sharing, file import and autosave.
5. Run tests, build release artifacts and verify Docker configuration.
6. Finish submission instructions and verify the demo flows.

