# DraftSpace

A lightweight collaborative document editor for the Ajaia AI-Native Full Stack Developer assessment. Write in TipTap, import UTF-8 plain text, and grant other demo users edit access.

## Live demo

**[Open DraftSpace](https://draftspace-app.onrender.com/)**

Hosted on Render using a Docker web service and managed PostgreSQL 17. The free web service may take a little time to wake after inactivity. The current free database expires on December 25, 2026, unless upgraded.

No login or password is required. Select one of the three seeded demo users: Alex Morgan (owner for the sample flow), Jordan Lee (collaborator), or Sam Rivera (unshared user).

### Quick reviewer walkthrough

1. Select Alex Morgan, create a document, rename it, and try bold, italic, underline, headings and lists.
2. Wait for **Saved**, then refresh to check persistence.
3. Open **Share**, grant Jordan Lee access, return to Documents, and switch to Jordan. Open the document under **Shared with Me** and edit it.
4. Switch to Sam Rivera and confirm the document is not listed or accessible under that identity.
5. Import a UTF-8 `.txt` file (1 byte–256 KB) and edit the imported content.

This is a public assessment demo with simulated identities, not production authentication. Do not enter confidential information. Simultaneous editing uses last-write-wins rather than real-time co-editing.

## Technology stack

- React, TypeScript, Vite, Tailwind CSS and TipTap
- ASP.NET Core 10, EF Core and Npgsql
- PostgreSQL 17 with JSONB document content
- xUnit integration tests and Docker Compose

## Run with Docker

From the repository root, with Docker Desktop or Docker Engine and Compose running:

```sh
docker compose up --build -d
```

Open [DraftSpace](http://127.0.0.1:8080). The API initializes a new PostgreSQL database automatically. The named volume keeps documents and shares across restarts and normal Compose shutdowns. Stop the stack with `docker compose down`.

The demo user selector switches between Alex Morgan, Jordan Lee and Sam Rivera. The API trusts a caller-controlled `X-Demo-User` header for simulated authentication. Anyone with network access can select or impersonate any demo user. The default Compose ports bind to loopback for local evaluation.

## Develop locally

Requires .NET 10, Node.js 24 or compatible Node and Docker for PostgreSQL.

```sh
docker compose up -d --wait db
dotnet run --project server --urls http://localhost:5050
```

In another terminal:

```sh
cd client
npm ci
npm run dev
```

Vite forwards `/api` requests to port 5050. The API uses the demo database credentials by default. Override them with `ConnectionStrings__DraftSpace`.

## Verify

```sh
docker compose up -d --wait db
dotnet test tests -c Release
docker compose config --quiet
cd client
npm ci
npm run build
```

Tests use isolated PostgreSQL databases, create and drop them automatically, and require a role with database creation privileges. To use a different test instance, set `DRAFTSPACE_TEST_CONNECTION` with host and credentials.

See [architecture](docs/ARCHITECTURE.md), [AI workflow](docs/AI-WORKFLOW.md), [submission details](SUBMISSION.md), and the [demo walkthrough](WALKTHROUGH.txt).
