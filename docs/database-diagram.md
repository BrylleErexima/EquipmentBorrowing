# Relational Database Schema

## Tables & Relationships

1. **Students** (PK: Id)
   - Id (INTEGER, Primary Key, Auto-increment)
   - StudentNumber (TEXT, Unique, Required)
   - FullName (TEXT, Required)
   - IsAllowedToBorrow (INTEGER/BOOLEAN)

2. **Equipment** (PK: Id)
   - Id (INTEGER, Primary Key, Auto-increment)
   - Name (TEXT, Required)
   - Type (TEXT, Required)
   - IsAvailable (INTEGER/BOOLEAN)

3. **Borrowings** (PK: Id)
   - Id (INTEGER, Primary Key, Auto-increment)
   - StudentId (INTEGER, Foreign Key -> Students.Id)
   - EquipmentId (INTEGER, Foreign Key -> Equipment.Id)
   - BorrowedAt (TEXT/DATETIME)
   - ExpectedReturnAt (TEXT/DATETIME)
   - ReturnedAt (TEXT/DATETIME, Optional)
   - Status (TEXT)