using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryBorrowingRepository : IBorrowingRepository
{
    private readonly List<Borrowing> _borrowings = new();
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IStudentRepository _studentRepository;

    public InMemoryBorrowingRepository(
        IEquipmentRepository equipmentRepository,
        IStudentRepository studentRepository)
    {
        _equipmentRepository = equipmentRepository;
        _studentRepository = studentRepository;
    }

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

    public async Task<IReadOnlyList<Borrowing>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var active = _borrowings
            .Where(b => b.Status == BorrowingStatus.Active)
            .ToList();

        foreach (var borrowing in active)
        {
            var equipment = await _equipmentRepository.GetByIdAsync(borrowing.EquipmentId, cancellationToken);
            var student = await _studentRepository.GetByIdAsync(borrowing.StudentId, cancellationToken);

            borrowing.EquipmentName = equipment?.Name ?? "Unknown equipment";
            borrowing.StudentName = student?.Name ?? "Unknown student";
        }

        return active;
    }
}   