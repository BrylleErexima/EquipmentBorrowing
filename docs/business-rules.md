# Supplied Lab 4 business rules

Appendix B — Business Rules and Operational Policies
The following rules govern the required workflows.
Students and equipment
- BR-01 — Student identity: Each student has a required institutional student number, unique after trimming
leading/trailing whitespace and case-insensitive comparison. Empty or duplicate numbers are rejected by validation and
database enforcement.
- BR-02 — Student attributes: A student has a required nonblank full name and student number. Contact information is
optional; if supplied, it must be nonblank after trimming. A newly registered student is eligible to borrow. No email format or
additional personal data is required.
- BR-03 — Student updates: Staff may edit the full name, optional contact information, and institutional number, subject to
BR-01–02. A blank optional contact clears it. The stable internal identity and all existing borrowing relationships and history
remain intact; eligibility changes use UC-14.
- BR-04 — Eligibility changes: Staff may permit or restrict an existing student. Restriction prevents new borrowing, but
does not cancel, erase, or prevent return of existing active borrowings. Repeating the current choice causes no unintended
change and gives understandable feedback.
- BR-05 — Equipment identity: Each physical item has a required, nonblank unique equipment identifier; trim surrounding
whitespace and compare identifiers case-insensitively. Enforce uniqueness in validation and the database.
- BR-06 — Equipment attributes and registration: An item has a required nonblank name and identifier, plus an optional
description/type. A blank optional description is stored as absent. Newly registered equipment is available and has no
active borrowing.
- BR-07 — Equipment updates: Staff may edit identifier, name and description subject to BR-05–06; the stable internal
identity and borrowing history remain intact. Availability, borrowing status and return dates are not editable descriptive
fields; they change only through the applicable borrowing/return workflow. Maintenance and retirement are deferred.
Borrowing and history
- BR-08 — Eligibility and references: A borrowing requires an existing eligible student and existing selected equipment.
A missing or stale reference is rejected without creating records.
- BR-09 — Active limit: A student may have at most three active item borrowings. Existing active records plus distinct
items requested must not exceed three. UC-03 counts one item; UC-06 counts all selected items.
- BR-10 — Availability: An item is borrowable only if available and without an active borrowing. A successful issue makes
it unavailable; a successful return makes it available. A database constraint or equivalent database-enforced protection
must prevent more than one active borrowing for the same item.
- BR-11 — Checkout date: The expected return date must be later than the checkout date, using consistently documented
local calendar dates. An absent or invalid date is rejected.
- BR-12 — Multi-item request: UC-06 requires at least two distinct items for one student and one expected return date.
Reject duplicates and invalid selections rather than silently counting or omitting them. Create one active borrowing per
item; a checkout parent entity is optional.

- BR-13 — Return transition: Only an active borrowing may be returned. Record the actual return date as the local
calendar date of return, which cannot precede checkout; a future expected date does not prevent early return. Mark the
record returned, retain it, and make the item available. Reject a repeat return without changing state.
- BR-14 — History and relationships: Each borrowing refers to exactly one existing student and one existing physical
item through enforced foreign keys. History retains active and returned records, dates and relationships through edits and
returns. Do not hard-delete students, equipment or borrowing history as part of required Lab 4 functionality.
Integrity, retrieval and recovery
- BR-15 — Revalidation and atomicity: Recheck current eligibility, active count, references and availability at the
transaction boundary. A multi-item issue commits every borrowing and related availability change together or commits
none. A conflicting update must not produce duplicate active borrowings or partial checkout; reject and refresh as
appropriate. No distributed infrastructure or particular locking technique is prescribed.
- BR-16 — Database integrity: PostgreSQL enforces primary keys, foreign keys, required fields, student/equipment
identifier uniqueness and the single-active-borrowing protection. Consistent normalization of identifiers must match the
uniqueness enforcement. Application validation supplements, not replaces, these constraints.
- BR-17 — Search and results: Search by case-insensitive partial text in student name, institutional number or equipment
name; trim surrounding search whitespace. Filter active, returned or all; sort by checkout date or expected return date,
ascending or descending, with a stable borrowing identifier as a tie-breaker. Criteria may combine and can be cleared.
Display matching records accurately; show an explicit empty state rather than an error for no matches.
- BR-18 — Errors and unknown outcomes: Explain validation rejections and database failures without exposing
credentials. Known failed writes leave no partial changes and do not report success. If connection loss makes commit
outcome unknown, say so, do not automatically retry or assert rollback, and reconnect and refresh before a staff decision;
the underlying transaction remains all-or-nothing. Let users retry after recovery.
- BR-19 — Persistence and loading: Store and retrieve required student, equipment and borrowing changes in
PostgreSQL across application restarts. Load database-backed histories and borrowing search asynchronously with
observable loading/error state and a responsive window. Distinguish missing records, no matches and connection failures.