using EquipmentBorrowing.Domain;
namespace EquipmentBorrowing.Application.Interfaces;
public interface IWorkflowRepository
{
    Task<T> ReadAsync<T>(Func<IWorkflowSession, Task<T>> operation);
    Task<T> WriteAsync<T>(Func<IWorkflowSession, Task<T>> operation);
}
public interface IWorkflowSession
{
    Task<List<Student>> StudentsAsync();
    Task<List<Equipment>> EquipmentAsync();
    Task<Student?> StudentAsync(int id);
    Task<Equipment?> ItemAsync(int id);
    Task<Borrowing?> BorrowingAsync(int id);
    Task<List<Borrowing>> BorrowingsAsync(BorrowingQuery query);
    Task<int> ActiveCountAsync(int studentId);
    Task<bool> HasActiveItemAsync(int equipmentId);
    Task<bool> StudentNumberExistsAsync(string number, int? exceptId);
    Task<bool> EquipmentIdentifierExistsAsync(string identifier, int? exceptId);
    void Add(Student student);
    void Add(Equipment equipment);
    void Add(Borrowing borrowing);
}
public enum StatusFilter { All, Active, Returned }
public enum BorrowingSort { CheckoutDate, ExpectedReturnDate }
public sealed record BorrowingQuery(string Search = "", StatusFilter Status = StatusFilter.All,
    BorrowingSort Sort = BorrowingSort.CheckoutDate, bool Descending = false,
    int? StudentId = null, int? EquipmentId = null);
public sealed class WorkflowException(string message, int statusCode = 409) : Exception(message) { public int StatusCode { get; } = statusCode; }
public sealed class PersistenceException(string message, bool outcomeUnknown = false, int statusCode = 500) : Exception(message)
{
    public bool OutcomeUnknown { get; } = outcomeUnknown;
    public int StatusCode { get; } = statusCode;
}
