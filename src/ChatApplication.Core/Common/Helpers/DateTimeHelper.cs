namespace ChatApplication.Core.Common.Helpers;

public static class DateTimeHelper
{
    public static DateTime UtcNow() => DateTime.UtcNow;
    public static string ToIso8601(DateTime dt) => dt.ToString("o");
}
