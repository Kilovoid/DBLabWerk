using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SQLWerk.Services
{
    public class GrntiService
    {
        public static string ParseGrnti(string? codeString)
        {
            string pattern1 = @"\b(\d{2}),(\d{2}),(\d{2})\b";
            string pattern2 = @"\b(\d{2}),(\d{2})\b";

            if (codeString != null)
            {
                codeString = codeString.Trim();
                codeString = Regex.Replace(codeString, pattern1, "$1.$2.$3");
                codeString = Regex.Replace(codeString, pattern2, "$1.$2");
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
            return (Regex.IsMatch(code, @"\A\d{2}\.\d{2}\.\d{2}\z") ||
                Regex.IsMatch(code, @"\A\d{2}\.\d{2}\z") ||
                Regex.IsMatch(code, @"\A\d{2}\z"));
        }
    }
}
