namespace WhereToStayInJapan.Shared.Extensions;

public static class StringExtensions
{
    public static string NormalizeKey(this string value) =>
        value
            .ToLowerInvariant()
            .Trim()
            .Replace("  ", " ")
            .Replace("'", "")
            .Replace(".", "")
            .Replace(",", "");
}
