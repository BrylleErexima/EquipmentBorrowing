using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed record BorrowEquipmentResult(bool Success, string Message, Borrowing? Borrowing = null);

public class BorrowEquipmentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IBorrowingRepository _borrowingRepository;

    public BorrowEquipmentService(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        IBorrowingRepository borrowingRepository)
    {
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _borrowingRepository = borrowingRepository;
    }

    public async Task<BorrowEquipmentResult> BorrowAsync(
        int studentId,
        int equipmentId,
        DateTime expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
            return new(false, "Student was not found.");

        if (!student.CanBorrow)
            return new(false, "Student is not allowed to borrow equipment.");

        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
        if (equipment is null)
            return new(false, "Equipment was not found.");

        if (!equipment.IsAvailable)
            return new(false, "Equipment is not available.");

        var activeBorrowings = await _borrowingRepository
    .GetActiveByStudentIdAsync(studentId, cancellationToken);

        if (activeBorrowings.Count >= student.MaxActiveBorrowings)
            return new(false, "Student has reached the maximum number of active borrowings.");

        var allBorrowings = await _borrowingRepository.GetAllAsync(cancellationToken);

        var borrowing = new Borrowing(
            id: allBorrowings.Count + 1,   
            studentId: student.Id,
            equipmentId: equipment.Id,
            dateBorrowed: DateTime.Today,
            expectedReturnDate: expectedReturnDate);

        await _borrowingRepository.AddAsync(borrowing, cancellationToken);

        equipment.IsAvailable = false;
        await _equipmentRepository.UpdateAsync(equipment, cancellationToken);

        return new(true, "Equipment borrowed successfully.", borrowing);
    }
}
