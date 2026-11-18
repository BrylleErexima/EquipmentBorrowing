using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
namespace EquipmentBorrowing.Application.Services;
public sealed record OperationResult(bool Success, string Message, bool OutcomeUnknown = false);
public sealed class WorkflowService(IWorkflowRepository repository)
{
    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Manila"));
    public Task<List<Student>> StudentsAsync() => repository.ReadAsync(s => s.StudentsAsync());
    public Task<List<Equipment>> EquipmentAsync() => repository.ReadAsync(s => s.EquipmentAsync());
    public Task<List<Borrowing>> BorrowingsAsync(BorrowingQuery query) => repository.ReadAsync(async s =>
    {
        if (query.StudentId is int student && await s.StudentAsync(student) is null) Reject("Student no longer exists. Refresh and reselect.");
        if (query.EquipmentId is int item && await s.ItemAsync(item) is null) Reject("Equipment no longer exists. Refresh and reselect.");
        return await s.BorrowingsAsync(query with { Search = query.Search.Trim() });
    });
    public Task<OperationResult> SaveStudentAsync(int? id, int version, string number, string name, string? contact) => Write(async s =>
    {
        Required(number, "Student number"); Required(name, "Full name");
        if (id is null && contact is { Length: > 0 } && string.IsNullOrWhiteSpace(contact))
            Reject("Contact information must be nonblank when supplied; leave it empty to omit it.");
        if (await s.StudentNumberExistsAsync(number.Trim(), id)) Reject("Student number already exists.");
        if (id is null) s.Add(new Student(number, name, contact));
        else
        {
            var student = await s.StudentAsync(id.Value) ?? throw new WorkflowException("Student no longer exists. Refresh.");
            Current(student.Version, version); student.Update(number, name, contact);
        }
        return "Student saved.";
    });
    public Task<OperationResult> SetEligibilityAsync(int id, int version, bool allowed) => Write(async s =>
    {
        var student = await s.StudentAsync(id) ?? throw new WorkflowException("Student no longer exists. Refresh.");
        Current(student.Version, version);
        if (student.CanBorrow == allowed) return allowed ? "Student is already eligible." : "Student is already restricted.";
        student.SetEligibility(allowed);
        return allowed ? "Student is now eligible." : "Student restricted. Existing loans remain returnable.";
    });
    public Task<OperationResult> SaveEquipmentAsync(int? id, int version, string identifier, string name, string? description) => Write(async s =>
    {
        Required(identifier, "Equipment identifier"); Required(name, "Equipment name");
        if (await s.EquipmentIdentifierExistsAsync(identifier.Trim(), id)) Reject("Equipment identifier already exists.");
        if (id is null) s.Add(new Equipment(identifier, name, description));
        else
        {
            var item = await s.ItemAsync(id.Value) ?? throw new WorkflowException("Equipment no longer exists. Refresh.");
            Current(item.Version, version); item.Update(identifier, name, description);
        }
        return "Equipment saved.";
    });
    public Task<OperationResult> CheckoutAsync(int studentId, IReadOnlyList<int> itemIds, DateOnly? expected, bool multiple) => Write(async s =>
    {
        if (multiple ? itemIds.Count < 2 : itemIds.Count != 1) Reject(multiple ? "Select at least two distinct items." : "Select one item.");
        if (itemIds.Distinct().Count() != itemIds.Count || itemIds.Any(x => x <= 0)) Reject("Duplicate or invalid equipment selection.");
        var today = Today;
        if (expected is null || expected <= today) Reject("Expected return date must be later than today's checkout date (Asia/Manila).");
        var student = await s.StudentAsync(studentId) ?? throw new WorkflowException("Student was not found. Refresh and reselect.");
        if (!student.CanBorrow) Reject("Student is restricted from new borrowing.");
        if (await s.ActiveCountAsync(studentId) + itemIds.Count > 3) Reject("A student may have at most three active items.");
        var items = new List<Equipment>();
        foreach (var id in itemIds)
        {
            var item = await s.ItemAsync(id) ?? throw new WorkflowException("Selected equipment was not found. Refresh and reselect.");
            if (!item.IsAvailable || await s.HasActiveItemAsync(id)) Reject($"{item.Identifier} is unavailable. Refresh your selection.");
            items.Add(item);
        }
        foreach (var item in items)
        {
            s.Add(new Borrowing(studentId, item.Id, today, expected!.Value)); item.SetAvailability(false);
        }
        return $"Checkout saved: {items.Count} item(s).";
    });
    public Task<OperationResult> ReturnAsync(int id) => Write(async s =>
    {
        var borrowing = await s.BorrowingAsync(id) ?? throw new WorkflowException("Borrowing no longer exists. Refresh.");
        if (borrowing.ActualReturnDate is not null) Reject("This borrowing has already been returned.");
        if (Today < borrowing.DateBorrowed) Reject("Return date cannot precede checkout.");
        var item = await s.ItemAsync(borrowing.EquipmentId) ?? throw new WorkflowException("Related equipment is missing. Refresh.");
        borrowing.MarkReturned(Today); item.SetAvailability(true); return "Return saved. History retained.";
    });
    private async Task<OperationResult> Write(Func<IWorkflowSession, Task<string>> action)
    {
        try { return new(true, await repository.WriteAsync(action)); }
        catch (WorkflowException e) { return new(false, e.Message); }
        catch (PersistenceException e) { return new(false, e.Message, e.OutcomeUnknown); }
    }
    private static void Required(string value, string label) { if (string.IsNullOrWhiteSpace(value)) Reject($"{label} is required."); }
    private static void Current(int actual, int expected) { if (actual != expected) Reject("This record changed. Refresh and reselect before editing."); }
    private static void Reject(string message) => throw new WorkflowException(message);
}
