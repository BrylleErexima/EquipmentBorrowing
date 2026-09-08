using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryBorrowingRepository : IBorrowingRepository
{
    private readonly List<Borrowing> _borrowings = new();

    public Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        _borrowings.Add(borrowing);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Borrowing>> GetActiveByStudentIdAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Borrowing> result = _borrowings
            .Where(b => b.StudentId == studentId && b.Status == BorrowingStatus.Active)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Borrowing>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<Borrowing>)_borrowings.ToList());

    public Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_borrowings.FirstOrDefault(b => b.Id == id));

    public Task UpdateAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    public Task<IReadOnlyList<Borrowing>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Borrowing> result = _borrowings
            .Where(b => b.Status == BorrowingStatus.Active)
            .ToList();

        return Task.FromResult(result);
    }
}
