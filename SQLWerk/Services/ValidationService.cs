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
        public static bool IsValid<T>(T row) where T : ITableRow
        {
            return row switch
            {
                ExhibitTableRow e => IsValidExhibit(e),

                VuzTableRow v => IsValidVuz(v),

                GrntiTableRow g => IsValidGrnti(g)
            };
        }
        private static bool IsValidExhibit(ExhibitTableRow row)
        {
            if (string.IsNullOrEmpty(row.Codvuz)) return false;
            if (string.IsNullOrEmpty(row.Grnti)) return false;
            return true;
        }

        private static bool IsValidVuz(VuzTableRow row)
        {
            if (string.IsNullOrEmpty(row.Codvuz) || !(row.Obl.All(char.IsDigit)))
            {
                return false;
            }
            if (string.IsNullOrEmpty(row.Z1) || ValidationService.ContainsLatin(row.Z1))
            {
                return false;
            }
            if (string.IsNullOrEmpty(row.Z1Full) || ValidationService.ContainsLatin(row.Z1Full))
            {
                return false;
            }
            if (string.IsNullOrEmpty(row.Z2) || ValidationService.ContainsLatin(row.Z2))
            {
                return false;
            }
            if (string.IsNullOrEmpty(row.Region) || ValidationService.ContainsLatin(row.Region))
            {
                return false;
            }
            if (string.IsNullOrEmpty(row.City) || ValidationService.ContainsLatin(row.City))
            {
                return false;
            }
            if (string.IsNullOrEmpty(row.Status) || ValidationService.ContainsLatin(row.Status))
            {
                return false;
            }
            if (string.IsNullOrEmpty(row.Obl) || !(row.Obl.All(char.IsDigit)))
            {
                return false;
            }
            if (string.IsNullOrEmpty(row.OblName) || ValidationService.ContainsLatin(row.OblName))
            {
                return false;
            }
            return true;
        }
        private static bool IsValidGrnti(GrntiTableRow row)
        {
            
            return true;
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
