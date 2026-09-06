using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentViewModel : ViewModelBase
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly BorrowEquipmentService _borrowEquipmentService;

    public ObservableCollection<Equipment> Equipment { get; } = new();
    public ObservableCollection<Student> Students { get; } = new();

    [ObservableProperty] private Equipment? selectedEquipment;
    [ObservableProperty] private Student? selectedStudent;
    [ObservableProperty] private DateTimeOffset? expectedReturnDate = DateTimeOffset.Now.AddDays(3);
    [ObservableProperty] private string? statusMessage;

    public EquipmentViewModel(
        IEquipmentRepository equipmentRepository,
        IStudentRepository studentRepository,
        BorrowEquipmentService borrowEquipmentService)
    {
        _equipmentRepository = equipmentRepository;
        _studentRepository = studentRepository;
        _borrowEquipmentService = borrowEquipmentService;
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        Equipment.Clear();
        foreach (var item in await _equipmentRepository.GetAllAsync())
            Equipment.Add(item);

        Students.Clear();
        foreach (var student in await _studentRepository.GetAllAsync())
            Students.Add(student);
    }

    [RelayCommand]
    private async Task BorrowAsync()
    {
        // Presentation validation only — no business rules here
        if (SelectedStudent is null) { StatusMessage = "Please select a student."; return; }
        if (SelectedEquipment is null) { StatusMessage = "Please select equipment."; return; }
        if (ExpectedReturnDate is null || ExpectedReturnDate <= DateTimeOffset.Now)
        {
            StatusMessage = "Please choose a valid future return date.";
            return;
        }

        var result = await _borrowEquipmentService.BorrowAsync(
            SelectedStudent.Id,
            SelectedEquipment.Id,
            ExpectedReturnDate.Value.DateTime);

        StatusMessage = result.Message;

        if (result.Success)
            await LoadAsync();
    }
}