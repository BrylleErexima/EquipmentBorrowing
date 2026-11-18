using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel(WorkflowService service) : ObservableObject
{
    public ObservableCollection<Student> Students { get; } = new();
    public ObservableCollection<Equipment> Equipment { get; } = new();
    public ObservableCollection<Equipment> Cart { get; } = new();
    public ObservableCollection<Borrowing> Borrowings { get; } = new();
    public ObservableCollection<Borrowing> StudentHistory { get; } = new();
    public ObservableCollection<Borrowing> EquipmentHistory { get; } = new();
    public StatusFilter[] StatusOptions { get; } = Enum.GetValues<StatusFilter>();
    public BorrowingSort[] SortOptions { get; } = Enum.GetValues<BorrowingSort>();
    private Student[] allStudents = Array.Empty<Student>();
    private Equipment[] allEquipment = Array.Empty<Equipment>();
    private bool recoveryRequired;
    private int studentRequest, equipmentRequest;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanInteract))] private bool isBusy;
    public bool CanInteract => !IsBusy;
    [ObservableProperty] private string message = "Refresh to connect to PostgreSQL.";
    [ObservableProperty] private string studentHistoryMessage = "Select a student to view history.";
    [ObservableProperty] private string equipmentHistoryMessage = "Select an item to view history.";
    [ObservableProperty] private bool isStudentHistoryLoading;
    [ObservableProperty] private bool isEquipmentHistoryLoading;
    [ObservableProperty] private Student? selectedStudent;
    [ObservableProperty] private Student? checkoutStudent;
    [ObservableProperty] private Equipment? selectedEquipment;
    [ObservableProperty] private Equipment? selectedCartItem;
    [ObservableProperty] private Borrowing? selectedBorrowing;
    [ObservableProperty] private string studentNumber = "";
    [ObservableProperty] private string studentName = "";
    [ObservableProperty] private string contact = "";
    [ObservableProperty] private string identifier = "";
    [ObservableProperty] private string equipmentName = "";
    [ObservableProperty] private string description = "";
    [ObservableProperty] private string studentSearch = "";
    [ObservableProperty] private string equipmentSearch = "";
    [ObservableProperty] private bool availableOnly;
    [ObservableProperty] private string borrowingSearch = "";
    [ObservableProperty] private StatusFilter status = StatusFilter.Active;
    [ObservableProperty] private BorrowingSort sort = BorrowingSort.CheckoutDate;
    [ObservableProperty] private bool descending = true;
    [ObservableProperty] private DateTime? expectedReturnDate = WorkflowService.Today.AddDays(3).ToDateTime(TimeOnly.MinValue);

    partial void OnSelectedStudentChanged(Student? value)
    {
        StudentNumber = value?.StudentNumber ?? ""; StudentName = value?.Name ?? ""; Contact = value?.Contact ?? "";
        _ = LoadStudentHistoryAsync();
    }
    partial void OnSelectedEquipmentChanged(Equipment? value)
    {
        Identifier = value?.Identifier ?? ""; EquipmentName = value?.Name ?? ""; Description = value?.Description ?? "";
        _ = LoadEquipmentHistoryAsync();
    }
    partial void OnStudentSearchChanged(string value) => FilterStudents();
    partial void OnEquipmentSearchChanged(string value) => FilterEquipment();
    partial void OnAvailableOnlyChanged(bool value) => FilterEquipment();
    private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> values)
    {
        target.Clear(); foreach (var value in values) target.Add(value);
    }
    private void FilterStudents() => Replace(Students, allStudents.Where(s =>
        s.Name.Contains(StudentSearch.Trim(), StringComparison.OrdinalIgnoreCase) || s.StudentNumber.Contains(StudentSearch.Trim(), StringComparison.OrdinalIgnoreCase)));
    private void FilterEquipment() => Replace(Equipment, allEquipment.Where(e => (!AvailableOnly || e.IsAvailable) &&
        (e.Name.Contains(EquipmentSearch.Trim(), StringComparison.OrdinalIgnoreCase) || e.Identifier.Contains(EquipmentSearch.Trim(), StringComparison.OrdinalIgnoreCase))));

    [RelayCommand] private async Task RefreshAsync() => await Run(async () =>
    {
        await ReloadAsync(); recoveryRequired = false;
        Message = $"Connected. {Students.Count} student(s), {Equipment.Count} item(s), {Borrowings.Count} matching borrowing(s). Empty lists mean no matches.";
    });
    private async Task ReloadAsync()
    {
        // Sequential within this workflow; each service call uses a fresh context.
        var studentId = SelectedStudent?.Id; var equipmentId = SelectedEquipment?.Id; var checkoutId = CheckoutStudent?.Id;
        allStudents = (await service.StudentsAsync()).ToArray();
        allEquipment = (await service.EquipmentAsync()).ToArray();
        FilterStudents(); FilterEquipment();
        SelectedStudent = allStudents.FirstOrDefault(x => x.Id == studentId);
        SelectedEquipment = allEquipment.FirstOrDefault(x => x.Id == equipmentId);
        CheckoutStudent = allStudents.FirstOrDefault(x => x.Id == checkoutId);
        await QueryAsync();
    }
    private async Task Run(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true; Message = "Loading…";
        try { await action(); }
        catch (PersistenceException e) { Message = e.Message; ClearStaleLists(); }
        catch (WorkflowException e) { Message = e.Message; }
        catch (Exception) { Message = "The operation could not complete. Refresh and retry."; }
        finally { IsBusy = false; }
    }
    private void ClearStaleLists()
    {
        allStudents = Array.Empty<Student>(); allEquipment = Array.Empty<Equipment>();
        Students.Clear(); Equipment.Clear(); Borrowings.Clear();
        SelectedStudent = null; SelectedEquipment = null; CheckoutStudent = null; SelectedBorrowing = null;
    }
    private async Task Mutate(Func<Task<OperationResult>> operation)
    {
        await Run(async () =>
        {
            if (recoveryRequired) { Message = "Previous commit outcome is unknown. Use Refresh successfully before deciding to resubmit."; return; }
            var result = await operation();
            recoveryRequired = result.OutcomeUnknown;
            if (result.OutcomeUnknown) { Message = result.Message; return; }
            if (result.Success) Cart.Clear();
            try { await ReloadAsync(); Message = result.Message; }
            catch (PersistenceException e) { ClearStaleLists(); Message = result.Message + " Readback failed: " + e.Message; }
        });
    }
    [RelayCommand] private void NewStudent() { SelectedStudent = null; StudentNumber = StudentName = Contact = ""; }
    [RelayCommand] private void NewEquipment() { SelectedEquipment = null; Identifier = EquipmentName = Description = ""; }
    [RelayCommand] private Task SaveStudentAsync() => Mutate(() => service.SaveStudentAsync(SelectedStudent?.Id, SelectedStudent?.Version ?? 0, StudentNumber, StudentName, Contact));
    [RelayCommand] private Task SaveEquipmentAsync() => Mutate(() => service.SaveEquipmentAsync(SelectedEquipment?.Id, SelectedEquipment?.Version ?? 0, Identifier, EquipmentName, Description));
    [RelayCommand] private Task PermitAsync() => EligibilityAsync(true);
    [RelayCommand] private Task RestrictAsync() => EligibilityAsync(false);
    private Task EligibilityAsync(bool allowed)
    {
        if (SelectedStudent is null) { Message = "Select a student first."; return Task.CompletedTask; }
        return Mutate(() => service.SetEligibilityAsync(SelectedStudent.Id, SelectedStudent.Version, allowed));
    }
    [RelayCommand] private void AddToCart()
    {
        if (SelectedEquipment is null) { Message = "Select an item first."; return; }
        if (Cart.Any(x => x.Id == SelectedEquipment.Id)) { Message = "This item is already selected. Duplicates are rejected."; return; }
        Cart.Add(SelectedEquipment); Message = $"{Cart.Count} item(s) selected for coordinated checkout.";
    }
    [RelayCommand] private void RemoveFromCart() { if (SelectedCartItem is not null) Cart.Remove(SelectedCartItem); }
    [RelayCommand] private Task BorrowAsync() => CheckoutAsync(false);
    [RelayCommand] private Task CheckoutMultipleAsync() => CheckoutAsync(true);
    private Task CheckoutAsync(bool multiple)
    {
        var ids = multiple ? Cart.Select(x => x.Id).ToArray() : SelectedEquipment is null ? Array.Empty<int>() : new[] { SelectedEquipment.Id };
        return Mutate(() => service.CheckoutAsync(CheckoutStudent?.Id ?? 0, ids,
            ExpectedReturnDate is DateTime date ? DateOnly.FromDateTime(date) : null, multiple));
    }
    [RelayCommand] private Task ReturnAsync()
    {
        if (SelectedBorrowing is null) { Message = "Select a borrowing first."; return Task.CompletedTask; }
        return Mutate(() => service.ReturnAsync(SelectedBorrowing.Id));
    }
    private async Task QueryAsync()
    {
        SelectedBorrowing = null;
        Replace(Borrowings, await service.BorrowingsAsync(new(BorrowingSearch, Status, Sort, Descending)));
    }
    [RelayCommand] private Task SearchBorrowingsAsync() => Run(async () =>
    {
        Borrowings.Clear(); await QueryAsync(); Message = Borrowings.Count == 0 ? "No matching borrowings." : $"{Borrowings.Count} borrowing(s) loaded.";
    });
    [RelayCommand] private Task ClearCriteriaAsync()
    {
        BorrowingSearch = ""; Status = StatusFilter.All; Sort = BorrowingSort.CheckoutDate; Descending = true;
        return SearchBorrowingsAsync();
    }
    [RelayCommand] private async Task LoadStudentHistoryAsync()
    {
        var request = ++studentRequest; var id = SelectedStudent?.Id;
        StudentHistory.Clear(); IsStudentHistoryLoading = id is not null;
        StudentHistoryMessage = id is null ? "Select a student to view history." : "Loading student history…";
        if (id is null) return;
        try
        {
            var rows = await service.BorrowingsAsync(new(StudentId: id));
            if (request != studentRequest) return;
            Replace(StudentHistory, rows); StudentHistoryMessage = rows.Count == 0 ? "No borrowing history for this student." : $"{rows.Count} student history record(s).";
        }
        catch (Exception e) { if (request == studentRequest) StudentHistoryMessage = e is PersistenceException or WorkflowException ? e.Message : "History load failed. Retry."; }
        finally { if (request == studentRequest) IsStudentHistoryLoading = false; }
    }
    [RelayCommand] private async Task LoadEquipmentHistoryAsync()
    {
        var request = ++equipmentRequest; var id = SelectedEquipment?.Id;
        EquipmentHistory.Clear(); IsEquipmentHistoryLoading = id is not null;
        EquipmentHistoryMessage = id is null ? "Select an item to view history." : "Loading equipment history…";
        if (id is null) return;
        try
        {
            var rows = await service.BorrowingsAsync(new(EquipmentId: id));
            if (request != equipmentRequest) return;
            Replace(EquipmentHistory, rows); EquipmentHistoryMessage = rows.Count == 0 ? "No borrowing history for this item." : $"{rows.Count} equipment history record(s).";
        }
        catch (Exception e) { if (request == equipmentRequest) EquipmentHistoryMessage = e is PersistenceException or WorkflowException ? e.Message : "History load failed. Retry."; }
        finally { if (request == equipmentRequest) IsEquipmentHistoryLoading = false; }
    }
}
