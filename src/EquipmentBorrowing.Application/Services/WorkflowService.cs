using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
namespace EquipmentBorrowing.Application.Services;
public sealed record OperationResult(bool Success, string Message, bool OutcomeUnknown = false, int StatusCode = 200, object? Value = null);
public sealed class WorkflowService(IWorkflowRepository repository)
{
    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Manila"));
    public Task<Student> StudentAsync(int id) => repository.ReadAsync(async s => await s.StudentAsync(id) ?? throw new WorkflowException("Student not found.", 404));
    public Task<Equipment> ItemAsync(int id) => repository.ReadAsync(async s => await s.ItemAsync(id) ?? throw new WorkflowException("Equipment not found.", 404));
    public Task<Borrowing> BorrowingAsync(int id) => repository.ReadAsync(async s =>
        await s.BorrowingAsync(id) ?? throw new WorkflowException("Borrowing not found.", 404));
    public Task<List<Student>> StudentsAsync() => repository.ReadAsync(s => s.StudentsAsync());
    public Task<List<Equipment>> EquipmentAsync() => repository.ReadAsync(s => s.EquipmentAsync());
    public Task<List<Borrowing>> BorrowingsAsync(BorrowingQuery query) => repository.ReadAsync(async s =>
    {
        if (query.StudentId is int student && await s.StudentAsync(student) is null) Reject("Student no longer exists. Refresh and reselect.", 404);
        if (query.EquipmentId is int item && await s.ItemAsync(item) is null) Reject("Equipment no longer exists. Refresh and reselect.", 404);
        return await s.BorrowingsAsync(query with { Search = query.Search.Trim() });
    });
    public Task<OperationResult> SaveStudentAsync(int? id, int version, string number, string name, string? contact) => Write(async s =>
    {
        Required(number, "Student number"); Required(name, "Full name");
        if (id is null && contact is not null && string.IsNullOrWhiteSpace(contact))
            Reject("Contact information must be nonblank when supplied; omit it or use null.", 400);
        if (await s.StudentNumberExistsAsync(number.Trim(), id)) Reject("Student number already exists.");
        Student saved;
        if (id is null) { saved = new Student(number, name, contact); s.Add(saved); }
        else
        {
            var student = await s.StudentAsync(id.Value) ?? throw new WorkflowException("Student no longer exists. Refresh.", 404);
            if (version >= 0) Current(student.Version, version); student.Update(number, name, contact); saved = student;
        }
        return new Change("Student saved.", saved);
    });
    public Task<OperationResult> SetEligibilityAsync(int id, int version, bool allowed) => Write(async s =>
    {
        var student = await s.StudentAsync(id) ?? throw new WorkflowException("Student no longer exists. Refresh.", 404);
        if (version >= 0) Current(student.Version, version);
        if (student.CanBorrow == allowed) return new Change(allowed ? "Student is already eligible." : "Student is already restricted.", student);
        student.SetEligibility(allowed);
        return new Change(allowed ? "Student is now eligible." : "Student restricted. Existing loans remain returnable.", student);
    });
    public Task<OperationResult> SaveEquipmentAsync(int? id, int version, string identifier, string name, string? description) => Write(async s =>
    {
        Required(identifier, "Equipment identifier"); Required(name, "Equipment name");
        if (await s.EquipmentIdentifierExistsAsync(identifier.Trim(), id)) Reject("Equipment identifier already exists.");
        Equipment saved;
        if (id is null) { saved = new Equipment(identifier, name, description); s.Add(saved); }
        else
        {
            var item = await s.ItemAsync(id.Value) ?? throw new WorkflowException("Equipment no longer exists. Refresh.", 404);
            if (version >= 0) Current(item.Version, version); item.Update(identifier, name, description); saved = item;
        }
        return new Change("Equipment saved.", saved);
    });
    public Task<OperationResult> CheckoutAsync(int studentId, IReadOnlyList<int> itemIds, DateOnly? expected, bool multiple) => Write(async s =>
    {
        if (multiple ? itemIds.Count < 2 : itemIds.Count != 1) Reject(multiple ? "Select at least two distinct items." : "Select one item.", 400);
        if (itemIds.Distinct().Count() != itemIds.Count || itemIds.Any(x => x <= 0)) Reject("Duplicate or invalid equipment selection.", 400);
        var today = Today;
        if (expected is null || expected <= today) Reject("Expected return date must be later than today's checkout date (Asia/Manila).", 400);
        var student = await s.StudentAsync(studentId) ?? throw new WorkflowException("Student was not found. Refresh and reselect.", 404);
        if (!student.CanBorrow) Reject("Student is restricted from new borrowing.");
        if (await s.ActiveCountAsync(studentId) + itemIds.Count > 3) Reject("A student may have at most three active items.");
        var items = new List<Equipment>();
        foreach (var id in itemIds)
        {
            var item = await s.ItemAsync(id) ?? throw new WorkflowException("Selected equipment was not found. Refresh and reselect.", 404);
            if (!item.IsAvailable || await s.HasActiveItemAsync(id)) Reject($"{item.Identifier} is unavailable. Refresh your selection.");
            items.Add(item);
        }
        var loans = new List<Borrowing>();
        foreach (var item in items)
        {
            var loan = new Borrowing(studentId, item.Id, today, expected!.Value); loans.Add(loan); s.Add(loan); item.SetAvailability(false);
        }
        return new Change($"Checkout saved: {items.Count} item(s).", loans);
    });
    public Task<OperationResult> ReturnAsync(int id) => Write(async s =>
    {
        var borrowing = await s.BorrowingAsync(id) ?? throw new WorkflowException("Borrowing no longer exists. Refresh.", 404);
        if (borrowing.ActualReturnDate is not null) Reject("This borrowing has already been returned.");
        if (Today < borrowing.DateBorrowed) Reject("Return date cannot precede checkout.");
        var item = await s.ItemAsync(borrowing.EquipmentId) ?? throw new WorkflowException("Related equipment is missing. Refresh.", 404);
        borrowing.MarkReturned(Today); item.SetAvailability(true); return new Change("Return saved. History retained.", borrowing);
    });
    private sealed record Change(string Message, object? Value = null);
    private async Task<OperationResult> Write(Func<IWorkflowSession, Task<Change>> action)
    {
        try { var change = await repository.WriteAsync(action); return new(true, change.Message, Value: change.Value); }
        catch (WorkflowException e) { return new(false, e.Message, StatusCode: e.StatusCode); }
        catch (PersistenceException e) { return new(false, e.Message, e.OutcomeUnknown, e.StatusCode); }
    }
    private static void Required(string value, string label) { if (string.IsNullOrWhiteSpace(value)) Reject($"{label} is required.", 400); }
    private static void Current(int actual, int expected) { if (actual != expected) Reject("This record changed. Refresh and reselect before editing."); }
    private static void Reject(string message, int statusCode = 409) => throw new WorkflowException(message, statusCode);
}
