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

## Laboratory Activity 3 - Database Persistence & Entity Framework Core

### 1. Relational Database Design

#### Database Diagram
The relational schema diagram is located in `docs/database-diagram.md`.

#### Tables & Keys
* **`Students`**: Stores student records.
  * `Id` (Primary Key, Auto-increment)
  * `Name` (Text, Required)
  * `CanBorrow` (Boolean)
  * `MaxActiveBorrowings` (Integer)
* **`Equipments`**: Stores equipment catalog.
  * `Id` (Primary Key, Auto-increment)
  * `Name` (Text, Required)
  * `IsAvailable` (Boolean)
* **`Borrowings`**: Tracks equipment borrowing transactions.
  * `Id` (Primary Key, Auto-increment)
  * `StudentId` (Foreign Key $\rightarrow$ `Students.Id`)
  * `EquipmentId` (Foreign Key $\rightarrow$ `Equipments.Id`)
  * `DateBorrowed` (DateTime)
  * `ExpectedReturnDate` (DateTime)
  * `ActualReturnDate` (DateTime, Nullable)

#### Relationships & Constraints
* **`Students` to `Borrowings`**: One-to-Many ($1:N$).
* **`Equipments` to `Borrowings`**: One-to-Many ($1:N$).
* **Constraints**: Foreign keys enforce referential integrity between `Borrowings`, `Students`, and `Equipments`.

---

### 2. SQLite and EF Core Integration

SQLite and Entity Framework Core were added to `EquipmentBorrowing.Infrastructure` and `EquipmentBorrowing.Desktop` via the following NuGet packages[cite: 8, 9]:
* `Microsoft.EntityFrameworkCore.Sqlite` (v8.0.0)
* `Microsoft.EntityFrameworkCore.Design` (v8.0.0)[cite: 8, 9]

In `App.axaml.cs`, the database file `app.db` is configured dynamically targeting `AppContext.BaseDirectory` so that it persists in the executable directory:

csharp
var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");

services.AddDbContext<EquipmentBorrowingDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
3. DbContext Responsibility
EquipmentBorrowingDbContext (located in EquipmentBorrowing.Infrastructure.Persistence) acts as the primary gateway to the SQLite database:   
PNG

Entity Mapping: Exposes DbSet<Student>, DbSet<Equipment>, and DbSet<Borrowing> properties.   
PNG

Configuration: Configures table constraints, primary/foreign keys, and initial seed data in OnModelCreating() via entity configuration classes in Persistence/Configurations/.   
PNG

Change Tracking & Execution: Translates LINQ operations into SQL queries and commits changes upon calling SaveChangesAsync().

4. Repository Transition
The architecture successfully transitioned from volatile in-memory collections to SQLite database persistence without changing UI or Domain logic:

In-Memory Architecture (Laboratory Activity 2)
Plaintext
IBorrowingRepository / IEquipmentRepository / IStudentRepository
        ↓
InMemoryBorrowingRepository / InMemoryEquipmentRepository / InMemoryStudentRepository
        ↓
In-Memory C# List<T> (Data lost when app closes)
Persistent Architecture (Laboratory Activity 3)
Plaintext
IBorrowingRepository / IEquipmentRepository / IStudentRepository
        ↓
EfBorrowingRepository / EfEquipmentRepository / EfStudentRepository
        ↓
EquipmentBorrowingDbContext (via Entity Framework Core 8.0)
        ↓
SQLite Database (app.db stored on disk)
Key Benefit: ViewModels and Domain Services depend solely on repository interfaces (IBorrowingRepository, IEquipmentRepository, IStudentRepository). Changing the concrete registrations in App.axaml.cs switched the storage mechanism across the entire desktop application seamlessly.   
PNG

5. Migration Process
Creating the Initial Migration:
Using EquipmentBorrowingDbContextFactory for design-time execution, the migration was generated using:   
PNG

PowerShell
dotnet ef migrations add InitialCreate --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Desktop
This produced 20261003090541_InitialCreate.cs and EquipmentBorrowingDbContextModelSnapshot.cs inside EquipmentBorrowing.Infrastructure/Migrations/.   
PNG

Applying Migrations at Application Startup:
In App.axaml.cs, automatic schema migration is executed when the app initializes:

C#
using (var scope = Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentBorrowingDbContext>();
    dbContext.Database.Migrate();
}
6. Generated SQL Examples
Querying Available Equipment
LINQ (C#):

C#
var availableEquipment = await _dbContext.Equipments
    .Where(e => e.IsAvailable)
    .ToListAsync();
Generated SQL (SQLite):

SQL
SELECT "e"."Id", "e"."IsAvailable", "e"."Name"
FROM "Equipments" AS "e"
WHERE "e"."IsAvailable" = 1;
Fetching Active Borrowings
LINQ (C#):

C#
var activeBorrowings = await _dbContext.Borrowings
    .Include(b => b.Equipment)
    .Include(b => b.Student)
    .Where(b => b.ActualReturnDate == null)
    .ToListAsync();
Generated SQL (SQLite):

SQL
SELECT "b"."Id", "b"."ActualReturnDate", "b"."DateBorrowed", "b"."EquipmentId", "b"."ExpectedReturnDate", "b"."StudentId"
FROM "Borrowings" AS "b"
INNER JOIN "Equipments" AS "e" ON "b"."EquipmentId" = "e"."Id"
INNER JOIN "Students" AS "s" ON "b"."StudentId" = "s"."Id"
WHERE "b"."ActualReturnDate" IS NULL;
7. Persistence Demonstration
Verification was completed through the following steps:

Launched EquipmentBorrowing.Desktop via Visual Studio / dotnet run[cite: 8].

Created a new equipment borrowing transaction via the application UI.

Closed the application completely.

Relaunched the application.

Confirmed that the borrowing record and updated equipment availability status loaded directly from app.db on disk.

8. Architectural Reflection
Why did the application not need to be completely rewritten when SQLite was introduced?
Because the system follows Dependency Inversion. The UI and Application Services depend on repository interfaces (IBorrowingRepository, etc.) rather than concrete storage implementations[cite: 9].

Why should the ViewModel not use DbContext directly?
To enforce Separation of Concerns. ViewModels handle presentation logic and UI state; exposing DbContext directly to ViewModels tightly couples the UI to EF Core and breaks unit testability.

What responsibility does the repository implementation now perform?
It translates domain operations into EF Core LINQ queries, manages DbContext interaction, and converts persistence models to domain entities.

What is the purpose of an EF Core migration?
It allows incremental schema updates over time while preserving existing data stored in the database.

Why are foreign keys important in the borrowing database?
They guarantee referential integrity, ensuring that borrowing transactions cannot reference non-existent students or equipment items.

Why can a read-only query benefit from AsNoTracking()?
It instructs EF Core to skip tracking entity changes in memory, reducing memory overhead and improving query performance for display-only views.

What would happen to the rest of the application if the SQLite implementation were replaced later by another database provider?
No changes would be required in the UI, ViewModels, or Domain logic. Only the DbContext provider setup and package registration in App.axaml.cs would change.
