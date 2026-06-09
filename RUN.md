# Coldrun ERP - Runbook

Last updated: 2026-05-27

## Purpose

This runbook explains how to run, test, and operate the current Coldrun implementation:

- API host (Minimal API, .NET 10)
- Trucks module (CRUD + filtering/sorting + status transitions)
- Operational endpoints (health, metrics, OpenAPI, Scalar)
- Seeder CLI (data seed + E2E scenario execution)

## Prerequisites

- .NET 10 SDK installed: https://dotnet.microsoft.com/download/dotnet/10.0
- Verify SDK:

```bash
dotnet --version
```

Expected output starts with `10.0`.

## Project Layout

```text
Coldrun/
|-- src/
|   |-- Coldrun/                    # API host
|   `-- Coldrun.Modules.Trucks/     # Truck module
|-- tests/
|   `-- Coldrun.Tests/              # Unit tests (xUnit)
|-- tools/
|   `-- Coldrun.Seeder/             # Seeder + E2E scenario runner
|-- docs/
|   |-- requirements.md
|   |-- analysis-phase2-plus-mvp-decisions.md
|   |-- current-implementation.md
|   `-- api-reference.md
`-- Coldrun.slnx
```

## Start The API

From the repository root:

```bash
dotnet run --project src/Coldrun/Coldrun.csproj
```

Default base URL: `http://localhost:5000`

### Runtime Endpoints

| Endpoint | Description |
|---|---|
| `GET /health/live` | Liveness health check |
| `GET /health/ready` | Readiness health check (currently in-memory store accessibility) |
| `GET /metrics` | In-process request metrics JSON |
| `GET /openapi/v1.json` | OpenAPI document |
| `GET /scalar/v1` | Scalar API UI |
| `GET/POST/PUT/DELETE /api/trucks` | Trucks API |

## Run Tests

Run unit tests:

```bash
dotnet test tests/Coldrun.Tests/Coldrun.Tests.csproj
```

Run with coverage collector:

```bash
dotnet test tests/Coldrun.Tests/Coldrun.Tests.csproj --collect:"XPlat Code Coverage"
```

## Seeder CLI

The seeder requires a running API and supports two modes:

- data seeding mode (`--file`)
- scenario mode (`--scenario`)

Exactly one of `--file` or `--scenario` must be provided.

### Seed Data Mode

```bash
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --file tools/Coldrun.Seeder/Data/sample-minimal.json
```

Seed after full reset:

```bash
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --file tools/Coldrun.Seeder/Data/sample-minimal.json --reset
```

### Scenario Mode (E2E-like Validation)

```bash
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --scenario tools/Coldrun.Seeder/Data/e2e-full-lifecycle.json --reset
```

### Base URL Resolution

The seeder resolves API URL in this order:

1. `COLDRUN_API_URL` environment variable (if set)
2. `--baseUrl` argument (if provided)
3. default `http://localhost:5000`

Example:

```bash
set COLDRUN_API_URL=http://localhost:5000
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --file tools/Coldrun.Seeder/Data/sample-all-statuses.json
```

### Available Seed Files

| File | Purpose |
|---|---|
| `tools/Coldrun.Seeder/Data/sample-minimal.json` | Small baseline data set |
| `tools/Coldrun.Seeder/Data/sample-all-statuses.json` | One or more trucks in all statuses |
| `tools/Coldrun.Seeder/Data/sample-edge-cases.json` | Boundary-style data cases |

### Available Scenario Files

| File | Purpose |
|---|---|
| `tools/Coldrun.Seeder/Data/e2e-self-transitions.json` | Self-transition policy behavior |
| `tools/Coldrun.Seeder/Data/e2e-full-lifecycle.json` | Full lifecycle transitions |
| `tools/Coldrun.Seeder/Data/e2e-invalid-transitions.json` | Invalid transition rejections |
| `tools/Coldrun.Seeder/Data/e2e-multi-truck.json` | Multi-truck execution paths |
| `tools/Coldrun.Seeder/Data/e2e-oos-escape.json` | Out-of-service escape paths |

## Operational Behavior Notes

- `X-Correlation-ID` is propagated/generated per request and returned in response headers.
- Global exception middleware converts known exception types to stable HTTP responses with JSON `{ "error": "..." }`.
- Truck endpoints are protected by a fixed-window rate limiter configured in `appsettings.json`.
- Metrics are in-memory and reset on process restart.

## CI Workflows

Current workflows under `.github/workflows/`:

- `ci.yml`: restore, build, test on push to `main` and on pull requests
- `pr-checks.yml`: validates PR title/body structure for non-draft PRs
- `manual-deploy.yml`: workflow-dispatch placeholder for manual deploy pipeline

## Documentation Map

- High-level implementation details: `docs/current-implementation.md`
- API contract and examples: `docs/api-reference.md`
- Requirements baseline: `docs/requirements.md`
- Architecture and phase analysis: `docs/analysis-phase2-plus-mvp-decisions.md`

## Quick Command Reference

```bash
# Run API
dotnet run --project src/Coldrun/Coldrun.csproj

# Run tests
dotnet test tests/Coldrun.Tests/Coldrun.Tests.csproj

# Seed baseline data
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --file tools/Coldrun.Seeder/Data/sample-minimal.json --reset

# Execute lifecycle scenario
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --scenario tools/Coldrun.Seeder/Data/e2e-full-lifecycle.json --reset
```
