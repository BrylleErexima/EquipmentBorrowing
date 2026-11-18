# Campus Equipment Borrowing - Laboratory Activity 4

Avalonia desktop application for **Laboratory Staff / Equipment Custodians**, continuing Laboratories 1-3. Implements UC-01-09 and UC-12-16 with PostgreSQL persistence. [Earlier laboratory documentation](docs/labs-1-to-3.md) is retained as historical context; the PostgreSQL instructions below supersede its SQLite setup.

## Prerequisites

- .NET **10 SDK** and a compatible Visual Studio installation (Avalonia extension optional).
- A local **PostgreSQL 18** server, or another PostgreSQL version supporting generated columns and partial indexes. Verification used PostgreSQL 18.
- Git; `psql`/pgAdmin for database setup. No Docker, cloud account or paid service is needed.
- NuGet access for restore. The solution pins Avalonia 12.1.2, CommunityToolkit.Mvvm 8.4.2, Npgsql provider 10.0.3 and EF Core 10.0.11.

## Local setup and execution

1. Clone this repository and open `EquipmentBorrowing.sln`.
2. Create an empty database and a local login in pgAdmin, or use `psql` as your local PostgreSQL administrator:

   ```sql
   CREATE ROLE equipment_app LOGIN;
   -- In psql, set its password interactively: \password equipment_app
   CREATE DATABASE equipment_borrowing OWNER equipment_app;
   ```

3. Configure the connection **outside source control**. In PowerShell, set this for the current session, substituting your local login details:

   ```powershell
   $env:EQUIPMENTBORROWING_CONNECTION = 'Host=localhost;Port=5432;Database=equipment_borrowing;Username=equipment_app'
   ```

   Use PostgreSQL's per-user `pgpass.conf` for the password (`%APPDATA%/postgresql/pgpass.conf` on Windows), or supply `Password=...` through your private environment configuration. Never commit a real connection string containing credentials. The fallback connection contains no password and targets `localhost / equipment_borrowing / postgres`.

4. Restore, apply the committed migration, and build:

   ```powershell
   dotnet restore
   dotnet tool restore
   dotnet ef database update --project src/EquipmentBorrowing.Infrastructure
   dotnet build EquipmentBorrowing.sln
   ```

   Migrations run explicitly with the Infrastructure design-time factory. Views and ViewModels never execute migrations. This is a **fresh PostgreSQL schema**; previous SQLite migrations remain inspectable in Git history. SQLite row transfer and simultaneous SQLite operation are outside the supplied Lab 4 scope. Existing `.db` files are not used by the application.

5. Run the desktop application:

   ```powershell
   dotnet run --project src/EquipmentBorrowing.Desktop
   ```

   In Visual Studio, select **EquipmentBorrowing.Desktop** as the startup project. If using a session environment variable, launch Visual Studio from that configured shell so it inherits the variable. **Domain is a class library and cannot launch the application.**

The initial migration inserts three fictional demonstration students (one restricted) and five available equipment items. Database-generated identity values are used. Seed insertion runs once through migration history, never as a startup reset; reapplying the migration preserves edits and loans. No personal data or credentials are supplied.

## Staff workflows

- **Equipment & Checkout:** locate by identifier/name, toggle available items, select an item to view its full active/returned history, register or edit descriptive fields. Select a student and a future expected return date to borrow one catalog item. For UC-06, add two or three different catalog items to the checkout list and submit them together. Duplicate selections receive explicit feedback.
- **Students & History:** locate by name/institutional number, select to load related history, register a new student, edit information, permit or restrict borrowing. `New student` clears the edit selection; saving a selected student updates that same internal identity. Blank optional contact clears it.
- **Borrowings & Returns:** initially shows active records. Search by student name, institutional number or equipment name; combine status and date sorting, choose direction, or clear criteria to show all. Select a record and press **Confirm return**. History is retained. Repeating a return is rejected.
- **Refresh / Reconnect** reloads current PostgreSQL state. Loading indicators keep asynchronous work observable. Empty result counts/messages differ from database errors. Histories replace their results on selection; an older request cannot overwrite a newer selection.

All checkout and return calendar dates use **Asia/Manila**, stored as PostgreSQL `date` / .NET `DateOnly`. A due date must be later than checkout. An early return is permitted. At most **three active items** are allowed per student, including items already on loan.

## Architecture and integrity decisions

```text
Avalonia XAML View -> CommunityToolkit ViewModel -> WorkflowService (Application)
    -> IWorkflowRepository / IWorkflowSession -> PostgreSQL EF Core repository -> PostgreSQL
```

Domain entities retain stable identities and explicit update/return transitions. The Application service owns eligibility, field validation, borrowing limits, dates, and allowed operations. Infrastructure owns SQL, mappings, EF migrations, queries, and transaction mechanics. The composition root (`App.axaml.cs`) registers the factory, repository, service and ViewModel. No View or ViewModel references DbContext, SQL, connection configuration or migrations.

`IDbContextFactory` creates and disposes **one context per operation**. Read operations use no tracking. A write gets one context and one transaction spanning current-state reads, business validation, all borrowing/availability changes, SaveChanges and commit. Singleton services retain only the thread-safe factory/repository, never a shared DbContext. Independent history loads can therefore overlap without concurrently using a context. The UI guards mutation commands while busy and generation-checks asynchronous master-detail results.

For this small custodian deployment, writers acquire PostgreSQL `SHARE ROW EXCLUSIVE` locks on the three tables in a consistent order **before reading business state**, at the default Read Committed isolation level. This serializes app writes (including eligibility edits) while permitting ordinary readers. It prevents two app requests from exceeding the student limit or issuing stale availability, with a throughput tradeoff suitable for this laboratory. No automatic write retry is enabled. See PostgreSQL's [locking documentation](https://www.postgresql.org/docs/current/explicit-locking.html).

PostgreSQL additionally enforces identity primary keys, restrictive foreign keys, nonblank required fields, normalized identifier uniqueness, valid dates, and a partial unique index on active equipment borrowings. Generated normalized identifiers apply PostgreSQL `lower` and trim the same Unicode whitespace set as .NET `String.Trim`; application uniqueness checks use the same database normalization. Case comparison follows the database collation. The three-item rule is enforced by the service under serialized transactions. Availability can only be changed through borrowing/return service workflows; management requests do not accept transaction fields. Version concurrency tokens reject stale descriptive/eligibility edits. [Schema and constraints](docs/database-diagram.md).

Known pre-commit failures dispose the uncommitted transaction and report failure. A transport error during commit can mean the server committed even though the client did not receive confirmation. The application reports **unknown outcome**, never claims rollback or success, and blocks further writes until a successful Refresh / Reconnect. Staff inspect the refreshed records before deciding whether to resubmit. Tests include a controlled post-commit acknowledgement failure and fresh readback; no fault injection is enabled in the production application.

## Verification

The console project is an integration acceptance runner, invoked using **dotnet run**, not `dotnet test`. It creates a unique database named `equipment_lab4_test_<guid>` and leaves it available for inspection. Configure a local test administrator with `CREATEDB`; never point fault demonstrations at shared data:

```powershell
$env:EQUIPMENTBORROWING_TEST_CONNECTION = 'Host=localhost;Port=5432;Database=postgres;Username=your_test_admin'
dotnet run --project tests/EquipmentBorrowing.Tests
# Use the exact generated name printed above, in a NEW process:
dotnet run --project tests/EquipmentBorrowing.Tests -- --readback equipment_lab4_test_<guid>
```

The runner applies the real migration and checks persistent state using fresh contexts after every rejection. It covers registration, protected edits, eligibility, single/multiple issue, returns, relationship histories, combined search and sorting, database constraints, competing checkout, post-write rollback, offline recovery, unknown commit outcomes, and actual compiled XAML/ViewModel integration through Avalonia Headless. It never drops or resets an existing database. After inspecting it, the owner may remove **only the generated disposable test database** through pgAdmin.

See [recorded evidence and remaining manual demonstration steps](docs/verification.md), [acceptance traceability](docs/traceability.md), [use-case diagram and complete catalog](docs/use-cases.md), [fourteen-case suite](docs/use-case-suite.md), [supplied business rules](docs/business-rules.md), and [database readback queries](docs/database-queries.sql).

## Submission and Git history

Prior commits are preserved. Inspect development history with `git log --oneline --all` and changes with `git show <commit>`. The earlier migration and in-memory implementation can be inspected at commit `4ce4476` before this PostgreSQL continuation. Updated source, reproducible migration, acceptance runner and documentation are included in this repository.

The laboratory also requires each member's individual submission to identify their full name and student ID (and their partner's, if working as a confirmed pair). Supply those in the **private submission comment**, as permitted by the handout; they are intentionally not invented or placed in this public repository. Deferred Lab 5 and future cases are classified in the catalog and are not required functionality here.
