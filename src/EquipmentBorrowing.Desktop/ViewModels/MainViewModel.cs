using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private ViewModelBase currentView;

    public EquipmentViewModel EquipmentViewModel { get; }
    public BorrowingsViewModel BorrowingsViewModel { get; }

    public MainWindowViewModel(EquipmentViewModel equipmentViewModel, BorrowingsViewModel borrowingsViewModel)
    {
        EquipmentViewModel = equipmentViewModel;
        BorrowingsViewModel = borrowingsViewModel;
        currentView = equipmentViewModel;
    }

    [RelayCommand]
    private async Task ShowEquipment()
    {
        await EquipmentViewModel.LoadAsync();
        CurrentView = EquipmentViewModel;
    }

    [RelayCommand]
    private async Task ShowBorrowings()
    {
        await BorrowingsViewModel.LoadAsync();
        CurrentView = BorrowingsViewModel;
    }
}