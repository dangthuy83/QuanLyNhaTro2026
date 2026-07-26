using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Services;

public readonly record struct CollectionPeriods(
    DateTime KyThu,
    DateTime KyTienPhong,
    DateTime KyDichVu,
    DateTime NgayDenHan)
{
    public int Thang => KyThu.Month;
    public int Nam => KyThu.Year;
}

public static class BillingCollectionPeriodPolicy
{
    public static readonly DateTime CutoverPeriod = new(2026, 8, 1);

    public static CollectionPeriods Resolve(DateTime kyThu)
    {
        var normalized = NormalizeMonth(kyThu, nameof(kyThu));
        if (normalized < CutoverPeriod)
            throw new InvalidOperationException(
                "Chính sách thu tiền phòng trước chỉ áp dụng từ kỳ thu 08/2026.");

        return new CollectionPeriods(
            normalized,
            normalized,
            normalized.AddMonths(-1),
            normalized.AddDays(9));
    }

    public static CollectionPeriods Resolve(int thang, int nam)
    {
        if (!BusinessDataLimits.IsValidPeriod(thang, nam))
            throw new InvalidOperationException("Kỳ thu phải nằm trong dải 01/2000 đến 12/2100.");
        return Resolve(new DateTime(nam, thang, 1));
    }

    public static CollectionPeriods Resolve(int? thang, int? nam, DateTime? today = null)
    {
        if (thang.HasValue || nam.HasValue)
        {
            var fallback = DefaultCollectionPeriod(today);
            return Resolve(thang ?? fallback.Month, nam ?? fallback.Year);
        }
        var period = DefaultCollectionPeriod(today);
        return Resolve(period);
    }

    public static DateTime DefaultCollectionPeriod(DateTime? today = null)
    {
        var current = new DateTime(
            (today ?? DateTime.Today).Year,
            (today ?? DateTime.Today).Month,
            1);
        return current < CutoverPeriod ? CutoverPeriod : current;
    }

    public static CollectionPeriods ResolveSettlement(DateTime eventDate)
    {
        var month = new DateTime(eventDate.Year, eventDate.Month, 1);
        if (month < CutoverPeriod)
            throw new InvalidOperationException(
                "Quyết toán theo chính sách mới chỉ áp dụng từ 08/2026.");
        return new CollectionPeriods(month, month, month, eventDate.Date);
    }

    public static CollectionPeriods Validate(HoaDon hoaDon)
    {
        var expected = hoaDon.LoaiHoaDon == "DinhKy"
            ? Resolve(hoaDon.KyThu)
            : new CollectionPeriods(
                NormalizeMonth(hoaDon.KyThu, nameof(hoaDon.KyThu)),
                NormalizeMonth(hoaDon.KyTienPhong, nameof(hoaDon.KyTienPhong)),
                NormalizeMonth(hoaDon.KyDichVu, nameof(hoaDon.KyDichVu)),
                hoaDon.NgayDenHan);

        if (hoaDon.KyTienPhong != expected.KyTienPhong
            || (hoaDon.LoaiHoaDon == "DinhKy" && hoaDon.KyDichVu != expected.KyDichVu)
            || (hoaDon.LoaiHoaDon != "DinhKy" && hoaDon.KyDichVu != hoaDon.KyTienPhong))
            throw new InvalidOperationException("Ba kỳ của hóa đơn không khớp loại nghiệp vụ.");
        return expected;
    }

    public static DateTime NormalizeMonth(DateTime value, string fieldName)
    {
        if (value.Day != 1)
            throw new InvalidOperationException($"{fieldName} phải là ngày đầu tháng.");
        return value.Date;
    }
}
