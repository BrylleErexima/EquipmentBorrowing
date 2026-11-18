# Laboratory Activity 4 verification and integration evidence

## Recorded environment and result

Verification date: **18 November 2026 (Asia/Manila)**. Windows, .NET SDK 10.0.404, PostgreSQL 18, Npgsql provider 10.0.3, EF Core 10.0.11, Avalonia 12.1.2. A separate disposable PostgreSQL cluster on loopback port 55484 was used; the pre-existing server on port 5432 was not modified. Test data are fictional.

Final acceptance database: `equipment_lab4_test_eabbd85ebd054f47953ebd67cef2e81d`.

- Release build: **0 warnings, 0 errors**.
- Integration runner: **131 assertions passed**, plus a headless compiled-XAML/ViewModel workflow suite covering the fourteen use-case routes.
- Migration/model check: no pending model changes.
- Server restart and a separate-process readback: active/returned history and edited student/equipment persisted (see linked transcript).

Evidence artifacts:

- [Full acceptance transcript](evidence/acceptance-run.txt).
- [Separate-process restart readback](evidence/restart-readback.txt).
- [PostgreSQL rows, migrations, constraints and indexes after restart](evidence/postgresql-readback.txt).
- [Rendered equipment/history screen](evidence/equipment-history.png), captured from the real XAML using Avalonia Headless and Skia. The screenshot supports presentation evidence; the database reads and controlled failure assertions establish persistence and atomicity.
- [Executable PostgreSQL scenarios](../tests/EquipmentBorrowing.Tests/Program.cs) and [headless UI workflow](../tests/EquipmentBorrowing.Tests/UiSmoke.cs).

## Setup, action, expected and observed outcomes

Each rejection snapshots all persisted student, equipment and borrowing fields before the action, then queries through a **new context** afterward and compares the serialized state. Identity sequence gaps after rolled-back inserts are allowed; no partial business rows are allowed.

| Acceptance | Controlled setup and action | Expected | Observed evidence |
|---|---|---|---|
| A01-A02 | Migrate a new database with 3 demo students and 5 available items; reapply migration; load catalog and available filter; search no matches; use refused connection. | Correct identities/availability, no repeated seed, empty distinct from failure. | Seed counts and migration idempotence pass; headless available filter and zero-result checks pass; offline load uses safe error text. |
| A03-A04 | Register LAB-100; issue one seed item; query active related records. Separately request missing student/item, unavailable item, same-day/absent due date, restricted student and over-limit issue. | One active row and unavailable item; accurate related identities; rejections have no changes. | Single checkout and fresh state reads pass; every rejection compares unchanged complete state. |
| A05 | Restrict active borrower, return before due date, then repeat return and return a missing ID. | Same historical row gets local return date, item available; restriction does not block return; repeats rejected. | Actual date and availability readback pass; repeat/missing requests leave state unchanged. |
| A06 success | Issue 2 distinct items; separately give another student 1 active item and coordinate 2 more. | All requested rows and unavailable states persist; exactly 3 active items allowed. | Both-row readback and exactly-at-limit count assertions pass. |
| A06 rejection | Duplicates, fewer than 2, empty selection, missing student/item, ineligible, invalid date, one unavailable plus available, and over-limit request. | Nothing in the request persists. | Each rejection and fresh complete-state equality pass. |
| A06 concurrency | Run two contexts against one available item; run two different-item requests against a student already holding 2 items. | Exactly one winner; no duplicate active item or fourth active student item. | Both race assertions pass; fresh active counts match. |
| A06/A18 rollback | Test-only SavedChanges interceptor throws IOException **after SQL writes succeed but before outer commit**, while issuing 2 available items. | No borrowing or availability changes persist. | Interceptor confirms result > 0 (writes observed); service reports known failure; fresh full snapshot equals pre-operation snapshot. |
| A07-A08 | Read histories for distinguishable students/items, no-history subjects and nonexistent IDs; select subjects in compiled UI. | Only related active/returned records; empty differs from missing/error; selection replaces details. | Related-ID isolation, empty and missing-subject assertions pass; headless master-detail loads and retained return history pass. |
| A09 | Search each specified field with mixed case and surrounding whitespace, all statuses, both date sorts/directions, combined criteria, no matches, literal wildcard text and reset. | Correct membership/order, stable ID tie-break, no writes; clear restores all. | All query assertions pass; headless reset and explicit no-match feedback pass. Seed checkout dates tie, while expected dates differ. |
| A12 | Register trimmed LAB-100 with contact; reject blank number/name, normalized duplicate and supplied whitespace contact. | One eligible record; rejected requests add none. | Defaults/trimming and unchanged-state rejection assertions pass. |
| A13 | Edit LAB-100 to LAB-101 / Edited Student and clear contact while history exists; invalid, duplicate and stale edits. | Same internal identity/history, absent contact; failed edits unchanged. | Fresh linked-history readback and concurrency/validation rejection checks pass. |
| A14 | Restrict active borrower, repeat choice, reject new loan, return existing one, permit and issue again. | Eligibility changes persist without erasing active/history; repeated state is harmless. | All eligibility, active-history and return checks pass. |
| A15 | Register LAB-EQ with blank optional description; reject blank required values and normalized duplicate. | Available item with null description and no history; no duplicate. | Default, empty-history and rejection assertions pass. |
| A16 | Edit on-loan equipment identifier/name, clear description; attempt invalid/duplicate/stale changes. | Same item/history/unavailability; protected fields absent from descriptive API. | Fresh linked-history/availability readback and invalid/stale rejection checks pass; source confirms no transaction-field inputs. |
| A17 | Apply migration to clean PostgreSQL; directly attempt duplicates (including tab/newline variants), blank/whitespace names, invalid FK and second active borrowing. | PostgreSQL rejects invalid data independently of UI validation. | Expected SQLSTATE 23505, 23514 and 23503 assertions pass; schema/index definitions recorded after restart. |
| A18 unknown | Test-only transaction interceptor throws IOException after real server commit, before the application receives successful acknowledgement. | Report outcome unknown, never rollback/success, no auto retry; inspect fresh state. | Unknown flag/message pass; exactly one new active record is read back. UI fake confirms a second write is blocked until successful refresh. |
| A18 recovery | Use refused loopback port for reads/writes, then reconnect through valid factory. | Secret-free errors and no false success; recovery allows loading. | Error classification, failed-write and recovered-read assertions pass; headless error/loading reset passes. |
| A19 | Review README, diagram/catalog, 14 tables, rule list, traceability, migration/model and Git history. | Source, documented routes and evidence agree. | Committed artifacts and reproducible commands are supplied; previous development history retained. |

## Integrated sequence actually exercised

The PostgreSQL runner registers students/items, borrows, edits records with linked history, restricts an active borrower, rejects a new issue, returns the loan, permits the student, issues multiple items, rejects invalid combinations, forces rollback after SQL writes, competes concurrent requests, and queries histories/search. The headless UI additionally executes registration, descriptive edits, restrict/permit, single borrow/return and coordinated checkout through the actual generated MVVM commands, then verifies three retained history records (one returned plus two active).

The final database was read from a new executable process **after restarting the disposable PostgreSQL server**. See the restart and SQL artifacts. No operational SQLite data were migrated, as the supplied scope explicitly does not require that. Local SQLite files were removed from Git tracking and retained on disk.

## Reproduction and presentation limits

Follow the README from a clean checkout; configure your own secret-free connection, migrate, build, and run the acceptance project. The runner prints its unique disposable database name for separate-process readback. Fault seams live only in the Tests project. They intentionally simulate a post-SQL failure and lost commit acknowledgement; they do not claim to reproduce a physical network outage during a distributed commit.

The rendered headless screen and command integration verify real XAML binding and application operations. Native window interaction, keyboard accessibility, and appearance on the instructor's display remain a useful **manual presentation check**, not recorded native-desktop evidence. For a live walkthrough, use the three staff tabs in the integrated order above, show loading/empty/error feedback, restart the app, and compare the PostgreSQL rows with `docs/database-queries.sql`. Include your own name/student ID (and confirmed partner's, if applicable) privately with the individual submission.
