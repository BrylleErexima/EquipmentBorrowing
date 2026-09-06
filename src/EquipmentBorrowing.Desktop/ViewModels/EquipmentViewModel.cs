using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentViewModel : ViewModelBase
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IStudentRepository _studentRepository;

    public ObservableCollection<Equipment> Equipment { get; } = new();
    public ObservableCollection<Student> Students { get; } = new();

    [ObservableProperty] private Equipment? selectedEquipment;
    [ObservableProperty] private Student? selectedStudent;
    [ObservableProperty] private DateTimeOffset? expectedReturnDate = DateTimeOffset.Now.AddDays(3);
    [ObservableProperty] private string? statusMessage;

    public EquipmentViewModel(IEquipmentRepository equipmentRepository, IStudentRepository studentRepository)
    {
        _equipmentRepository = equipmentRepository;
        _studentRepository = studentRepository;
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
}