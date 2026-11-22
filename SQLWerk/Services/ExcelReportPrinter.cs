using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SQLWerk.Services;

public static class ExcelReportPrinter
{
    public static async Task<string> OpenAsync(string path, Func<Task>? beforePdfFallback = null)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                if (await TryExcelPreviewAsync(path)) return "Просмотр печати закрыт.";
            }
            catch
            {
                // Excel can be registered but unavailable, unlicensed, or fail to open.
                // The saved workbook is retained; use the same PDF fallback in every case.
            }
        }
        if (beforePdfFallback != null) await beforePdfFallback();
        return await Task.Run(() => ReportPdfFallback.Open(path));
    }

    private static Task<bool> TryExcelPreviewAsync(string path)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            object? app = null;
            object? books = null;
            object? workbook = null;
            Exception? error = null;
            var previewShown = false;
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var type = Type.GetTypeFromProgID("Excel.Application");
                    if (type != null)
                    {
                        app = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Не удалось запустить Excel.");
                        dynamic excel = app;
                        excel.Visible = true;
                        books = excel.Workbooks;
                        workbook = ((dynamic)books).Open(path, UpdateLinks: 0, ReadOnly: true);
                        ((dynamic)workbook).PrintPreview(false);
                        previewShown = true;
                    }
                }
            }
            catch (Exception ex) { error = ex; }
            finally
            {
                try { if (workbook != null) ((dynamic)workbook).Close(false); } catch { }
                try { if (app != null) ((dynamic)app).Quit(); } catch { }
                if (OperatingSystem.IsWindows())
                    foreach (var value in new[] { workbook, books, app })
                        try { if (value != null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value); }
                        catch { }
            }
            if (error != null) completion.TrySetException(error);
            else completion.TrySetResult(previewShown);
        }) { IsBackground = true, Name = "SQLWerk report printing" };
        if (OperatingSystem.IsWindows()) thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}
