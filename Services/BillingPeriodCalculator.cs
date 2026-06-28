namespace QuanLyNhaTro.Services;

public static class BillingPeriodCalculator
{
    public static int GetDaysInMonth(int thang, int nam)
        => DateTime.DaysInMonth(nam, thang);

    public static int CountOccupiedDays(
        int thang,
        int nam,
        DateTime ngayBatDauTinhTien,
        DateTime? ngayKetThucTinhTien)
    {
        var periodStart = new DateTime(nam, thang, 1);
        var periodEndExclusive = periodStart.AddMonths(1);

        var startInclusive = MaxDate(periodStart, ngayBatDauTinhTien.Date);
        var endExclusive = ngayKetThucTinhTien.HasValue
            ? MinDate(periodEndExclusive, ngayKetThucTinhTien.Value.Date.AddDays(1))
            : periodEndExclusive;

        return Math.Max(0, (endExclusive - startInclusive).Days);
    }

    public static decimal CalculateRoomCharge(decimal giaThang, int soNgayO, int soNgayTrongThang)
    {
        if (soNgayTrongThang <= 0)
            throw new ArgumentOutOfRangeException(nameof(soNgayTrongThang), "So ngay trong thang phai lon hon 0.");

        if (soNgayO < 0 || soNgayO > soNgayTrongThang)
            throw new ArgumentOutOfRangeException(nameof(soNgayO), "So ngay o phai nam trong thang lap hoa don.");

        return soNgayO == soNgayTrongThang
            ? giaThang
            : Math.Round(giaThang / soNgayTrongThang * soNgayO, 0);
    }

    private static DateTime MaxDate(DateTime a, DateTime b)
        => a >= b ? a : b;

    private static DateTime MinDate(DateTime a, DateTime b)
        => a <= b ? a : b;
}
