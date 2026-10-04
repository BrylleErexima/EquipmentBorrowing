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
* **`Students`**: Stores student information.
  * `Id` (Primary Key, Auto-increment)
  * `Name` (Text, Required)
  * `CanBorrow` (Boolean)
  * `MaxActiveBorrowings` (Integer)
* **`Equipments`**: Stores equipment catalog.
  * `Id` (Primary Key, Auto-increment)
  * `Name` (Text, Required)
  * `IsAvailable` (Boolean)
* **`Borrowings`**: Tracks borrowing transactions.
  * `Id` (Primary Key, Auto-increment)
  * `StudentId` (Foreign Key $\rightarrow$ `Students.Id`)
  * `EquipmentId` (Foreign Key $\rightarrow$ `Equipments.Id`)
  * `DateBorrowed` (DateTime)
  * `ExpectedReturnDate` (DateTime)
  * `ActualReturnDate` (DateTime, Nullable)

#### Relationships & Constraints
* **`Students` to `Borrowings`**: One-to-Many ($1:N$). A student can have multiple borrowing records.
* **`Equipments` to `Borrowings`**: One-to-Many ($1:N$). Equipment can have multiple borrowing historical records.
* **Constraints**: Foreign Key constraints ensure that a borrowing record must reference a valid student and equipment item. `Student.Name` and `Equipment.Name` are required fields.

---

### 2. SQLite and EF Core Integration

SQLite and EF Core were added to the solution via NuGet packages:
* `Microsoft.EntityFrameworkCore.Sqlite`
* `Microsoft.EntityFrameworkCore.Tools`

In `App.axaml.cs`, the connection path is configured dynamically to target `AppContext.BaseDirectory` so that `app.db` is consistently saved alongside the application executable:

```csharp
var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");

services.AddDbContext<EquipmentBorrowingDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

```

---

### 3. DbContext Responsibility

`EquipmentBorrowingDbContext` serves as the central bridge between the C# application domain models and the SQLite relational database. Its core responsibilities include:

1. **Mapping Entities**: Exposing `DbSet<Student>`, `DbSet<Equipment>`, and `DbSet<Borrowing>` properties to map C# classes to database tables.
2. **Schema Configuration**: Applying Fluent API configurations in `OnModelCreating()` for primary keys, foreign keys, constraints, and initial seed data.
3. **Change Tracking & Persistence**: Translating LINQ queries into SQL statements and executing `SaveChanges()` to commit database changes.

---

### 4. Repository Transition

The architecture evolved from in-memory persistence to database persistence without modifying the core business logic:

#### Previous Architecture (In-Memory)

```text
Repository Interface (IBorrowingRepository)
        ↓
In-Memory Implementation (InMemoryBorrowingRepository)
        ↓
C# List<Borrowing> in RAM (Data lost on app exit)

```

#### New Architecture (Persistent Database)

```text
Repository Interface (IBorrowingRepository)
        ↓
EF Core Repository (EfBorrowingRepository)
        ↓
EF DbContext (EquipmentBorrowingDbContext)
        ↓
SQLite Database (app.db on disk)

```

**Key Advantage**: Because the ViewModels and Domain Services only depend on repository interfaces (`IEquipmentRepository`, `IBorrowingRepository`), swapping the underlying implementation in Dependency Injection required zero changes to UI or domain code.

---

### 5. Migration Process

Database migrations were generated and applied using EF Core tools:

1. **Create Migration**:
```powershell
dotnet ef migrations add InitialCreate --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Desktop

```


2. **Apply Database Schema at Startup**:
In `App.axaml.cs`, automatic schema migration is invoked on application initialization:
```csharp
using (var scope = Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentBorrowingDbContext>();
    dbContext.Database.Migrate();
}

```



---

### 6. Generated SQL Examples

#### Example 1: Query Available Equipment

* **LINQ Statement**:
```csharp
var equipmentList = await _dbContext.Equipments
    .Where(e => e.IsAvailable)
    .ToListAsync();

```


* **Generated SQL**:
```sql
SELECT "e"."Id", "e"."IsAvailable", "e"."Name"
FROM "Equipments" AS "e"
WHERE "e"."IsAvailable" = 1;

```



#### Example 2: Active Borrowings Query

* **LINQ Statement**:
```csharp
var activeBorrowings = await _dbContext.Borrowings
    .Include(b => b.Equipment)
    .Include(b => b.Student)
    .Where(b => b.ActualReturnDate == null)
    .ToListAsync();

```


* **Generated SQL**:
```sql
SELECT "b"."Id", "b"."ActualReturnDate", "b"."DateBorrowed", "b"."EquipmentId", "b"."ExpectedReturnDate", "b"."StudentId", ...
FROM "Borrowings" AS "b"
INNER JOIN "Equipments" AS "e" ON "b"."EquipmentId" = "e"."Id"
INNER JOIN "Students" AS "s" ON "b"."StudentId" = "s"."Id"
WHERE "b"."ActualReturnDate" IS NULL;

```



---

### 7. Persistence Demonstration

Data persistence was verified through the following steps:

1. Ran the desktop application and created a new borrowing record (e.g., borrowing "Dell XPS 15 Laptop" for "Juan Dela Cruz").
2. Verified that the record appeared in the **Active Borrowings** list view.
3. Closed the application completely.
4. Relaunched the application via `dotnet run`.
5. Navigated back to **Active Borrowings** and confirmed the borrowing record remained visible, proving that data was loaded directly from the `app.db` file on disk rather than volatile RAM.

---

### 8. Architectural Reflection

1. **Why did the application not need to be completely rewritten when SQLite was introduced?**
Because the project follows the Dependency Inversion Principle and Repository Pattern. The UI and application layers rely on abstract interfaces (`IEquipmentRepository`, `IBorrowingRepository`) rather than concrete database classes.
2. **Why should the ViewModel not use `DbContext` directly?**
To maintain Separation of Concerns. ViewModels should only handle presentation logic. Using `DbContext` directly in ViewModels couples UI code to EF Core and makes unit testing difficult.
3. **What responsibility does the repository implementation now perform?**
It acts as an adapter that translates repository interface calls into EF Core LINQ queries against `DbContext` and converts database entities into domain models.
4. **What is the purpose of an EF Core migration?**
It keeps the database schema synchronized with changes made to C# entity models over time without deleting existing database records.
5. **Why are foreign keys important in the borrowing database?**
They enforce relational integrity, preventing orphan records by ensuring every borrowing entry references a valid `Student` and `Equipment`.
6. **Why can a read-only query benefit from `AsNoTracking()`?**
It tells EF Core not to track changes on returned entities, reducing memory usage and speeding up query performance for display-only operations.
7. **What would happen to the rest of the application if the SQLite implementation were replaced later by another database provider?**
The domain models, business services, ViewModels, and Views would remain completely unchanged. Only the `AddDbContext` database driver setup and connection string in `App.axaml.cs` would need to be updated.

```

```
