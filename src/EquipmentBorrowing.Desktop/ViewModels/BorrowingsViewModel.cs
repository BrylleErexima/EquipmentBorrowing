using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingsViewModel : ObservableObject
{
	private readonly IBorrowingRepository _borrowingRepository;
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
		var borrowings = await _borrowingRepository.GetActiveBorrowingsAsync();
		ActiveBorrowings = new ObservableCollection<Borrowing>(borrowings);
	}

	[RelayCommand]
	private async Task ReturnAsync()
	{
		if (SelectedBorrowing == null)
		{
			StatusMessage = "Please select a borrowing record to return.";
			return;
		}

		try
		{
			await _returnService.ReturnAsync(SelectedBorrowing.Id);
			StatusMessage = "Equipment returned successfully!";
			await LoadBorrowingsAsync();
		}
		catch (Exception ex)
		{
			StatusMessage = $"Return failed: {ex.Message}";
		}
	}
}