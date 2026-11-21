using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SQLWerk.Services;

public static class ExcelReportPrinter
{
    // A separate STA keeps Excel's modal preview from blocking the SQLWerk interface.
    public static Task<bool> OpenAsync(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            OpenAssociated(path);
            return Task.FromResult(false);
        }
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            object? app = null;
            object? books = null;
            object? workbook = null;
            try
            {
                if (!OperatingSystem.IsWindows())
                {
                    OpenAssociated(path);
                    completion.SetResult(false);
                    return;
                }
                var type = Type.GetTypeFromProgID("Excel.Application");
                if (type == null)
                {
                    OpenAssociated(path);
                    completion.SetResult(false);
                    return;
                }
                app = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Не удалось запустить Excel.");
                dynamic excel = app;
                excel.Visible = true;
                books = excel.Workbooks;
                workbook = ((dynamic)books).Open(path, UpdateLinks: 0, ReadOnly: true);
                ((dynamic)workbook).PrintPreview(false);
                completion.SetResult(true);
            }
            catch (Exception ex) { completion.TrySetException(ex); }
            finally
            {
                // Only this report's private Excel instance is closed; existing Excel windows are untouched.
                try { if (workbook != null) ((dynamic)workbook).Close(false); } catch { }
                try { if (app != null) ((dynamic)app).Quit(); } catch { }
                if (OperatingSystem.IsWindows())
                    foreach (var value in new[] { workbook, books, app })
                        try { if (value != null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value); }
                        catch { }
            }
        }) { IsBackground = true, Name = "SQLWerk report printing" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private static void OpenAssociated(string path) =>
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}
