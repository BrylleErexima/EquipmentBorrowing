using System;

namespace EquipmentBorrowing.Desktop.ViewModels;

public class BorrowingDisplayItem
{
    public int BorrowingId { get; init; }
    public string EquipmentName { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public DateTime ExpectedReturnDate { get; init; }
}