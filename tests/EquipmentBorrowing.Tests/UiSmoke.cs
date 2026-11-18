using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;

static class UiSmoke
{
    // Run the actual compiled XAML and ViewModel commands on Avalonia's dispatcher.
    public static void Run(WorkflowService service)
    {
        AppBuilder.Configure<EquipmentBorrowing.Desktop.App>()
            .UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        using var stop = new CancellationTokenSource();
        Exception? error = null;
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var vm = new MainWindowViewModel(service);
                var window = new MainWindow { DataContext = vm };
                window.Show();
                await vm.RefreshCommand.ExecuteAsync(null);
                if (vm.Equipment.Count == 0 || vm.Students.Count == 0 || vm.IsBusy) throw new Exception("UI refresh did not load lists.");
                vm.AvailableOnly = true;
                if (vm.Equipment.Any(x => !x.IsAvailable)) throw new Exception("Available filter showed unavailable equipment.");
                vm.EquipmentSearch = "no-matches-xyz";
                if (vm.Equipment.Count != 0) throw new Exception("Catalog empty filter failed.");
                vm.EquipmentSearch = ""; vm.AvailableOnly = false;
                vm.SelectedStudent = vm.Students.First(x => x.StudentNumber == "LAB-101");
                await vm.LoadStudentHistoryCommand.ExecuteAsync(null);
                if (vm.StudentHistory.Count == 0) throw new Exception("Student master-detail did not load.");
                vm.SelectedEquipment = vm.Equipment.First(x => x.Name == "Edited Laptop");
                await vm.LoadEquipmentHistoryCommand.ExecuteAsync(null);
                if (vm.EquipmentHistory.Count == 0) throw new Exception("Equipment master-detail did not load.");
                if (Environment.GetEnvironmentVariable("EQUIPMENTBORROWING_UI_CAPTURE") is string capturePath)
                {
                    window.UpdateLayout();
                    using var frame = window.CaptureRenderedFrame();
                    frame?.Save(capturePath, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                }
                await vm.ClearCriteriaCommand.ExecuteAsync(null);
                if (!vm.Borrowings.Any(x => x.ActualReturnDate is not null)) throw new Exception("Clear criteria did not restore returned history.");
                vm.BorrowingSearch = "no-matches-xyz";
                await vm.SearchBorrowingsCommand.ExecuteAsync(null);
                if (vm.Borrowings.Count != 0 || vm.Message != "No matching borrowings.") throw new Exception("UI empty state incorrect.");
                await vm.ClearCriteriaCommand.ExecuteAsync(null);
                vm.NewStudentCommand.Execute(null);
                vm.StudentNumber = "UI-001"; vm.StudentName = "UI Smoke Student";
                await vm.SaveStudentCommand.ExecuteAsync(null);
                vm.SelectedStudent = vm.Students.Single(x => x.StudentNumber == "UI-001");
                vm.StudentName = "UI Edited Student";
                await vm.SaveStudentCommand.ExecuteAsync(null);
                await vm.RestrictCommand.ExecuteAsync(null);
                if (vm.SelectedStudent!.CanBorrow) throw new Exception("UI restriction failed.");
                await vm.PermitCommand.ExecuteAsync(null);
                for (var i = 1; i <= 2; i++)
                {
                    vm.NewEquipmentCommand.Execute(null);
                    vm.Identifier = $"UI-EQ-{i}"; vm.EquipmentName = $"UI Equipment {i}";
                    await vm.SaveEquipmentCommand.ExecuteAsync(null);
                }
                vm.SelectedEquipment = vm.Equipment.Single(x => x.Identifier == "UI-EQ-1");
                vm.EquipmentName = "UI Edited Equipment";
                await vm.SaveEquipmentCommand.ExecuteAsync(null);
                vm.CheckoutStudent = vm.Students.Single(x => x.StudentNumber == "UI-001");
                await vm.BorrowCommand.ExecuteAsync(null);
                vm.SelectedBorrowing = vm.Borrowings.Single(x => x.Student.StudentNumber == "UI-001");
                await vm.ReturnCommand.ExecuteAsync(null);
                foreach (var id in new[] { "UI-EQ-1", "UI-EQ-2" })
                {
                    vm.SelectedEquipment = vm.Equipment.Single(x => x.Identifier == id);
                    vm.AddToCartCommand.Execute(null);
                }
                await vm.CheckoutMultipleCommand.ExecuteAsync(null);
                if (vm.Borrowings.Count(x => x.Student.StudentNumber == "UI-001" && x.ActualReturnDate is null) != 2)
                    throw new Exception("UI coordinated checkout failed.");
                vm.SelectedStudent = vm.Students.Single(x => x.StudentNumber == "UI-001");
                await vm.LoadStudentHistoryCommand.ExecuteAsync(null);
                if (vm.StudentHistory.Count != 3) throw new Exception("UI history did not preserve return.");
                var unknownRepository = new UnknownRepository();
                var uncertainVm = new MainWindowViewModel(new WorkflowService(unknownRepository));
                uncertainVm.StudentNumber = "X"; uncertainVm.StudentName = "Name";
                await uncertainVm.SaveStudentCommand.ExecuteAsync(null);
                await uncertainVm.SaveStudentCommand.ExecuteAsync(null);
                if (unknownRepository.Writes != 1 || !uncertainVm.Message.Contains("unknown")) throw new Exception("UI did not block uncertain resubmission.");
                await uncertainVm.RefreshCommand.ExecuteAsync(null);
                await uncertainVm.SaveStudentCommand.ExecuteAsync(null);
                if (unknownRepository.Writes != 2) throw new Exception("UI refresh did not unblock staff decision.");
                var offline = new MainWindowViewModel(new WorkflowService(new OfflineRepository()));
                await offline.RefreshCommand.ExecuteAsync(null);
                if (!offline.Message.Contains("Database load failed") || offline.IsBusy) throw new Exception("UI error state incorrect.");
                window.Close();
                Console.WriteLine("PASS: A01-A16/A18 headless window / compiled XAML, all fourteen workflow command routes, history, empty/error states and unknown-outcome retry gate");
            }
            catch (Exception e) { error = e; }
            finally { stop.Cancel(); }
        });
        Dispatcher.UIThread.MainLoop(stop.Token);
        if (error is not null) throw new Exception("UI smoke failed", error);
    }
    private sealed class OfflineRepository : IWorkflowRepository
    {
        public Task<T> ReadAsync<T>(Func<IWorkflowSession, Task<T>> action) => throw new PersistenceException("Database load failed. Refresh and retry.");
        public Task<T> WriteAsync<T>(Func<IWorkflowSession, Task<T>> action) => throw new PersistenceException("Database write failed.");
    }
    private sealed class UnknownRepository : IWorkflowRepository
    {
        public int Writes { get; private set; }
        public Task<T> ReadAsync<T>(Func<IWorkflowSession, Task<T>> action) => Task.FromResult((T)Activator.CreateInstance(typeof(T))!);
        public Task<T> WriteAsync<T>(Func<IWorkflowSession, Task<T>> action)
        {
            Writes++; throw new PersistenceException("Commit outcome is unknown. Refresh before deciding to retry.", true);
        }
    }
}
