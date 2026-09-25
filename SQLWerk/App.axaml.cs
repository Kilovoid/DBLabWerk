using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SQLWerk.Data.Abstractions;
using SQLWerk.Data.Sqlite;
using SQLWerk.Services;
using SQLWerk.ViewModels;
using SQLWerk.Views;
using System;

namespace SQLWerk;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IExcelReader, ExcelReader>();
        services.AddSingleton<IConnectionFactory, SqliteConnectionFactory>();

        services.AddScoped<IExhibitRepository, ExhibitRepository>();
        services.AddScoped<IVuzRepository, VuzRepository>();
        services.AddScoped<IGrntiRepository, GrntiRepository>();

        services.AddTransient<MainWindowViewModel>();
    }
}