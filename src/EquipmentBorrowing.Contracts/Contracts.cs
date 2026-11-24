using System.Text.Json.Serialization;

namespace EquipmentBorrowing.Contracts;

public sealed record Student(int Id, string StudentNumber, string FullName, string? Contact, bool IsEligible)
{
    [JsonIgnore] public string Name => FullName;
    [JsonIgnore] public bool CanBorrow => IsEligible;
    public override string ToString() => $"{StudentNumber} · {FullName} ({(IsEligible ? "Eligible" : "Restricted")})";
}
public sealed record Equipment(int Id, string EquipmentCode, string Name, string? Description, bool IsAvailable)
{
    [JsonIgnore] public string Identifier => EquipmentCode;
    public override string ToString() => $"{EquipmentCode} · {Name} · {(IsAvailable ? "Available" : "On loan")} · {Description}";
}
public sealed record Borrowing(int Id, int StudentId, int EquipmentId, DateOnly BorrowedDate,
    DateOnly ExpectedReturnDate, DateOnly? ReturnedDate, string Status, Student Student, Equipment Equipment)
{
    [JsonIgnore] public DateOnly? ActualReturnDate => ReturnedDate;
    public override string ToString() => $"#{Id} · {Student} · {Equipment.EquipmentCode} · {Equipment.Name} | Checkout {BorrowedDate:yyyy-MM-dd} | Due {ExpectedReturnDate:yyyy-MM-dd} | {Status} | Returned {ReturnedDate:yyyy-MM-dd}";
}
public sealed record StudentRequest(string StudentNumber, string FullName, string? Contact = null);
public sealed record EquipmentRequest(string EquipmentCode, string Name, string? Description = null);
public sealed record StudentUpdateRequest(string StudentNumber, string FullName, string? Contact);
public sealed record EquipmentUpdateRequest(string EquipmentCode, string Name, string? Description);
public sealed record EligibilityRequest(bool IsEligible);
public sealed record BorrowingRequest(int StudentId, int EquipmentId, DateOnly ExpectedReturnDate);
public sealed record CheckoutRequest(int StudentId, int[] EquipmentIds, DateOnly ExpectedReturnDate);
public sealed record CheckoutResponse(IReadOnlyList<Borrowing> Borrowings);
public sealed record ApiError(string Code, string Message);
public enum StatusFilter { All, Active, Returned }
public enum BorrowingSort { CheckoutDate, ExpectedReturnDate }
public sealed record BorrowingQuery(string Search = "", StatusFilter Status = StatusFilter.All,
    BorrowingSort Sort = BorrowingSort.CheckoutDate, bool Descending = false, int? StudentId = null, int? EquipmentId = null);
public sealed record ClientResult(bool Success, string Message, bool OutcomeUnknown = false);
public sealed class ApiClientException(string message) : Exception(message);
