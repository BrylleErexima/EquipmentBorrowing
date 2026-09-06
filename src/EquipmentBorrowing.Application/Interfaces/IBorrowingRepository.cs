using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IBorrowingRepository
{
    Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Borrowing>> GetActiveByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Borrowing>> GetAllAsync(CancellationToken cancellationToken = default);
}
