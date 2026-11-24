# Lab 5 architecture and preserved integrity

The desktop is an asynchronous HTTP client. The independently runnable API owns business coordination and PostgreSQL access. Shared Contracts contains transport records only; it has no dependency on the domain, application or database.

```text
Desktop MainWindow → MainWindowViewModel → WorkflowApiClient
    → HttpClient / JSON / /api/v1
    → ApiHost routes → WorkflowService → IWorkflowRepository / IWorkflowSession
    → PostgresWorkflowRepository → DbContext factory → PostgreSQL
```

| Project | References and responsibility |
|---|---|
| Contracts | No project dependencies; typed JSON requests/responses, query options and client results |
| Domain | No project dependencies; Student, Equipment, Borrowing and state transitions |
| Application | Domain; validation, eligibility, limits, checkout, return, read coordination |
| Infrastructure | Application and Domain; EF Core mappings, migration, transaction/query implementation |
| API | Infrastructure and Contracts; registers the service/repository/context factory and translates HTTP/contracts |
| Desktop | Contracts; Avalonia, HttpClient, observable state and commands |
| Tests | API/Desktop plus server layers; controlled disposable data, real HTTP and independent database evidence |

## Startup and code paths

`src/EquipmentBorrowing.Api/Program.cs` starts `ApiHost.Create(args)`. `ApiHost.cs` registers the context factory, repository and WorkflowService. No Avalonia process or desktop dependency is required. It serves the committed OpenAPI document and maps API-01–16. Strict JSON deserialization rejects unknown/protected fields and missing required constructor parameters. Query parsing rejects unsupported values. Middleware returns the required safe error envelope for validation, missing resources, method/route errors and server failures.

`src/EquipmentBorrowing.Desktop/App.axaml.cs` configures HttpClient from `EQUIPMENTBORROWING_API_URL` and injects WorkflowApiClient into MainWindowViewModel. `Services/WorkflowApiClient.cs` serializes the contract records, deserializes published responses and awaits every request. It distinguishes HttpRequestException/timeouts from received HTTP errors. `ViewModels/MainViewModel.cs` retains the existing screen workflows and refreshes lists, histories and availability after mutations. Missing/stale rejections trigger readback. Generation counters prevent older history requests from replacing a newer selection. The desktop has no DbContext, SQL, connection string configuration or PostgreSQL references.

`src/EquipmentBorrowing.Application/Services/WorkflowService.cs` retains original business validation. It now returns the saved entity/list and structured outcome metadata for the adapter, avoiding a second post-commit database operation just to construct a response. Existing version-aware calls remain supported; the published HTTP contract does not accept a client version field. API edits read the current version inside the serialized write transaction and modify only allowed fields. Server validation remains authoritative when the desktop's cached selection is stale.

`src/EquipmentBorrowing.Infrastructure/Repositories/PostgresWorkflowRepository.cs` uses one context per operation, no tracking for reads and one transaction per write. It locks Students, Equipments and Borrowings in consistent order before business reads, using SHARE ROW EXCLUSIVE at Read Committed. That serializes writers and permits readers, protecting limits and availability with a throughput tradeoff appropriate to this small staff system. The session loads borrowing relationships for individual reads as well as histories. Transaction disposal rolls back known failures. A transport failure during commit reports unknown outcome rather than asserting rollback. There is no automatic write retry.

## Schema and preservation

The Lab 4 PostgreSQL migration and model snapshot are unchanged. Primary keys, restrictive foreign keys, nonblank constraints, generated normalized identifier indexes, date checks, version concurrency tokens and the partial unique active-equipment index remain in force. Lab 5 requires no new table or column. Multi-item checkout and return still update loans and availability in the same transaction; returned records are retained.

Only the acceptance runner can reset its newly generated disposable database. Its SaveChanges/transaction interceptors inject faults after actual writes or after commit; neither is installed in the production API. Fresh Npgsql connections capture state independently of EF tracking. Existing databases and the running PostgreSQL service are not stopped or reset.

## Verification references

`tests/EquipmentBorrowing.Tests/Lab5.cs` runs T01–T08 over real Kestrel sockets and PostgreSQL. `UiSmoke.cs` runs actual compiled XAML and ViewModel commands on a dedicated Avalonia dispatcher thread, including unknown-outcome gating and real unreachable/server-error screens. [Results](test-results.md) connect the code paths to actual HTTP, database, standalone-process and UI evidence. Historical Lab 4 acceptance and documentation remain in place; its UI runner now uses an HTTP host to exercise the changed boundary.
