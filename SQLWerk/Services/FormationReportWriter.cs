using ClosedXML.Excel;
using SQLWerk.Models;
using System;
using System.IO;
using System.Linq;

namespace SQLWerk.Services;

public static class FormationReportWriter
{
    public static void Save(string path, FormationReport report)
    {
        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add("Отчёт");
        sheet.Style.Font.FontName = "Arial";
        sheet.Style.Font.FontSize = 10;
        sheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        sheet.Style.Alignment.WrapText = true;
        sheet.Range("A1:F1").Merge().FirstCell().Value = report.Title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Row(1).Height = Height(report.Title, 130, 22, 28);
        for (var i = 0; i < report.Information.Length; i++)
        {
            var row = i + 3;
            sheet.Range(row, 1, row, 6).Merge().FirstCell().Value = report.Information[i];
            sheet.Row(row).Height = Height(report.Information[i], 155, 14, 18);
        }
        const int header = 8;
        string[] headers = { "Наименование проекта", "Регистрационный номер", "ГРНТИ",
            "Руководители с должностью", "Наличие экспоната", "Краткое наименование ВУЗа" };
        double[] widths = { 48, 23, 22, 40, 18, 26 };
        for (var col = 1; col <= 6; col++)
        {
            sheet.Column(col).Width = widths[col - 1];
            sheet.Cell(header, col).Value = headers[col - 1];
        }
        sheet.Row(header).Height = 32;
        var headerRange = sheet.Range(header, 1, header, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCECF8");
        for (var i = 0; i < report.Rows.Length; i++)
        {
            var source = report.Rows[i];
            string[] values = { source.Project, source.Registration, source.Grnti,
                source.Leaders, source.Exhibit, source.University };
            var row = header + 1 + i;
            for (var col = 1; col <= 6; col++)
            {
                // Assign text, preserving leading zeros and treating '=' as data, not a formula.
                sheet.Cell(row, col).Style.NumberFormat.Format = "@";
                sheet.Cell(row, col).Value = values[col - 1];
            }
            sheet.Row(row).Height = values.Select((text, index) =>
                Height(text, widths[index] * 0.85, 14, 32)).Max();
        }
        var last = header + report.Rows.Length;
        var table = sheet.Range(header, 1, last, 6);
        table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.SheetView.FreezeRows(header);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.SetRowsToRepeatAtTop(header, header);
        sheet.PageSetup.PrintAreas.Add(1, 1, last, 6);
        sheet.PageSetup.Margins.Top = 0.4;
        sheet.PageSetup.Margins.Bottom = 0.4;
        sheet.PageSetup.Margins.Left = 0.3;
        sheet.PageSetup.Margins.Right = 0.3;
        sheet.PageSetup.Header.Left.Clear(); sheet.PageSetup.Header.Center.Clear(); sheet.PageSetup.Header.Right.Clear();
        sheet.PageSetup.Footer.Left.Clear(); sheet.PageSetup.Footer.Center.Clear(); sheet.PageSetup.Footer.Right.Clear();
        var temp = Path.Combine(Path.GetDirectoryName(path)!, $".{Guid.NewGuid():N}.xlsx");
        try
        {
            book.SaveAs(temp);
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static double Height(string text, double characters, double lineHeight, double minimum) =>
        Math.Min(409, Math.Max(minimum, text.Split('\n').Sum(line =>
            Math.Max(1, Math.Ceiling(line.TrimEnd('\r').Length / characters))) * lineHeight + 6));
}
