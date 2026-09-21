using CommunityToolkit.Mvvm.ComponentModel;

namespace SQLWerk.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [ObservableProperty]
    public string Poka { get; set; } = "Goodbye";
}
