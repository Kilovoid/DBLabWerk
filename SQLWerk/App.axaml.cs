using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SQLWerk.Data.Sqlite;
using SQLWerk.Services;
using SQLWerk.ViewModels;
using SQLWerk.Views;

namespace SQLWerk;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var factory = new SqliteConnectionFactory("exhibits.db");
            var repo = new ExhibitRepository(factory);
            IExcelReader reader = new ExcelReader();

            var vm = new MainWindowViewModel(reader, repo);

            string xlsPath = @"C:/Users/somas/Downloads/Vyst_mo.XLS";
            vm.Initialize(xlsPath);
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}