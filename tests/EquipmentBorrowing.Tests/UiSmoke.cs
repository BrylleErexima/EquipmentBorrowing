using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using EquipmentBorrowing.Api;
using EquipmentBorrowing.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;

static class UiSmoke
{
    // Retain the Lab 4 runner, exercising the updated desktop through a real HTTP boundary.
    public static void Run(WorkflowService service)
    {
        var app = ApiHost.Create(["--urls", "http://127.0.0.1:0", "--Logging:LogLevel:Default", "Warning"], services => services.AddSingleton(service));
        app.StartAsync().GetAwaiter().GetResult();
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single() + "/api/v1/") };
            Run(new WorkflowApiClient(http), "LAB-101", "Edited Laptop");
        }
        finally { app.StopAsync().GetAwaiter().GetResult(); app.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
    }
    // Run the actual compiled XAML and ViewModel commands on Avalonia's dispatcher.
    public static void Run(WorkflowApiClient service, string studentNumber = "ST-100", string equipmentName = "Camera", Func<Task>? independentChange = null)
    {
        EnsureUi();
        using var completed = new ManualResetEventSlim();
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
                vm.SelectedStudent = vm.Students.First(x => x.StudentNumber == studentNumber);
                await vm.LoadStudentHistoryCommand.ExecuteAsync(null);
                if (vm.StudentHistory.Count == 0) throw new Exception("Student master-detail did not load.");
                vm.SelectedEquipment = vm.Equipment.First(x => x.Name == equipmentName);
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
                if (independentChange is not null) { await independentChange(); await vm.RefreshCommand.ExecuteAsync(null); if (!vm.Equipment.Any(x => x.Name == "Independent HTTP edit")) throw new Exception("Independent edit did not refresh."); }
                var unknownRepository = new UnknownHandler();
                var uncertainVm = new MainWindowViewModel(new WorkflowApiClient(new HttpClient(unknownRepository) { BaseAddress = new Uri("http://test/api/v1/") }));
                uncertainVm.StudentNumber = "X"; uncertainVm.StudentName = "Name";
                await uncertainVm.SaveStudentCommand.ExecuteAsync(null);
                await uncertainVm.SaveStudentCommand.ExecuteAsync(null);
                if (unknownRepository.Writes != 1 || !uncertainVm.Message.Contains("unknown")) throw new Exception("UI did not block uncertain resubmission.");
                await uncertainVm.RefreshCommand.ExecuteAsync(null);
                await uncertainVm.SaveStudentCommand.ExecuteAsync(null);
                if (unknownRepository.Writes != 2) throw new Exception("UI refresh did not unblock staff decision.");
                var offline = new MainWindowViewModel(new WorkflowApiClient(new HttpClient(new OfflineHandler()) { BaseAddress = new Uri("http://test/api/v1/") }));
                await offline.RefreshCommand.ExecuteAsync(null);
                if (!offline.Message.Contains("no HTTP response") || offline.IsBusy) throw new Exception("UI error state incorrect.");
                window.Close();
                Console.WriteLine("PASS: A01-A16/A18 headless window / compiled XAML, all fourteen workflow command routes, history, empty/error states and unknown-outcome retry gate");
            }
            catch (Exception e) { error = e; }
            finally { completed.Set(); }
        });
        completed.Wait();
        if (error is not null) throw new Exception("UI smoke failed", error);
    }
    private static readonly object UiLock = new();
    private static bool initialized;
    private static void EnsureUi()
    {
        lock (UiLock)
        {
            if (initialized) return;
            using var ready = new ManualResetEventSlim();
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    AppBuilder.Configure<EquipmentBorrowing.Desktop.App>()
                        .UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
                }
                catch (Exception e) { failure = e; }
                finally { ready.Set(); }
                if (failure is null) Dispatcher.UIThread.MainLoop(CancellationToken.None);
            }) { IsBackground = true, Name = "Lab acceptance Avalonia dispatcher" };
            thread.Start(); ready.Wait();
            if (failure is not null) throw failure;
            initialized = true;
        }
    }
    public static void VerifyFeedback(WorkflowApiClient client, string expected)
    {
        EnsureUi();
        using var completed = new ManualResetEventSlim();
        Exception? error = null;
        Dispatcher.UIThread.Post(async () =>
        {
            var window = new MainWindow();
            try
            {
                var vm = new MainWindowViewModel(client); window.DataContext = vm; window.Show();
                var loading = vm.RefreshCommand.ExecuteAsync(null);
                if (!loading.IsCompleted && !vm.IsBusy) throw new Exception("Loading state missing.");
                await loading;
                if (!vm.Message.Contains(expected) || vm.IsBusy) throw new Exception("Incorrect UI failure feedback: " + vm.Message);
                Console.WriteLine("UI observed feedback: " + vm.Message);
                if (Environment.GetEnvironmentVariable("EQUIPMENTBORROWING_UI_CAPTURE") is string path)
                {
                    window.UpdateLayout(); using var frame = window.CaptureRenderedFrame();
                    frame?.Save(Path.Combine(Path.GetDirectoryName(path)!, expected == "HTTP 500" ? "ui-server-error.png" : "ui-unreachable.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                }
            }
            catch (Exception e) { error = e; }
            finally { window.Close(); completed.Set(); }
        });
        completed.Wait();
        if (error is not null) throw error;
    }
    private sealed class OfflineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => throw new HttpRequestException("Controlled transport failure");
    }
    private sealed class UnknownHandler : HttpMessageHandler
    {
        public int Writes { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.Method == HttpMethod.Get) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json") });
            Writes++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = JsonContent.Create(new EquipmentBorrowing.Contracts.ApiError("outcome_unknown", "Commit outcome is unknown. Refresh before deciding to retry.")) });
        }
    }
}