namespace QuanLyNhaTro.Services;

public readonly record struct BillingPeriod(int Thang, int Nam)
{
    public DateTime FirstDay => new(Nam, Thang, 1);
}

public static class DefaultBillingPeriodResolver
{
    public static BillingPeriod Resolve(DateTime? today = null)
    {
        var previousMonth = (today ?? DateTime.Today).Date.AddMonths(-1);
        return new BillingPeriod(previousMonth.Month, previousMonth.Year);
    }

    public static BillingPeriod Resolve(int? thang, int? nam, DateTime? today = null)
    {
        if (thang.HasValue && nam.HasValue)
            return new BillingPeriod(thang.Value, nam.Value);

        var fallback = Resolve(today);
        return new BillingPeriod(thang ?? fallback.Thang, nam ?? fallback.Nam);
    }
}
