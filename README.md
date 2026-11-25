# DraftSpace

A lightweight collaborative document editor for the Ajaia AI-Native Full Stack Developer assessment. Write in TipTap, import UTF-8 plain text, and grant other demo users edit access.

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
