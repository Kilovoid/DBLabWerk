namespace SQLWerk.Models;
public sealed record VuzCodeSuggestion(string Code, string ShortName)
{
    public string Label => string.IsNullOrWhiteSpace(ShortName) ? Code : $"{Code} — {ShortName}";
}
