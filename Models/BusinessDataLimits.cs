namespace QuanLyNhaTro.Models;

public static class BusinessDataLimits
{
    public const int MinYear = 2000;
    public const int MaxYear = 2100;

    public static bool IsValidPeriod(int month, int year)
        => month is >= 1 and <= 12 && year is >= MinYear and <= MaxYear;

    public static bool IsValidBusinessDate(DateTime date)
        => date.Year is >= MinYear and <= MaxYear;
}
