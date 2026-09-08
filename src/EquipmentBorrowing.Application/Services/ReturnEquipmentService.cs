using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class ReturnEquipmentService
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IEquipmentRepository _equipmentRepository;

    public ReturnEquipmentService(
        IBorrowingRepository borrowingRepository,
        IEquipmentRepository equipmentRepository)
    {
        _borrowingRepository = borrowingRepository;
        _equipmentRepository = equipmentRepository;
    }

    public async Task<ReturnResult> ReturnAsync(int borrowingId)
    {
        var borrowing = await _borrowingRepository.GetByIdAsync(borrowingId);
        if (borrowing is null)
            return ReturnResult.Fail("Borrowing record not found.");

        if (borrowing.Status == BorrowingStatus.Returned)
            return ReturnResult.Fail("This equipment has already been returned.");

        borrowing.MarkReturned();
        await _borrowingRepository.UpdateAsync(borrowing);

        var equipment = await _equipmentRepository.GetByIdAsync(borrowing.EquipmentId);
        if (equipment is not null)
        {
            equipment.IsAvailable = true;
            await _equipmentRepository.UpdateAsync(equipment);
        }

        return ReturnResult.Ok();
    }
}

public class ReturnResult
{
    public bool Success { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ReturnResult Ok() => new() { Success = true };
    public static ReturnResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}