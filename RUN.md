# Coldrun ERP — Running the Projects

## Prerequisites

- **.NET 10 SDK** — install from https://dotnet.microsoft.com/download/dotnet/10.0
- Verify: `dotnet --version` should output `10.0.x`

---

## Project Structure

```
Coldrun/
├── src/
│   ├── Coldrun/                      # Main API host (Minimal APIs)
│   └── Coldrun.Modules.Trucks/       # Truck module (domain, endpoints, services)
├── tests/
│   └── Coldrun.Tests/                # xUnit unit tests
├── tools/
│   └── Coldrun.Seeder/               # Data seeding & E2E scenario runner (CLI)
└── Coldrun.slnx                      # Solution file
```

---

## 1. Running the API

From the `Coldrun/` directory:

```bash
dotnet run --project src/Coldrun/Coldrun.csproj
```

The API starts on **http://localhost:5000** by default.

### Endpoints

| Endpoint | Description |
|----------|-------------|
| `http://localhost:5000/health/live` | Liveness health check |
| `http://localhost:5000/health/ready` | Readiness health check |
| `http://localhost:5000/metrics` | Request metrics (JSON) |
| `http://localhost:5000/api/trucks` | Truck CRUD (POST, GET, PUT, DELETE) |
| `http://localhost:5000/openapi/v1.json` | OpenAPI spec |
| `http://localhost:5000/scalar/v1` | Scalar API documentation UI |

---

## 2. Running Unit Tests

From the `Coldrun/` directory:

```bash
dotnet test tests/Coldrun.Tests/Coldrun.Tests.csproj
```

With coverage:

```bash
dotnet test tests/Coldrun.Tests/Coldrun.Tests.csproj --collect:"XPlat Code Coverage"
```

---

## 3. Running the Data Seeder

The seeder populates the API with truck data from JSON files. The API must be running first.

### Basic Usage

```bash
# Seed from a JSON file
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --file tools/Coldrun.Seeder/Data/sample-minimal.json

# Reset (delete all trucks) before seeding
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --file tools/Coldrun.Seeder/Data/sample-minimal.json --reset
```

### Custom Base URL

```bash
# Via CLI argument
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --file tools/Coldrun.Seeder/Data/sample-minimal.json --baseUrl http://localhost:5000

# Via environment variable
set COLDRUN_API_URL=http://localhost:5000
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --file tools/Coldrun.Seeder/Data/sample-minimal.json
```

### Available Seed Data Files

| File | Description |
|------|-------------|
| `Data/sample-minimal.json` | Minimal truck data (2 trucks) |
| `Data/sample-all-statuses.json` | Trucks in all possible statuses |
| `Data/sample-edge-cases.json` | Edge case truck configurations |

---

## 4. Running E2E Scenario Tests

The seeder doubles as an E2E scenario runner. Scenarios define ordered sequences of truck creation and status transitions with validation.

### Run All Scenarios from a File

```bash
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --scenario tools/Coldrun.Seeder/Data/e2e-self-transitions.json
```

### Run with Reset

```bash
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --scenario tools/Coldrun.Seeder/Data/e2e-full-lifecycle.json --reset
```

### Available E2E Scenario Files

| File | Description |
|------|-------------|
| `Data/e2e-self-transitions.json` | FSM self-transition validation |
| `Data/e2e-full-lifecycle.json` | Complete truck lifecycle scenarios |
| `Data/e2e-invalid-transitions.json` | Invalid transition rejection tests |
| `Data/e2e-multi-truck.json` | Multi-truck parallel scenarios |
| `Data/e2e-oos-escape.json` | Out-of-service escape scenarios |

### Scenario Output

Each scenario reports:
- **PASS/FAIL** per step
- **PASS/FAIL** per scenario
- Summary of passed vs. failed steps

---

## 5. CI Pipeline

The GitHub Actions CI pipeline (`.github/workflows/ci.yml`) runs on every push and PR:

1. `dotnet restore` — restore dependencies
2. `dotnet build --configuration Release` — build all projects
3. `dotnet test --configuration Release` — run unit tests

---

## Quick Reference

```bash
# Start API
dotnet run --project src/Coldrun/Coldrun.csproj

# Run tests (separate terminal)
dotnet test tests/Coldrun.Tests/Coldrun.Tests.csproj

# Seed data (API must be running)
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --file tools/Coldrun.Seeder/Data/sample-minimal.json --reset

# Run E2E scenarios (API must be running)
dotnet run --project tools/Coldrun.Seeder/Coldrun.Seeder.csproj -- --scenario tools/Coldrun.Seeder/Data/e2e-full-lifecycle.json --reset
```
