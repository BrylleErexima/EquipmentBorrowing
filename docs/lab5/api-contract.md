# Lab 5 endpoint contract

Base path `/api/v1`; JSON UTF-8. [OpenAPI 3.1](openapi.json) is served at `/openapi/v1.json`. Run the API independently using the root README. Internal positive integer IDs are stable and opaque; never substitute display identifiers such as ST-100 or EQ-001.

| API | Method and route | Input | Success |
|---|---|---|---|
| API-01 | GET `/equipment` | Optional `available=true` or `false` | 200 equipment array |
| API-02 | GET `/students` | Optional `search` over full name/number | 200 student array |
| API-03 | GET `/students/{id}` | None | 200 student |
| API-04 | POST `/students` | `studentNumber`, `fullName`, optional `contact` | 201 student; Location API-03 |
| API-05 | PUT `/students/{id}` | `studentNumber`, `fullName`, `contact` | 200 student |
| API-06 | PATCH `/students/{id}/eligibility` | `isEligible` boolean | 200 student; same value is a no-op |
| API-07 | GET `/equipment/{id}` | None | 200 equipment |
| API-08 | POST `/equipment` | `equipmentCode`, `name`, optional `description` | 201 equipment; Location API-07 |
| API-09 | PUT `/equipment/{id}` | `equipmentCode`, `name`, `description` | 200 equipment |
| API-10 | POST `/borrowings` | `studentId`, `equipmentId`, `expectedReturnDate` | 201 borrowing; Location API-13 |
| API-11 | POST `/checkout-requests` | `studentId`, `equipmentIds` array, `expectedReturnDate` | 201 `{ "borrowings": [...] }` |
| API-12 | POST `/borrowings/{id}/return` | Empty JSON object `{}` | 200 returned borrowing |
| API-13 | GET `/borrowings/{id}` | None | 200 borrowing |
| API-14 | GET `/borrowings` | Optional `search`, `status`, `sort`, `direction` | 200 borrowing array |
| API-15 | GET `/students/{id}/borrowings` | None | 200 active and returned history array |
| API-16 | GET `/equipment/{id}/borrowings` | None | 200 active and returned history array |

Student: `id`, `studentNumber`, `fullName`, nullable `contact`, `isEligible`. Equipment: `id`, `equipmentCode`, `name`, nullable `description`, `isAvailable`. Borrowing: `id`, `studentId`, `equipmentId`, `borrowedDate`, `expectedReturnDate`, nullable `returnedDate`, string `status` (`Active` or `Returned`), and related `student` and `equipment` objects. Related display fields reflect current descriptions while relationships retain their stable IDs.

Creates may omit optional fields or supply null. PUT is a complete update of editable fields: include nullable contact/description; null clears it. Blank equipment description is normalized to null. Blank contact clears it on update; a supplied blank contact on create is rejected. Required strings must remain nonblank after trimming. Display identifiers are trimmed and compared case-insensitively using the existing PostgreSQL normalization/unique indexes. Registration starts eligible/available. IDs, availability, dates and status cannot be set through descriptive edits. Unknown/protected body fields are rejected, including `isAvailable` on equipment PUT and `isEligible` on student PUT.

API-14 defaults to `status=All`, `sort=borrowedDate`, `direction=asc`. Supported alternatives: `Active|Returned|All`, `borrowedDate|expectedReturnDate`, `asc|desc`. Search is trimmed, case-insensitive partial matching over student full name, student number or equipment name. Filters combine. Borrowing ID ascending breaks date ties in both directions. Clear criteria restores the defaults. No matches and empty histories return `[]`; a missing history subject returns 404. Unsupported/repeated query keys and malformed boolean/filter values return 400.

Calendar dates use `YYYY-MM-DD` in Asia/Manila. The server determines checkout and actual return dates. Expected return must be later than checkout; early return is permitted. Multi-item checkout requires at least two distinct items and revalidates current references, eligibility, availability and the three-active-item limit inside one transaction. Every loan and related availability change commits together.

## Errors and recovery

Every error body has `code` and `message`:

```json
{"code":"conflict","message":"Student is restricted from new borrowing."}
```

| Status | Meaning |
|---|---|
| 400 | Invalid JSON/types/required fields, protected or unknown fields, nonpositive request IDs, invalid date or query, duplicate selections, fewer than two items for API-11 |
| 404 | Unknown resource/reference ID or route |
| 405 | Known route with unsupported HTTP method |
| 409 | Duplicate normalized identifier, ineligible student, unavailable item, active limit, conflicting state or repeated return |
| 500 | Safe server/database failure; no credentials or exception stack in body |

Codes are `invalid_request`, `not_found`, `method_not_allowed`, `conflict`, `server_error` and `outcome_unknown`. A connection failure without an HTTP response has **no HTTP status**, and the desktop explains that separately from a received 500. Mutations are not automatically retried.

A received `outcome_unknown` means commit acknowledgement was lost; it does not establish rollback. A transport failure while awaiting a mutation response is conservatively treated the same way. Successfully refresh and inspect state through a fresh connection before deciding to retry. The acceptance runner demonstrates both a known rollback after SaveChanges and an actual committed write whose acknowledgement is lost. No production fault endpoint exists.

## Example

Resolve IDs with GET first; substitute the returned values and a future local date:

```http
POST /api/v1/checkout-requests
Content-Type: application/json

{"studentId":4,"equipmentIds":[2,6],"expectedReturnDate":"2026-11-26"}
```

Example IDs and dates illustrate the shape; they are not fixed seed keys. Full executed requests and bodies are in [the evidence log](evidence/execution.txt).
