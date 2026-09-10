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
    private readonly BorrowEquipmentService _borrowService;

    [ObservableProperty]
    private ObservableCollection<Equipment> _equipmentList = new();

    [ObservableProperty]
    private ObservableCollection<Student> _students = new();

    [ObservableProperty]
    private Equipment? _selectedEquipment;

    [ObservableProperty]
    private Student? _selectedStudent;

    [ObservableProperty]
    private DateTimeOffset? _expectedReturnDate = DateTimeOffset.Now.AddDays(1);

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty] private Equipment? selectedEquipment;
    [ObservableProperty] private Student? selectedStudent;
    [ObservableProperty] private DateTime? expectedReturnDate = DateTime.Now.AddDays(3);
    [ObservableProperty] private string? statusMessage;


    public EquipmentViewModel(
        IEquipmentRepository equipmentRepository,
        IStudentRepository studentRepository,
        BorrowEquipmentService borrowService)
    {
        _equipmentRepository = equipmentRepository;
        _studentRepository = studentRepository;
        _borrowService = borrowService;

        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        var equipment = await _equipmentRepository.GetAllAsync();
        EquipmentList = new ObservableCollection<Equipment>(equipment);

        var students = await _studentRepository.GetAllAsync();
        Students = new ObservableCollection<Student>(students);
    }

    [RelayCommand]
    private async Task BorrowAsync()
    {

        if (SelectedEquipment == null || SelectedStudent == null || ExpectedReturnDate == null)

        if (SelectedStudent is null) { StatusMessage = "Please select a student."; return; }
        if (SelectedEquipment is null) { StatusMessage = "Please select equipment."; return; }
        if (ExpectedReturnDate is null || ExpectedReturnDate <= DateTime.Now)

        {
            StatusMessage = "Please select equipment, a student, and a return date.";
            return;
        }


        try
        {
            await _borrowService.BorrowAsync(
                SelectedEquipment.Id,
                SelectedStudent.Id,
                ExpectedReturnDate.Value.DateTime);

        var result = await _borrowEquipmentService.BorrowAsync(
            SelectedStudent.Id,
            SelectedEquipment.Id,
            ExpectedReturnDate.Value);  


            StatusMessage = "Equipment borrowed successfully!";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}