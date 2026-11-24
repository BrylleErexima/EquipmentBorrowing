# Completed Lab 5 traceability matrix

The original fourteen use-case tables remain in [the Lab 4 suite](../use-case-suite.md). Paths below are relative to the repository root. All source paths exist. T01–T08 and evidence are consolidated in [test-results.md](test-results.md).

Common server adapter: `src/EquipmentBorrowing.Api/ApiHost.cs` (`Create`, API route mappings, `Body`, `Failure`, response mapping). Common client: `src/EquipmentBorrowing.Desktop/Services/WorkflowApiClient.cs`. Business service: `src/EquipmentBorrowing.Application/Services/WorkflowService.cs`. Persistence: `src/EquipmentBorrowing.Infrastructure/Repositories/PostgresWorkflowRepository.cs` and the unchanged PostgreSQL migration. Runner: `tests/EquipmentBorrowing.Tests/Lab5.cs`; compiled UI demonstration: `tests/EquipmentBorrowing.Tests/UiSmoke.cs`.

| Lab 4 use case | Applicable rules | Endpoint | Tests | Actual implementation reference | Actual evidence |
|---|---|---|---|---|---|
| UC-01 | BR-05–07,10,18–19 | API-01 | T01,T07 | ApiHost GET equipment; WorkflowService.EquipmentAsync; WorkflowApiClient.EquipmentAsync; MainWindowViewModel.FilterEquipment | execution.txt BEGIN/PASS T01,T07; standalone-http.txt; ui-history.png |
| UC-02 | BR-10,18–19 | API-01 | T01,T07 | ApiHost available query; MainWindowViewModel.AvailableOnly / FilterEquipment | execution.txt T01 available=true and T07; UiSmoke available filter assertion |
| UC-03 | BR-08–11,14–16,18–19 | API-10,13 | T04,T07 | ApiHost POST/GET borrowings; WorkflowService.CheckoutAsync / BorrowingAsync; WorkflowApiClient.CheckoutAsync | execution.txt T04 before/after snapshots and T07 HTTP POST borrowings |
| UC-04 | BR-14,18–19 | API-14 | T01,T06,T07 | ApiHost GET borrowings status; repository Session.BorrowingsAsync; MainWindowViewModel.QueryAsync | execution.txt T01 status=Active, T06 status membership/order and T07 refresh |
| UC-05 | BR-10,13–16,18–19 | API-12,13 | T04,T07 | ApiHost return route; WorkflowService.ReturnAsync; MainWindowViewModel.ReturnCommand | execution.txt T04 return/repeat/restart; T07 HTTP return and history |
| UC-06 | BR-08–12,14–16,18–19 | API-11 | T05,T07 | ApiHost checkout-requests; WorkflowService.CheckoutAsync; repository.WriteAsync; MainWindowViewModel.CheckoutMultipleCommand | execution.txt T05 boundaries/rollback/contention and T07; independent DB snapshots |
| UC-07 | BR-14,18–19 | API-02,03,15 | T01,T06,T07 | ApiHost students and student-history; WorkflowService.StudentAsync / BorrowingsAsync; MainWindowViewModel.LoadStudentHistoryAsync | execution.txt T01/T06 histories and T07 UI command trace |
| UC-08 | BR-14,18–19 | API-01,07,16 | T01,T06,T07 | ApiHost equipment and equipment-history; WorkflowService.ItemAsync / BorrowingsAsync; MainWindowViewModel.LoadEquipmentHistoryAsync | execution.txt T01/T06 history and T07; ui-history.png |
| UC-09 | BR-17–19 | API-14 | T06,T07 | ApiHost query validation; repository Session.BorrowingsAsync; MainWindowViewModel.SearchBorrowingsAsync / ClearCriteriaAsync | execution.txt T06 searches/status/date order/tie-break/combined/reset; T07 empty state |
| UC-12 | BR-01–02,16,18–19 | API-04 | T02,T07 | ApiHost POST students; WorkflowService.SaveStudentAsync; WorkflowApiClient.SaveStudentAsync | execution.txt T02 201/Location, 400/409 and snapshots; T07 UI-001 |
| UC-13 | BR-01–03,14,16,18–19 | API-05 | T02,T07 | ApiHost PUT students; StudentUpdateRequest; WorkflowService.SaveStudentAsync | execution.txt T02 persisted edit/restart and T07 student edit; lab4-regression.txt protected/stale edits |
| UC-14 | BR-04,08,13–14,18–19 | API-06 | T02,T07 | ApiHost PATCH eligibility; WorkflowService.SetEligibilityAsync; MainWindowViewModel.PermitCommand / RestrictCommand | execution.txt T02 restriction/return/permit/no-op and T07 |
| UC-15 | BR-05–06,10,16,18–19 | API-08 | T03,T07 | ApiHost POST equipment; WorkflowService.SaveEquipmentAsync; WorkflowApiClient.SaveEquipmentAsync | execution.txt T03 create/Location/duplicate and T07 UI-EQ records |
| UC-16 | BR-05–07,10,14,16,18–19 | API-01,07,09 | T03,T07 | ApiHost PUT equipment; EquipmentUpdateRequest; WorkflowService.SaveEquipmentAsync | execution.txt T03 descriptive/protected edit and restart; T07 independent HTTP edit |

Evidence filenames in the table resolve under [evidence/](evidence/). Cross-cutting BR-15/16 transaction protection is exercised by T05 and the retained Lab 4 regression. BR-18/19 loading/error/persistence behavior is exercised by T07/T08, the UI error captures, and separate-process restart-readback.txt. API-01–16 machine-readable operations are verified in T01 and retrieved from the independently started executable in standalone-http.txt. No extra/deferred use cases were introduced.
