namespace EquipmentBorrowing.Domain;
public class Borrowing
{
    public int Id { get; private set; }
    public int StudentId { get; private set; }
    public int EquipmentId { get; private set; }
    public DateOnly DateBorrowed { get; private set; }
    public DateOnly ExpectedReturnDate { get; private set; }
    public DateOnly? ActualReturnDate { get; private set; }
    public BorrowingStatus Status => ActualReturnDate is null ? BorrowingStatus.Active : BorrowingStatus.Returned;
    public Student Student { get; private set; } = null!;
    public Equipment Equipment { get; private set; } = null!;
    private Borrowing() { }
    public Borrowing(int studentId, int equipmentId, DateOnly checkout, DateOnly expected)
    {
        if (expected <= checkout) throw new ArgumentException("Expected return must be later than checkout.");
        StudentId = studentId; EquipmentId = equipmentId; DateBorrowed = checkout; ExpectedReturnDate = expected;
    }
    public void MarkReturned(DateOnly today)
    {
        if (ActualReturnDate is not null || today < DateBorrowed) throw new InvalidOperationException("Invalid return transition.");
        ActualReturnDate = today;
    }
    public override string ToString() => $"#{Id} · {Student} · {Equipment.Identifier} · {Equipment.Name} | Checkout {DateBorrowed:yyyy-MM-dd} | Due {ExpectedReturnDate:yyyy-MM-dd} | {Status} | Returned {ActualReturnDate:yyyy-MM-dd}";
}
