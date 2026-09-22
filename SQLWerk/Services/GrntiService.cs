using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SQLWerk.Services
{
    public class GrntiService
    {
        public string ParseGrnti(string? codeString)
        { 
            if (codeString != null)
            {
                codeString = codeString.Trim();
                string[] codes = codeString.Split(new[] { ",", ";", " " }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < codes.Length; i++)
                {
                    codes[i] = codes[i].Trim();
                    if (!(IsValid(codes[i])))
                    {
                        return codeString;
                    }
                }

                string codeResult = string.Join(", ", codes);
                return codeResult;
            }
            return codeString;
        }

        public static bool IsValid(string code)
        {
            return (Regex.IsMatch(code, @"^\d{2}\.\d{2}\.\d{2}$") ||
                Regex.IsMatch(code, @"^\d{2}\.\d{2}$") ||
                Regex.IsMatch(code, @"^\d{2}$"));
        }
    }
}
