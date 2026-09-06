using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentViewModel : ViewModelBase
{
    public ObservableCollection<Equipment> Equipment { get; } = new();
    public ObservableCollection<Student> Students { get; } = new();

    [ObservableProperty] private Equipment? selectedEquipment;
    [ObservableProperty] private Student? selectedStudent;
    [ObservableProperty] private DateTimeOffset? expectedReturnDate = DateTimeOffset.Now.AddDays(3);
    [ObservableProperty] private string? statusMessage;
}