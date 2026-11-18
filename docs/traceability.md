# Use-case-to-acceptance traceability

The [suite](use-case-suite.md) preserves the supplied rules and acceptance criteria. The executable evidence is [Program.cs](../tests/EquipmentBorrowing.Tests/Program.cs) plus [UiSmoke.cs](../tests/EquipmentBorrowing.Tests/UiSmoke.cs); observed results and limitations are in [verification.md](verification.md). Console labels correspond to Appendix C acceptance IDs.

| Use case / evidence | Staff route | Application operation | Verification coverage |
|---|---|---|---|
| UC-01 / A01: View Equipment Catalog | Equipment & Checkout / Refresh | `EquipmentAsync` | catalog/seed readback, load failure, compiled XAML |
| UC-02 / A02: Find Available Equipment | Available items only / locate | `EquipmentAsync + presentation filter` | availability readback; UI filter and zero-result feedback |
| UC-03 / A03: Borrow Equipment | Borrow selected catalog item | `CheckoutAsync(multiple: false)` | single success; ineligible, missing, unavailable, dates and limit rejection |
| UC-04 / A04: View Active Borrowings | Borrowings & Returns / Active | `BorrowingsAsync(Status: Active)` | related identity/status checks; initial Active filter |
| UC-05 / A05: Return Equipment | Confirm return | `ReturnAsync` | early restricted return, repeat/missing rejection, returned dates and available item |
| UC-06 / A06: Check Out Multiple Equipment Items | Checkout list / Check out all | `CheckoutAsync(multiple: true)` | two-item success; duplicates, count, missing/unavailable/date/limit; post-write rollback; concurrency |
| UC-07 / A07: View Student Borrowing History | Select student / history | `BorrowingsAsync(StudentId)` | related history isolation, empty versus missing, UI master-detail |
| UC-08 / A08: View Equipment Borrowing History | Select equipment / history | `BorrowingsAsync(EquipmentId)` | related history isolation, empty versus missing, UI master-detail |
| UC-09 / A09: Search, Filter, and Sort Borrowings | Borrowings / Search / Clear | `BorrowingsAsync(BorrowingQuery)` | all specified search fields, statuses, combined criteria, two sort fields/directions and stable IDs |
| UC-12 / A12: Register Student | New student / Save | `SaveStudentAsync(id: null)` | trimmed registration, defaults, duplicates/blank rejection, restart readback |
| UC-13 / A13: Update Student Information | Select student / Save | `SaveStudentAsync(id: existing)` | same ID/history, clear contact, blank/duplicate/stale edits, restart readback |
| UC-14 / A14: Change Student Borrowing Eligibility | Permit / Restrict | `SetEligibilityAsync` | restricted new issue, active loan retained/returnable, repeated choice, permit |
| UC-15 / A15: Register Equipment | New equipment / Save | `SaveEquipmentAsync(id: null)` | initial availability, null description, duplicate/blank rejection, restart readback |
| UC-16 / A16: Update Equipment Information | Select equipment / Save | `SaveEquipmentAsync(id: existing)` | same ID/history/availability, clear description, invalid/duplicate/stale edits, restart readback |

| Cross-cutting acceptance | Implementation / evidence |
|---|---|
| A17 schema/integrity | Clean Npgsql migration; normalized unique indexes, nonblank/date checks, enforced FKs, partial active-item unique index; direct PostgreSQL rejection tests. |
| A18 failure/recovery/architecture | One factory context per operation; serialized writer transactions; fresh readback after controlled post-SQL rollback; post-commit acknowledgement loss reports unknown; UI resubmission gate; offline load/write and recovery. |
| A19 submission | README, complete catalog/diagram, eighteen-field suite for fourteen cases, rules, schema, traceability, verification and preserved Git history. Individual names/IDs supplied privately by the student. |