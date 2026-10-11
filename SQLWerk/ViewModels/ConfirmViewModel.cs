using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SQLWerk.ViewModels;

public partial class ConfirmViewModel : ObservableObject
{
    public string Title { get; }
    public string Message { get; }
    public bool Result { get; private set; }

    public event EventHandler? CloseRequested;

    public ConfirmViewModel(string title, string message)
    {
        Title = title;
        Message = message;
    }

    [RelayCommand] private void Yes() { Result = true; CloseRequested?.Invoke(this, EventArgs.Empty); }
    [RelayCommand] private void No() { Result = false; CloseRequested?.Invoke(this, EventArgs.Empty); }
}