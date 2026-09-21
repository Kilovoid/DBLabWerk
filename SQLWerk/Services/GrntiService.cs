using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SQLWerk.Services
{
    public class GrntiService
    {
        public string GrntiReader(string? codestring)
        { 
            if (codestring != null)
            {
                codestring = codestring.Trim();
                string[] codes = codestring.Split(new[] { ",", ";", " " }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < codes.Length; i++)
                {
                    codes[i] = codes[i].Trim();
                    if (!(Regex.IsMatch(codes[i], @"^\d{2}\.\d{2}\.\d{2}$") || Regex.IsMatch(codes[i], @"^\d{2}\.\d{2}$") || Regex.IsMatch(codes[i], @"^\d{2}$")))
                    {
                        Console.WriteLine($"Неверный формат");
                        return codestring;
                    }
                }

                string coderesult = string.Join(", ", codes);
                return coderesult;
            }
            return codestring;
        }
    }
}
