using SQLWerk.Data.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQLWerk.Services
{
    public class HeaderValidator
    {
        public static bool IsMatch(string filePath, string[] expected, IExcelReader reader, out string error)
        {
            error = "";
            string[] actual;
            try
            {
                actual = reader.GetHeaders(filePath);
            }
            catch (Exception ex)
            {
                error = $"Can't open file {filePath} : {ex.Message}";
                return false;
            }

            if (actual.Length != expected.Length)
            {
                error = $"Files do not compare";
                return false;
            }

            for (int i = 0; i < expected.Length; i++)
            {
                if (!string.Equals(actual[i], expected[i]))
                {
                    error = $"{Path.GetFileName(filePath)}: wrong column {i + 1}";
                    return false;
                }
            }
            return true;
        }
    }
}
