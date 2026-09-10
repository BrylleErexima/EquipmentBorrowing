using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{

    [ObservableProperty]
    private ViewModelBase _currentView;

    public EquipmentViewModel EquipmentVm { get; }
    public BorrowingsViewModel BorrowingsVm { get; }

    public MainViewModel(EquipmentViewModel equipmentVm, BorrowingsViewModel borrowingsVm)
    {
        EquipmentVm = equipmentVm;
        BorrowingsVm = borrowingsVm;
        _currentView = EquipmentVm;
    }

    [RelayCommand]
    private void NavigateToEquipment() => CurrentView = EquipmentVm;

    [RelayCommand]
    private async Task NavigateToBorrowings()
    {
        await BorrowingsVm.LoadBorrowingsAsync();
        CurrentView = BorrowingsVm;
    }

    [ObservableProperty] private ViewModelBase currentView;

    public EquipmentViewModel EquipmentViewModel { get; }
    public BorrowingsViewModel BorrowingsViewModel { get; }

    public MainWindowViewModel(EquipmentViewModel equipmentViewModel, BorrowingsViewModel borrowingsViewModel)
    {
        EquipmentViewModel = equipmentViewModel;
        BorrowingsViewModel = borrowingsViewModel;
        currentView = equipmentViewModel;
    }

    [RelayCommand] private void ShowEquipment() => CurrentView = EquipmentViewModel;
    [RelayCommand] private void ShowBorrowings() => CurrentView = BorrowingsViewModel;
}