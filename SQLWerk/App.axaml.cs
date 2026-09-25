using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SQLWerk.Data.Abstractions;
using SQLWerk.Data.Sqlite;
using SQLWerk.Models;
using SQLWerk.Services;
using SQLWerk.ViewModels;
using SQLWerk.Views;
using System;
using System.IO;

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
        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var baseDir = AppContext.BaseDirectory;
                var exhibitsPath = Path.Combine(baseDir, "Vyst_mo.xls");
                var vuzPath = Path.Combine(baseDir, "VUZ.xls");
                var grntiPath = Path.Combine(baseDir, "grntirub.xls");

                var vm = Services.GetRequiredService<MainWindowViewModel>();
                vm.Initialize(exhibitsPath, vuzPath, grntiPath);
                desktop.MainWindow = new MainWindow { DataContext = vm };
            }
        }
        catch (Exception ex)
        {
            File.WriteAllText(
                Path.Combine(AppContext.BaseDirectory, "startup-error.txt"),
                ex.ToString());
            throw;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IExcelReader, ExcelReader>();
        var dbPath = Path.Combine(AppContext.BaseDirectory, "sqlwerk.db");
        services.AddSingleton<IConnectionFactory>(_ => new SqliteConnectionFactory(dbPath));

        services.AddScoped<IExhibitRepository, ExhibitRepository>();
        services.AddScoped<IVuzRepository, VuzRepository>();
        services.AddScoped<IGrntiRepository, GrntiRepository>();

        services.AddScoped<IRepository<ExhibitTableRow>>(sp => sp.GetRequiredService<IExhibitRepository>());
        services.AddScoped<IRepository<VuzTableRow>>(sp => sp.GetRequiredService<IVuzRepository>());
        services.AddScoped<IRepository<GrntiTableRow>>(sp => sp.GetRequiredService<IGrntiRepository>());

        services.AddScoped < ImportService<ExhibitTableRow>>();
        services.AddScoped<ImportService<VuzTableRow>>();
        services.AddScoped<ImportService<GrntiTableRow>>();

        services.AddTransient<MainWindowViewModel>();
    }
}