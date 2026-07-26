namespace QuanLyNhaTro.Services;

public static class MeterReadingWindowPolicy
{
    public static (DateTime Start, DateTime End) RegularWindow(int thang, int nam)
    {
        if (!Models.BusinessDataLimits.IsValidPeriod(thang, nam))
            throw new InvalidOperationException("Kỳ sử dụng chỉ số không hợp lệ.");
        var start = new DateTime(nam, thang, 1).AddMonths(1);
        return (start, start.AddDays(4));
    }

    public static bool IsRegularReadDate(DateTime ngayDoc, int thang, int nam)
    {
        var window = RegularWindow(thang, nam);
        return ngayDoc.Date >= window.Start && ngayDoc.Date <= window.End;
    }
}
