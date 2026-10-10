using Microsoft.Extensions.Logging;
using SQLWerk.Data.Abstractions;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLWerk.Services
{
    public class CrossValidationService
    {
        private readonly ILogger<CrossValidationService> _logger;

        public CrossValidationService(ILogger<CrossValidationService> logger) => _logger = logger;

        public int RemoveInvalidExhibits(
            IExhibitRepository exhibitRepo,
            IVuzRepository vuzRepo,
            IGrntiRepository grntiRepo)
        {
            var validCodvuz = vuzRepo.GetAll()
                .Where(v => !string.IsNullOrWhiteSpace(v.Codvuz)
                         && !string.IsNullOrWhiteSpace(v.Z1Full))
                .Select(v => v.Codvuz!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var validRubrics = grntiRepo.GetAll()
                .Where(g => !string.IsNullOrWhiteSpace(g.Codrub))
                .Select(g => ExtractRubric(g.Codrub!))
                .Where(r => r.Length == 2)
                .ToHashSet(StringComparer.Ordinal);

            var exhibits = exhibitRepo.GetAll();
            var kept = new List<ExhibitTableRow>(exhibits.Count);
            int removed = 0;

            foreach (var e in exhibits)
            {
                var issues = new List<string>();

                var cod = e.Codvuz?.Trim() ?? "";
                if (cod.Length == 0)
                    issues.Add("пустой код ВУЗа");
                else if (!validCodvuz.Contains(cod))
                    issues.Add($"ВУЗ '{cod}' не найден или без полного наименования");

                var missing = GetMissingRubrics(e.Grnti, validRubrics);
                if (missing.Count > 0)
                    issues.Add($"рубрики ГРНТИ не найдены: {string.Join(", ", missing)}");

                if (issues.Count == 0)
                {
                    kept.Add(e);
                }
                else
                {
                    removed++;
                    _logger.LogWarning(
                        "Выставка Id={Id} удалена. Причина: {Reason}",
                        e.Id, string.Join("; ", issues));
                }
            }

            if (removed > 0)
            {
                exhibitRepo.Clear();
                exhibitRepo.SaveAll(kept);
            }

            return removed;
        }

        private static string ExtractRubric(string code)
        {
            var trimmed = code.Trim();
            return trimmed.Length >= 2 ? trimmed.Substring(0, 2) : trimmed;
        }

        private static List<string> GetMissingRubrics(string? grntiString, HashSet<string> valid)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(grntiString)) return result;

            var parts = grntiString.Split(
                new[] { ',', ';' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var rubric = ExtractRubric(part);
                if (rubric.Length == 2
                    && !valid.Contains(rubric)
                    && !result.Contains(rubric))
                {
                    result.Add(rubric);
                }
            }
            return result;
        }
    }
}