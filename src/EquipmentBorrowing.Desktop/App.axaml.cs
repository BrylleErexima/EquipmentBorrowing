using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Repositories;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;

namespace EquipmentBorrowing.Desktop;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        ConfigureServices(collection);
        Services = collection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Repositories MUST be Singletons to keep in-memory state alive
        services.AddSingleton<IEquipmentRepository, InMemoryEquipmentRepository>();
        services.AddSingleton<IBorrowingRepository, InMemoryBorrowingRepository>();
        services.AddSingleton<IStudentRepository, InMemoryStudentRepository>();

        // Application Services
        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<ReturnEquipmentService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<EquipmentViewModel>();
        services.AddTransient<BorrowingsViewModel>();
    }
}