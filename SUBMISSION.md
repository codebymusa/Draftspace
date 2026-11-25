# Ajaia assessment submission

**Project** DraftSpace

**Local demo** [127.0.0.1:8080](http://127.0.0.1:8080). Launch with `docker compose up --build -d` from this repository.

**Source** Six milestone commits on the local main branch. The demo was deployed locally through Docker Compose.

## Implemented

- Create, rename, edit, save and reopen documents with TipTap JSON and PostgreSQL.
- Format text with bold, italic, underline, headings, bullet lists and numbered lists.
- Import UTF-8 .txt files with type, size and encoding validation.
- Switch seeded demo users, distinguish owned and shared documents, and manage edit access as owner.
- See autosave status, validation errors, and loading and empty states.

## Verified

- Six Release xUnit tests passed against PostgreSQL.
- TypeScript, Vite, Docker image build, and Compose configuration validation passed.
- Headless Chrome at 390 px verified create, rename, bold, underline, heading, autosave, reload, sharing, outsider rejection, UTF-8 import, and absence of horizontal overflow.
- A shared document retained its owner, recipient access and heading after both Compose containers restarted.

The Vite build emits a warning about TipTap's use-client directive. Demo identity is simulated, and simultaneous editor sessions use last-write-wins saves. Public hosting and a recorded video were outside the local verification.
