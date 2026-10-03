namespace EquipmentBorrowing.Domain;

public class Borrowing
{
    public int Id { get; private set; }
    public int StudentId { get; private set; }
    public int EquipmentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string EquipmentName { get; private set; } = string.Empty;
    public DateTime DateBorrowed { get; private set; }
    public DateTime ExpectedReturnDate { get; private set; }
    public DateTime? ActualReturnDate { get; set; }
    public BorrowingStatus Status { get; private set; } = BorrowingStatus.Active;

    // Navigation properties for EF Core
    public Student? Student { get; private set; }
    public Equipment? Equipment { get; private set; }

    // Required by EF Core for materialization
    private Borrowing() { }

    public Borrowing(
        int id,
        int studentId,
        int equipmentId,
        string studentName,
        string equipmentName,
        DateTime dateBorrowed,
        DateTime expectedReturnDate)
    {
        Id = id;
        StudentId = studentId;
        EquipmentId = equipmentId;
        StudentName = studentName;
        EquipmentName = equipmentName;
        DateBorrowed = dateBorrowed;
        ExpectedReturnDate = expectedReturnDate;
        Status = BorrowingStatus.Active;
    }

    public void MarkReturned(DateTime returnDate)
    {
        ActualReturnDate = returnDate;
        Status = BorrowingStatus.Returned;
    }
}