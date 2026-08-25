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
