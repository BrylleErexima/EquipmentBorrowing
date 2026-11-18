# Use-case diagram and catalog

Primary actor: **Laboratory Staff / Equipment Custodian**. Students are record subjects and stakeholders. PostgreSQL is the supporting persistence system. Each ellipse is a staff goal; internal rule checks are not include/extend cases.

```mermaid
flowchart LR
  Staff["Laboratory Staff / Equipment Custodian"]
  subgraph System["Campus Equipment Borrowing - Lab 4"]
    UC01(["UC-01: View Equipment Catalog"])
    UC02(["UC-02: Find Available Equipment"])
    UC03(["UC-03: Borrow Equipment"])
    UC04(["UC-04: View Active Borrowings"])
    UC05(["UC-05: Return Equipment"])
    UC06(["UC-06: Check Out Multiple Equipment Items"])
    UC07(["UC-07: View Student Borrowing History"])
    UC08(["UC-08: View Equipment Borrowing History"])
    UC09(["UC-09: Search, Filter, and Sort Borrowings"])
    UC12(["UC-12: Register Student"])
    UC13(["UC-13: Update Student Information"])
    UC14(["UC-14: Change Student Borrowing Eligibility"])
    UC15(["UC-15: Register Equipment"])
    UC16(["UC-16: Update Equipment Information"])
  end
  Staff --- UC01
  Staff --- UC02
  Staff --- UC03
  Staff --- UC04
  Staff --- UC05
  Staff --- UC06
  Staff --- UC07
  Staff --- UC08
  Staff --- UC09
  Staff --- UC12
  Staff --- UC13
  Staff --- UC14
  Staff --- UC15
  Staff --- UC16
```

## Scope catalog

| ID | Use case | Classification |
|---|---|---|
| UC-01 | View Equipment Catalog | Required continuation |
| UC-02 | Find Available Equipment | Required continuation |
| UC-03 | Borrow Equipment | Required continuation |
| UC-04 | View Active Borrowings | Required continuation |
| UC-05 | Return Equipment | Required continuation |
| UC-06 | Check Out Multiple Equipment Items | Required new |
| UC-07 | View Student Borrowing History | Required new |
| UC-08 | View Equipment Borrowing History | Required new |
| UC-09 | Search, Filter, and Sort Borrowings | Required new |
| UC-10 | View Overdue Borrowings | Deferred to Laboratory Activity 5 |
| UC-11 | View Current Equipment Holder | Deferred to Laboratory Activity 5 |
| UC-12 | Register Student | Required new |
| UC-13 | Update Student Information | Required new |
| UC-14 | Change Student Borrowing Eligibility | Required new |
| UC-15 | Register Equipment | Required new |
| UC-16 | Update Equipment Information | Required new |
| UC-17 | Retire Equipment from Circulation | Deferred to Laboratory Activity 5 |
| UC-18 | Mark Equipment as Under Maintenance | Deferred to Laboratory Activity 5 |
| UC-19 | Restore Equipment to Circulation | Deferred to Laboratory Activity 5 |
| UC-20 | Record Equipment Condition Upon Return | Deferred to Laboratory Activity 5 |
| UC-21 | Renew or Extend an Active Borrowing | Deferred to Laboratory Activity 5 |
| UC-22 | Return Multiple Equipment Items | Deferred to Laboratory Activity 5 |
| UC-23 | View Borrowings Due Soon | Deferred to Laboratory Activity 5 |
| UC-24 | View Student's Active Borrowing Count | Deferred to Laboratory Activity 5 |
| UC-25 | View Equipment Utilization Summary | Deferred to Laboratory Activity 5 |
| UC-26 | Manage Equipment Categories | Deferred to Laboratory Activity 5 |
| UC-27 | Void an Erroneous Borrowing Without Erasing Its History | Deferred to Laboratory Activity 5 |
| UC-28 | Request an Equipment Reservation | Future scope |
| UC-29 | Approve, Reject, or Cancel Reservations | Future scope |
| UC-30 | Review Staff Action Audit History | Future scope |
| UC-31 | Manage Staff Accounts and Permissions | Future scope |
| UC-32 | Export Borrowing Reports | Future scope |
| UC-33 | Integrate External Services or Notifications | Future scope |

The [complete fourteen-case suite](use-case-suite.md) preserves the supplied eighteen fields per case, including all actor branches and acceptance criteria. [Business rules](business-rules.md) and [acceptance evidence](traceability.md) accompany it. No further specifications are introduced for deferred/future cases.