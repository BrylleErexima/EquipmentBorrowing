using System;
<<<<<<< HEAD
=======
using Avalonia;
>>>>>>> af48d0191fdb9a5f37fb5ec114e1fa7e4dddbf8a
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Repositories;
using EquipmentBorrowing.Desktop.ViewModels; // <-- Add this missing line
using EquipmentBorrowing.Desktop.Views;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Desktop;

public partial class App : Avalonia.Application
{
<<<<<<< HEAD
    public static IServiceProvider? Services { get; private set; }
=======
    public static IServiceProvider Services { get; private set; } = null!;
>>>>>>> af48d0191fdb9a5f37fb5ec114e1fa7e4dddbf8a

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
<<<<<<< HEAD
        var collection = new ServiceCollection();
        ConfigureServices(collection);
        Services = collection.BuildServiceProvider();
=======
        var services = new ServiceCollection();

        ConfigureServices(services);

        Services = services.BuildServiceProvider();
>>>>>>> af48d0191fdb9a5f37fb5ec114e1fa7e4dddbf8a

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

<<<<<<< HEAD
    private void ConfigureServices(IServiceCollection services)
    {
        // Repositories
        services.AddSingleton<IEquipmentRepository, InMemoryEquipmentRepository>();
        services.AddSingleton<IBorrowingRepository, InMemoryBorrowingRepository>();
        services.AddSingleton<IStudentRepository, InMemoryStudentRepository>();

        // Application Services
        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<ReturnEquipmentService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<EquipmentViewModel>();
        services.AddTransient<BorrowingsViewModel>();
=======
    private static void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<IEquipmentRepository, InMemoryEquipmentRepository>();
        services.AddSingleton<IStudentRepository, InMemoryStudentRepository>();
        services.AddSingleton<IBorrowingRepository, InMemoryBorrowingRepository>();

        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<ReturnEquipmentService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<EquipmentViewModel>();
        services.AddSingleton<BorrowingsViewModel>();
>>>>>>> af48d0191fdb9a5f37fb5ec114e1fa7e4dddbf8a
    }
}