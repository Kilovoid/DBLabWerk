using SQLWerk.Data.Abstractions;
using SQLWerk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLWerk.Services
{
    internal static class ValidationService
    {
        public static (bool IsValid, string? Reason) Validate<T>(T row) where T : ITableRow
        {
            return row switch
            {
                ExhibitTableRow e => IsValidExhibit(e),

                VuzTableRow v => IsValidVuz(v),

                GrntiTableRow g => IsValidGrnti(g)
            };
        }
        private static (bool, string?) IsValidExhibit(ExhibitTableRow row)
        {
            var issues = new List<string>();
            if (string.IsNullOrEmpty(row.Codvuz)) 
                issues.Add("Пустое поле Кода Вуза");
            if (string.IsNullOrEmpty(row.Grnti)) 
                issues.Add("Пустое поле ГРНТИ");
            if (!GrntiService.IsAllValid(row.Grnti))
                issues.Add($"Некорректный грнти: {row.Grnti}");

            return issues.Count == 0
                ? (true, null)
                : (false, string.Join(";", issues));
        }

        private static (bool, string?) IsValidVuz(VuzTableRow row)
        {
            var issues = new List<string>();

            if (string.IsNullOrWhiteSpace(row.Codvuz)) 
                issues.Add("Пустое поле codvuz");
            if (string.IsNullOrWhiteSpace(row.Obl) || !row.Obl.All(char.IsDigit))
                issues.Add("Поле obl пустое или содержит не цифры");

            void CheckRu(string? val, string name)
            {
                if (string.IsNullOrWhiteSpace(val)) issues.Add($"Пустое поле {name}");
                else if (ContainsLatin(val)) issues.Add($"Поле {name} содержит латиницу");
            }

            CheckRu(row.Z1, "z1");
            CheckRu(row.Z1Full, "z1full");
            CheckRu(row.Z2, "z2");
            CheckRu(row.Region, "region");
            CheckRu(row.City, "city");
            CheckRu(row.Status, "status");
            CheckRu(row.OblName, "oblname");

            return issues.Count == 0
                ? (true, null)
                : (false, string.Join("; ", issues));
        }
        private static (bool, string?) IsValidGrnti(GrntiTableRow row)
        {
            var issues = new List<string>();

            if (string.IsNullOrWhiteSpace(row.Codrub))
                issues.Add("Пустое поле ГРНТИ");
            if (string.IsNullOrWhiteSpace(row.Rubrika))
                issues.Add("Пустое поле рубрики");

            return issues.Count == 0 ? (true, null)
                : (false, string.Join("; ", issues));
        }
        public static bool ContainsLatin(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            foreach (char c in text)
            {
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                    return true;
            }

            return false;
        }
    }
}
