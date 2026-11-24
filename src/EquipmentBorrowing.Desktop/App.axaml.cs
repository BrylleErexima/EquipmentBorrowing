using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;

namespace EquipmentBorrowing.Desktop;
public partial class App : Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        var address = Environment.GetEnvironmentVariable("EQUIPMENTBORROWING_API_URL") ?? "http://127.0.0.1:5080/";
        services.AddSingleton(new HttpClient { BaseAddress = new Uri(new Uri(address.TrimEnd('/') + "/"), "api/v1/"), Timeout = TimeSpan.FromSeconds(15) });
        services.AddSingleton<WorkflowApiClient>();
        services.AddSingleton<MainWindowViewModel>();
        var provider = services.BuildServiceProvider();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = provider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
            desktop.MainWindow.Opened += async (_, _) => await vm.RefreshCommand.ExecuteAsync(null);
            desktop.Exit += (_, _) => provider.Dispose();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
