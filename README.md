# Campus Equipment Borrowing — Laboratory Activity 5

Avalonia staff desktop → asynchronous HTTP → ASP.NET Core REST API → Application workflows → EF Core/PostgreSQL. Lab 5 continues the Lab 4 use cases UC-01–09 and UC-12–16 and business rules BR-01–19. No authentication, web client, Docker or cloud deployment is required.

## Requirements

- .NET 10 SDK and NuGet access. The solution pins Avalonia 12.1.2, CommunityToolkit.Mvvm 8.4.2, EF Core 10.0.11 and Npgsql provider 10.0.3.
- Local PostgreSQL (verification uses PostgreSQL 18).
- Git and optionally pgAdmin/psql. In Visual Studio, start **EquipmentBorrowing.Api** and **EquipmentBorrowing.Desktop** as separate processes. Domain is a library.

## PostgreSQL setup and migrations

Create a local application login and database using pgAdmin or a PostgreSQL administrator:

```sql
CREATE ROLE equipment_app LOGIN;
-- Set a private password in pgAdmin, or interactively in psql: \password equipment_app
CREATE DATABASE equipment_borrowing OWNER equipment_app;
```

In PowerShell at the repository root:

```powershell
$env:EQUIPMENT_BORROWING_DB = 'Host=localhost;Port=5432;Database=equipment_borrowing;Username=equipment_app'
dotnet restore EquipmentBorrowing.sln
dotnet tool restore
dotnet ef database update --project src/EquipmentBorrowing.Infrastructure
dotnet build EquipmentBorrowing.sln
```

Use your private PostgreSQL password file (`%APPDATA%/postgresql/pgpass.conf` on Windows) or set the password privately in the environment. Never commit credentials. `EQUIPMENT_BORROWING_DB` is required by the API and EF design-time factory; it is **not** read by Desktop. The older Lab 4 README named a different variable; the current source uses the name above.

The existing `20261118144542_PostgreSqlInitial` migration is unchanged. It creates the schema, constraints and original demonstration data once; application startup never resets or migrates data. Lab 5 changes the transport boundary and needs no schema change. Existing identities, relationships and active/returned history remain intact. Do not reset an existing application database to run acceptance tests.

## Start API and desktop separately

Terminal 1, with the database environment configured:

```powershell
dotnet run --project src/EquipmentBorrowing.Api -- --urls http://127.0.0.1:5080
```

The API works without Avalonia. For example, in another terminal:

```powershell
Invoke-RestMethod http://127.0.0.1:5080/api/v1/equipment
Invoke-RestMethod http://127.0.0.1:5080/api/v1/borrowings?status=Active
Invoke-RestMethod http://127.0.0.1:5080/openapi/v1.json
```

Terminal 2:

```powershell
$env:EQUIPMENTBORROWING_API_URL = 'http://127.0.0.1:5080/'
dotnet run --project src/EquipmentBorrowing.Desktop
```

`EQUIPMENTBORROWING_API_URL` is the server root, without `/api/v1`; its default is the URL above. Launch Visual Studio from a configured shell if it should inherit session variables. Desktop needs only the API URL, and has no Infrastructure, EF Core or PostgreSQL dependency.

## Contract and staff workflows

[Endpoint contract](docs/lab5/api-contract.md) describes all API-01–16 routes, request fields and response/error behavior. The committed [OpenAPI 3.1 description](docs/lab5/openapi.json) is served unchanged at `/openapi/v1.json`. JSON uses camelCase, stable internal IDs and `YYYY-MM-DD` dates in Asia/Manila. Lists return arrays, including `[]`. Borrowing responses include related student/equipment display records.

- **Equipment & Checkout:** locate and edit descriptive fields; filter availability; select a student, item and future due date; or add two or three different items to checkout together.
- **Students & History:** register/edit students, permit/restrict new borrowing, and load the selected student's active/returned history.
- **Borrowings & Returns:** initially show active loans; search student name/number or equipment name; combine status and date sorting; return an active loan. Clear criteria restores All / borrowedDate / ascending, matching the API default.
- **Refresh / Reconnect:** retrieve current API state after independent changes or recovery. Errors differ from empty lists. No HTTP response means a connection failure; HTTP 500 means a reachable server reported a failure.

The server revalidates eligibility, availability, identities, dates and the maximum of three active items per student. Multi-item checkout commits all loans and availability changes together. Returned history remains stored. Unknown/protected input fields are rejected. Creates may omit optional fields; PUT sends all editable fields, with null clearing contact/description.

Mutations are never automatically retried. If commit acknowledgement or a mutation response is lost, the UI reports an unknown outcome and blocks resubmission until a successful refresh. Inspect the refreshed records before deciding to retry.

## Repeatable acceptance execution and disposable data

The console runner executes the supplied T01–T08, using real Kestrel HTTP, PostgreSQL and compiled Avalonia XAML. It is invoked with `dotnet run`, **not** `dotnet test`. Configure a local test administrator with CREATEDB. The runner creates a unique `equipment_lab5_test_<32 hex digits>` database, applies the real migration and resets **only that generated database** to S1–S4/E1–E7 before each test/boundary scenario. It never drops or resets an existing database. IDs are resolved from API responses.

```powershell
$env:EQUIPMENTBORROWING_TEST_CONNECTION = 'Host=localhost;Port=5432;Database=postgres;Username=your_test_admin'
New-Item -ItemType Directory -Force artifacts | Out-Null
$env:EQUIPMENTBORROWING_UI_CAPTURE = "$PWD/artifacts/ui-history.png"
dotnet run --project tests/EquipmentBorrowing.Tests -- --lab5
# In a NEW process, substitute the exact generated database name printed by the run:
dotnet run --project tests/EquipmentBorrowing.Tests -- --lab5 --readback equipment_lab5_test_<guid>
```

Use private password configuration as above. The runner records its actual Asia/Manila date and uses the reference's allowed relative-date equivalent: E1 is due yesterday; other loans are due in two days. Seed checkout dates are nine/eight/seven days earlier; E3 was returned two days earlier. Future mutation dates follow that same server date.

Every mutation logs a before/after snapshot through an independent Npgsql connection. Controlled faults throw after SaveChanges but before commit (known rollback), or after commit before acknowledgement (unknown outcome, resolved by fresh read). These seams exist only in the test project; the deployed API has no fault/reset routes. T08 uses an invalid connection on a separate host rather than stopping the existing PostgreSQL service. Test databases are retained for inspection; remove only the generated disposable database yourself when finished.

The historical Lab 4 runner is still available with no arguments, or `--readback equipment_lab4_test_<guid>`. Its UI checks now cross HTTP as well.

## Submission records and evidence

- [Architecture and boundary changes](docs/lab5/architecture.md)
- [Implementation baseline and final revision](docs/lab5/baseline.md)
- [Completed cross-laboratory traceability matrix](docs/lab5/traceability.md)
- [Consolidated T01–T08 results, failures, corrections and retests](docs/lab5/test-results.md)
- [Original Lab 4 business rules](docs/business-rules.md), [fourteen use cases](docs/use-case-suite.md), [database diagram](docs/database-diagram.md) and [historical verification](docs/verification.md)

Resolve the completed revision with `git rev-parse 'lab5-submitted^{commit}'`. Earlier commits and Lab 4 evidence are preserved. No historical `lab4-submitted` tag is invented without verification of the actual submission. Earlier Labs 1–3 are retained in [historical documentation](docs/labs-1-to-3.md).

Submit your authentic full name and student ID in the private submission comment. A confirmed pair must identify both partners, and each member submits individually. These personal details are not invented or published in this repository.
