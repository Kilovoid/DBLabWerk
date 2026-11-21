using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLWerk.Models;

// A snapshot: later edits to the group do not change an already saved report.
public sealed record FormationReport(string Group, DateTime CreatedAt, string[] Information,
    FormationReportRow[] Rows)
{
    public string Title => $"Отчёт по формированию {Group} от {CreatedAt:dd.MM.yyyy}";

    public static FormationReport Capture(string group, string rubrics, string regions,
        string districts, IEnumerable<NirGroupEntry> entries)
    {
        var rows = entries.DistinctBy(e => e.Exhibit.Id).Select(e => e.Exhibit)
            .Select(e => new FormationReportRow(e.Subject ?? "", e.Regnumber ?? "", e.Grnti ?? "",
                string.Join(Environment.NewLine, new[] { e.Bossname, e.Bosstitle }
                    .Where(value => !string.IsNullOrWhiteSpace(value))),
                ExhibitLabel(e.Exhitype), e.Z2 ?? "")).ToArray();
        return new FormationReport(group, DateTime.Now,
            new[] { $"Выставок: {rows.Length}", rubrics, regions, districts }, rows);
    }

    private static string ExhibitLabel(string? value) => (value ?? "").Trim().ToUpperInvariant() switch
    {
        "Е" or "E" => "Есть", "Н" or "H" or "N" => "Нет",
        "П" or "P" => "Планируется", _ => value ?? ""
    };
}

public sealed record FormationReportRow(string Project, string Registration, string Grnti,
    string Leaders, string Exhibit, string University);
