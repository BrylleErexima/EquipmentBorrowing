<<<<<<< HEAD
using System;
=======
>>>>>>> af48d0191fdb9a5f37fb5ec114e1fa7e4dddbf8a
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingsViewModel : ViewModelBase
{
    private readonly IBorrowingRepository _borrowingRepository;
<<<<<<< HEAD
    private readonly ReturnEquipmentService _returnService;

    [ObservableProperty]
    private ObservableCollection<Borrowing> _activeBorrowings = new();

    [ObservableProperty]
    private Borrowing? _selectedBorrowing;

    [ObservableProperty]
    private string? _statusMessage;

    public BorrowingsViewModel(IBorrowingRepository borrowingRepository, ReturnEquipmentService returnService)
    {
        _borrowingRepository = borrowingRepository;
        _returnService = returnService;
    }

    public async Task LoadBorrowingsAsync()
    {
        var borrowings = await _borrowingRepository.GetActiveAsync();
        ActiveBorrowings = new ObservableCollection<Borrowing>(borrowings);
=======
    private readonly ReturnEquipmentService _returnEquipmentService;

    public ObservableCollection<Borrowing> ActiveBorrowings { get; } = new();

    [ObservableProperty]
    private Borrowing? selectedBorrowing;

    [ObservableProperty]
    private string? statusMessage;

    public BorrowingsViewModel(
        IBorrowingRepository borrowingRepository,
        ReturnEquipmentService returnEquipmentService)
    {
        _borrowingRepository = borrowingRepository;
        _returnEquipmentService = returnEquipmentService;

        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        ActiveBorrowings.Clear();

        foreach (var borrowing in await _borrowingRepository.GetActiveAsync())
        {
            ActiveBorrowings.Add(borrowing);
        }
>>>>>>> af48d0191fdb9a5f37fb5ec114e1fa7e4dddbf8a
    }

    [RelayCommand]
    private async Task ReturnAsync()
    {
<<<<<<< HEAD
        if (SelectedBorrowing == null)
=======
        if (SelectedBorrowing is null)
>>>>>>> af48d0191fdb9a5f37fb5ec114e1fa7e4dddbf8a
        {
            StatusMessage = "Please select a borrowing record to return.";
            return;
        }

<<<<<<< HEAD
        try
        {
            await _returnService.ReturnAsync(SelectedBorrowing.Id);
            StatusMessage = "Equipment returned successfully!";
            await LoadBorrowingsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Return failed: {ex.Message}";
=======
        var result = await _returnEquipmentService.ReturnAsync(
            SelectedBorrowing.Id);

        StatusMessage = result.Success
            ? "Equipment returned successfully."
            : result.ErrorMessage;

        if (result.Success)
        {
            await LoadAsync();
>>>>>>> af48d0191fdb9a5f37fb5ec114e1fa7e4dddbf8a
        }
    }
}