using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;

namespace EquipmentBorrowing.Desktop;
public partial class App : Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<EquipmentBorrowingDbContext>(o => o.UseNpgsql(PostgresConfiguration.ConnectionString));
        services.AddSingleton<IWorkflowRepository, PostgresWorkflowRepository>();
        services.AddSingleton<WorkflowService>();
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
