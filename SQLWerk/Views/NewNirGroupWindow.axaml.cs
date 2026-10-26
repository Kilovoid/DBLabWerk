using Avalonia.Controls;
using Avalonia.Interactivity;
using SQLWerk.ViewModels;

namespace SQLWerk.Views;

public partial class NewNirGroupWindow : Window
{
    private readonly MainWindowViewModel? _vm;
    private readonly TextBox _input;
    private readonly TextBlock _message;

    public NewNirGroupWindow()
    {
        InitializeComponent();
        _input = this.FindControl<TextBox>("GroupNameInput")!;
        _message = this.FindControl<TextBlock>("ValidationMessage")!;
        Opened += (_, _) => _input.Focus();
    }

    public NewNirGroupWindow(MainWindowViewModel vm) : this() => _vm = vm;

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        var code = (_input.Text ?? "").Trim();
        if (!_vm.TryCreateNirGroup(code, out var error))
        {
            _message.Text = error;
            _input.Focus();
            return;
        }
        Close(code);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();
}
