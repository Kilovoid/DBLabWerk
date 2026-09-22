using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLWerk.Services;
using Avalonia.Platform.Storage;
using System.IO;
using ExcelDataReader;

namespace SQLWerk.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {

    }
        

    public sealed record GrntiRowVm(int rowNumber, string Original, string Formatted, bool IsValid)
    {
        public string Status => IsValid ? "OK" : "Error";

        public IBrush RowBackGround => IsValid ? Brushes.Transparent : Brushes.Red;
    }
}
