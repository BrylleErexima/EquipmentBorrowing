# EquipmentBorrowing

## 1. Requirements Analysis

### Actor
**Student** – requests equipment and returns it when finished.

### Use Cases

| Item | Description |
|---|---|
| **Use Case** | Borrow Equipment |
| **Primary Actor** | Student |
| **Preconditions** | Student exists, is allowed to borrow, and equipment is available. |
| **Main Action** | Student requests an available equipment. |
| **Expected Result** | A borrowing record is created and the equipment becomes unavailable. |
| **Possible Failure** | Student is not allowed, equipment does not exist, equipment is unavailable, or the borrowing limit is reached. |

| Item | Description |
|---|---|
| **Use Case** | Return Equipment |
| **Primary Actor** | Student |
| **Preconditions** | The student has an active borrowing. |
| **Main Action** | Student returns the equipment. |
| **Expected Result** | The borrowing is marked as returned and the equipment becomes available. |
| **Possible Failure** | The borrowing does not exist or is already returned. |

| Item | Description |
|---|---|
| **Use Case** | Find Available Equipment |
| **Primary Actor** | Student |
| **Preconditions** | Equipment records are available. |
| **Main Action** | Student checks the available equipment. |
| **Expected Result** | The system shows equipment that can be borrowed. |
| **Possible Failure** | No equipment is currently available. |

### Domain Concepts

- **Student** – stores the student information and borrowing permission/limit.
- **Equipment** – stores the equipment information and availability.
- **Borrowing** – records who borrowed what, the dates, and the status.
- **BorrowingStatus** – shows whether a borrowing is Active or Returned.

The domain objects only handle their own data and state. Database work and user-interface work are kept outside the domain.

## 2. Solution Structure

- **Domain** – contains Student, Equipment, Borrowing, and BorrowingStatus.
- **Application** – contains the borrowing service and repository interfaces.
- **Infrastructure** – contains simple in-memory repositories.
- **Tests** – runs a small success and failure demonstration.

## 3. Dependency Direction

```text
Tests
  ↓
Application → Domain
  ↓
Infrastructure → Application → Domain
```

The application uses interfaces, so it does not depend on a database implementation.

## 4. Use Case Mapping

**Actor:** Student  
**Use Case:** Borrow Equipment  
**Application Service:** BorrowEquipmentService  
**Domain Objects Used:** Student, Equipment, Borrowing, BorrowingStatus  
**Repository Interfaces Used:** IStudentRepository, IEquipmentRepository, IBorrowingRepository  
**Infrastructure Implementations Used:** InMemoryStudentRepository, InMemoryEquipmentRepository, InMemoryBorrowingRepository

## 5. Reflection

**1. Why use a repository interface?**  
It keeps the application code separate from the database or storage method.

**2. What can stay unchanged if SQLite is added later?**  
The domain models, application service, and repository interfaces can stay the same.

**3. Which project would contain Avalonia Views later?**  
A separate UI project would contain them.

**4. Should an Avalonia button directly execute database queries?**  
No. It should call application logic instead.

**5. What represents the business operation?**  
`BorrowEquipmentService` performs the Borrow Equipment operation.

## 6. How to Run

### In Visual Studio

1. Open `EquipmentBorrowing.sln`.
2. In Solution Explorer, right-click **EquipmentBorrowing.Tests**.
3. Choose **Set as Startup Project**.
4. Press **Ctrl + F5**.

The program should show one successful borrowing and one failure because the projector is unavailable.

### Command Line

From the `EquipmentBorrowing` folder:

```text
dotnet build
dotnet run --project tests/EquipmentBorrowing.Tests
```

No database, Entity Framework Core, or graphical interface is used in this activity.

## Desktop Project (Laboratory Activity 2)

`EquipmentBorrowing.Desktop` is the Avalonia presentation layer added on top of the Laboratory Activity 1
architecture. It references `EquipmentBorrowing.Application` and `EquipmentBorrowing.Infrastructure`
only and contains no domain or business logic. Its responsibilities are limited to displaying data,
collecting input, holding presentation state (ViewModels), and invoking existing application services.

## Updated Architecture

Avalonia View
   |  binding / command
   v
ViewModel
   |  application operation
   v
Application Service
   |
   +--> Domain
   |
   v
Repository Interface
   ^
   |
Infrastructure Implementation

## Borrow Equipment Flow
1. User selects a student, equipment item, and expected return date in `EquipmentView`.
2. Pressing "Borrow Equipment" triggers `EquipmentViewModel.BorrowCommand`.
3. The ViewModel performs presentation-only validation (required selections, valid future date).
4. The ViewModel calls `BorrowEquipmentService.BorrowAsync(...)`.
5. The service applies business rules (availability, borrowing limits, eligibility) using
   repositories and domain entities, creates a new `Borrowing` with `Status = BorrowingStatus.Active`,
   and returns a result.
6. The ViewModel updates `StatusMessage` and refreshes the equipment collection.

## Return Equipment Flow
1. User selects an active borrowing in `BorrowingsView`.
2. Pressing "Return Equipment" triggers `BorrowingsViewModel.ReturnCommand`.
3. The ViewModel calls `ReturnEquipmentService.ReturnAsync(borrowingId)`.
4. The service locates the borrowing by ID, checks that its `Status` is `BorrowingStatus.Active`
   (rejecting the request otherwise), calls `borrowing.MarkReturned()` to flip its status to
   `BorrowingStatus.Returned`, and marks the related equipment available again via the repositories.
5. The ViewModel updates `StatusMessage` and reloads both the borrowings and equipment lists.
6. When `BorrowingsViewModel.LoadAsync()` re-fetches active borrowings,
   `InMemoryBorrowingRepository.GetActiveAsync()` joins each `Borrowing` against
   `IEquipmentRepository`/`IStudentRepository` by ID to populate the read-only display fields
   `EquipmentName` and `StudentName` used by the view — this join is presentation shaping only and
   does not affect any business decision.

## Architectural Reflection

1. **Why should the View not call a repository directly?**
   It would bypass presentation state and let the UI reach data access without going through the
   business rules in Application/Domain, breaking separation of concerns and making rule enforcement
   inconsistent.

2. **Why should business rules not be implemented in the ViewModel?**
   They would then be duplicated or diverge between the UI and any other consumer of the Application
   layer, and would be untestable without spinning up the UI framework.

3. **What is the responsibility of the ViewModel?**
   To hold presentation state, expose observable collections/properties for binding, define commands,
   perform lightweight input validation, and delegate actual operations to application services.

4. **Why can the existing Application layer work without knowing Avalonia is being used?**
   Because it depends only on abstractions (repository interfaces) and plain C# types, with no reference
   to any UI framework, so it can be reused by a desktop app, a web app, or tests unchanged.

5. **What advantage is gained from registering dependencies in one composition point?**
   All wiring decisions live in a single place (`App.axaml.cs`), so the rest of the app depends only on
   interfaces, and swapping an implementation later requires changing one file, not the whole codebase.
   This also matters concretely here: `InMemoryBorrowingRepository` itself now depends on
   `IEquipmentRepository` and `IStudentRepository` to build display names, so the composition root is
   what guarantees those two are registered and resolvable before `IBorrowingRepository` is constructed.

6. **If the in-memory repository were replaced by SQLite later, which parts should remain unchanged?**
   Domain, Application services, repository interfaces, and the entire Desktop project (Views and
   ViewModels) — only the Infrastructure implementations and the DI registration in `App.axaml.cs`
   would change. The `EquipmentName`/`StudentName` join in `GetActiveAsync()` would also carry over
   unchanged, since it only calls the repository interfaces, not any in-memory-specific detail.
