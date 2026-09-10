using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingsViewModel : ViewModelBase
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ReturnEquipmentService _returnEquipmentService;

    public ObservableCollection<BorrowingDisplayItem> ActiveBorrowings { get; } = new();

    [ObservableProperty] private BorrowingDisplayItem? selectedBorrowing;
    [ObservableProperty] private string? statusMessage;

    public BorrowingsViewModel(
        IBorrowingRepository borrowingRepository,
        IEquipmentRepository equipmentRepository,
        IStudentRepository studentRepository,
        ReturnEquipmentService returnEquipmentService)
    {
        _borrowingRepository = borrowingRepository;
        _equipmentRepository = equipmentRepository;
        _studentRepository = studentRepository;
        _returnEquipmentService = returnEquipmentService;
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        var activeBorrowings = await _borrowingRepository.GetActiveAsync();
        var equipment = await _equipmentRepository.GetAllAsync();
        var students = await _studentRepository.GetAllAsync();

        ActiveBorrowings.Clear();
        foreach (var borrowing in activeBorrowings)
        {
            var equipmentName = equipment.FirstOrDefault(e => e.Id == borrowing.EquipmentId)?.Name ?? "Unknown equipment";
            var studentName = students.FirstOrDefault(s => s.Id == borrowing.StudentId)?.Name ?? "Unknown student";

            ActiveBorrowings.Add(new BorrowingDisplayItem
            {
                BorrowingId = borrowing.Id,
                EquipmentName = equipmentName,
                StudentName = studentName,
                ExpectedReturnDate = borrowing.ExpectedReturnDate
            });
        }
    }

    [RelayCommand]
    private async Task ReturnAsync()
    {
        if (SelectedBorrowing is null)
        {
            StatusMessage = "Please select a borrowing record to return.";
            return;
        }

        var result = await _returnEquipmentService.ReturnAsync(SelectedBorrowing.BorrowingId);

        StatusMessage = result.Success ? "Equipment returned successfully." : result.ErrorMessage;

        if (result.Success)
            await LoadAsync();
    }
}