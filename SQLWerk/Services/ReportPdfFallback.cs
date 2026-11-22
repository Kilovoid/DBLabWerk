using ClosedXML.Excel;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SQLWerk.Models;

namespace SQLWerk.Services;

public static class ReportPdfFallback
{
    private static readonly object FontLock = new();

    public static string Open(string excelPath)
    {
        return OpenPdf(Create(excelPath));
    }

    public static string OpenPdf(string path)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            if (process == null)
                return $"PDF сохранён: {path}. Откройте его в просмотрщике PDF для печати.";
            process.Dispose();
            return $"Для печати открыт PDF: {path}. Нажмите Ctrl+P в просмотрщике.";
        }
        catch (Exception)
        {
            return $"PDF сохранён: {path}. Не удалось открыть просмотрщик. Откройте этот файл вручную или перенесите на другой компьютер для печати.";
        }
    }

    public static string Create(string excelPath)
    {
        // Read the saved workbook, never the current editable group.
        using var book = new XLWorkbook(excelPath);
        var sheet = book.Worksheet(1);
        var last = sheet.LastRowUsed()?.RowNumber() ?? 8;
        var information = Enumerable.Range(3, 4).Select(row => sheet.Cell(row, 1).GetString()).ToArray();
        var rows = Enumerable.Range(8, Math.Max(1, last - 7))
            .Select(row => Enumerable.Range(1, 6).Select(col => sheet.Cell(row, col).GetString()).ToArray()).ToArray();
        var bytes = Render(sheet.Cell(1, 1).GetString(), information, rows);
        var baseName = Path.GetFileNameWithoutExtension(excelPath);
        if (baseName.Length > 100) baseName = baseName.Substring(0, 100);
        var fileName = baseName + $"_печать_{Guid.NewGuid():N}.pdf";
        try { return Write(Path.GetDirectoryName(Path.GetFullPath(excelPath))!, fileName, bytes); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SQLWerk", "Print");
            return Write(directory, fileName, bytes);
        }
    }

    public static void Save(string path, FormationReport report)
    {
        string[] headers = { "Наименование проекта", "Регистрационный номер", "ГРНТИ",
            "Руководители с должностью", "Наличие экспоната", "Краткое наименование ВУЗа" };
        var rows = new[] { headers }.Concat(report.Rows.Select(row => new[]
            { row.Project, row.Registration, row.Grnti, row.Leaders, row.Exhibit, row.University })).ToArray();
        var bytes = Render(report.Title, report.Information, rows);
        var temp = Path.Combine(Path.GetDirectoryName(path)!, $".{Guid.NewGuid():N}.pdf");
        try
        {
            File.WriteAllBytes(temp, bytes);
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static byte[] Render(string reportTitle, string[] information, string[][] rows)
    {
        lock (FontLock) { GlobalFontSettings.FontResolver ??= new ReportFontResolver(); }
        var document = new Document();
        document.Info.Title = reportTitle;
        document.Styles[StyleNames.Normal]!.Font.Name = "SQLWerk Report";
        document.Styles[StyleNames.Normal]!.Font.Size = 9;
        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.2);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.2);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.2);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.2);
        var title = section.AddParagraph(reportTitle);
        title.Format.Font.Bold = true;
        title.Format.Font.Size = 14;
        title.Format.SpaceAfter = Unit.FromCentimeter(0.4);
        foreach (var item in information)
            section.AddParagraph(item).Format.SpaceAfter = Unit.FromPoint(4);
        section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(4);
        var table = section.AddTable();
        table.Borders.Width = Unit.FromPoint(0.5);
        table.Rows.VerticalAlignment = VerticalAlignment.Top;
        table.Rows.LeftIndent = Unit.Zero;
        table.TopPadding = Unit.FromPoint(4);
        table.BottomPadding = Unit.FromPoint(4);
        // Sum = 27.3 cm, the printable width of landscape A4 with 1.2 cm margins.
        foreach (var width in new[] { 7.5, 3.2, 3.3, 6.0, 2.8, 4.5 })
            table.AddColumn(Unit.FromCentimeter(width));
        for (var sourceRow = 0; sourceRow < rows.Length; sourceRow++)
        {
            var row = table.AddRow();
            if (sourceRow == 0)
            {
                row.HeadingFormat = true;
                row.Format.Font.Bold = true;
                row.Shading.Color = Color.FromRgb(220, 236, 248);
            }
            for (var col = 1; col <= 6; col++)
                row.Cells[col - 1].AddParagraph(rows[sourceRow][col - 1]);
        }
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var pdf = renderer.PdfDocument;
        using var bytes = new MemoryStream();
        pdf.Save(bytes, false);
        return bytes.ToArray();
    }

    private static string Write(string directory, string name, byte[] bytes)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        var created = false;
        try
        {
            using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            created = true;
            file.Write(bytes);
        }
        catch
        {
            try { if (created && File.Exists(path)) File.Delete(path); } catch { }
            throw;
        }
        return path;
    }

    private sealed class ReportFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) =>
            new(bold ? "DejaVuSans-Bold" : "DejaVuSans", false, italic);

        public byte[] GetFont(string faceName)
        {
            using var stream = typeof(ReportPdfFallback).Assembly.GetManifestResourceStream($"SQLWerk.ReportFonts.{faceName}.ttf")
                ?? throw new InvalidOperationException("Не найден встроенный шрифт отчёта.");
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            return bytes.ToArray();
        }
    }
}
