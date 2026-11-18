using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace EquipmentBorrowing.Infrastructure.Repositories;

public sealed class PostgresWorkflowRepository(IDbContextFactory<EquipmentBorrowingDbContext> factory) : IWorkflowRepository
{
    public async Task<T> ReadAsync<T>(Func<IWorkflowSession, Task<T>> operation)
    {
        try
        {
            await using var db = await factory.CreateDbContextAsync();
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            return await operation(new Session(db));
        }
        catch (Exception e) when (IsDatabaseError(e)) { throw new PersistenceException("Database load failed. Check the connection, then refresh to retry."); }
    }
    public async Task<T> WriteAsync<T>(Func<IWorkflowSession, Task<T>> operation)
    {
        var committing = false;
        try
        {
            await using var db = await factory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();
            // Small custodian deployment: serialize writers, allow readers. Acquire before any rule reads.
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE \"Students\", \"Equipments\", \"Borrowings\" IN SHARE ROW EXCLUSIVE MODE");
            var result = await operation(new Session(db));
            await db.SaveChangesAsync();
            committing = true;
            await transaction.CommitAsync();
            return result;
        }
        catch (Exception e) when (IsDatabaseError(e))
        {
            if (committing && !HasServerError(e))
                throw new PersistenceException("Commit outcome is unknown. Do not resubmit. Reconnect and refresh records before deciding whether to retry.", true);
            if (e is DbUpdateConcurrencyException)
                throw new PersistenceException("A record changed. Refresh and reselect before trying again.");
            if (e is PostgresException { SqlState: "23505" } || e.InnerException is PostgresException { SqlState: "23505" })
                throw new PersistenceException("A duplicate identifier or active equipment borrowing conflicts with this request. Refresh and reselect.");
            throw new PersistenceException("Database write failed; this transaction was not committed. Check the connection, refresh and retry.");
        }
    }
    private static bool IsDatabaseError(Exception e) => e is NpgsqlException or DbUpdateException or TimeoutException or System.IO.IOException
        || (e.InnerException is not null && IsDatabaseError(e.InnerException));
    private static bool HasServerError(Exception e) => e is PostgresException || (e.InnerException is not null && HasServerError(e.InnerException));

    private sealed class Session(EquipmentBorrowingDbContext db) : IWorkflowSession
    {
        public Task<List<Student>> StudentsAsync() => db.Students.OrderBy(x => x.StudentNumber).ToListAsync();
        public Task<List<Equipment>> EquipmentAsync() => db.Equipments.OrderBy(x => x.Identifier).ToListAsync();
        public Task<Student?> StudentAsync(int id) => db.Students.FirstOrDefaultAsync(x => x.Id == id);
        public Task<Equipment?> ItemAsync(int id) => db.Equipments.FirstOrDefaultAsync(x => x.Id == id);
        public Task<Borrowing?> BorrowingAsync(int id) => db.Borrowings.FirstOrDefaultAsync(x => x.Id == id);
        public Task<int> ActiveCountAsync(int studentId) => db.Borrowings.CountAsync(x => x.StudentId == studentId && x.ActualReturnDate == null);
        public Task<bool> HasActiveItemAsync(int id) => db.Borrowings.AnyAsync(x => x.EquipmentId == id && x.ActualReturnDate == null);
        public Task<bool> StudentNumberExistsAsync(string number, int? exceptId) => db.Students
            .FromSqlInterpolated($"SELECT * FROM \"Students\" WHERE \"NormalizedNumber\" = lower({number})")
            .AnyAsync(x => exceptId == null || x.Id != exceptId);
        public Task<bool> EquipmentIdentifierExistsAsync(string identifier, int? exceptId) => db.Equipments
            .FromSqlInterpolated($"SELECT * FROM \"Equipments\" WHERE \"NormalizedIdentifier\" = lower({identifier})")
            .AnyAsync(x => exceptId == null || x.Id != exceptId);
        public void Add(Student student) => db.Students.Add(student);
        public void Add(Equipment item) => db.Equipments.Add(item);
        public void Add(Borrowing borrowing) => db.Borrowings.Add(borrowing);
        public Task<List<Borrowing>> BorrowingsAsync(BorrowingQuery q)
        {
            var rows = db.Borrowings.Include(x => x.Student).Include(x => x.Equipment).AsQueryable();
            if (q.StudentId is int student) rows = rows.Where(x => x.StudentId == student);
            if (q.EquipmentId is int item) rows = rows.Where(x => x.EquipmentId == item);
            if (q.Status == StatusFilter.Active) rows = rows.Where(x => x.ActualReturnDate == null);
            if (q.Status == StatusFilter.Returned) rows = rows.Where(x => x.ActualReturnDate != null);
            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var text = "%" + q.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
                rows = rows.Where(x => EF.Functions.ILike(x.Student.Name, text, "\\") ||
                    EF.Functions.ILike(x.Student.StudentNumber, text, "\\") || EF.Functions.ILike(x.Equipment.Name, text, "\\"));
            }
            var ordered = q.Sort == BorrowingSort.CheckoutDate
                ? (q.Descending ? rows.OrderByDescending(x => x.DateBorrowed) : rows.OrderBy(x => x.DateBorrowed))
                : (q.Descending ? rows.OrderByDescending(x => x.ExpectedReturnDate) : rows.OrderBy(x => x.ExpectedReturnDate));
            return ordered.ThenBy(x => x.Id).ToListAsync();
        }
    }
}
