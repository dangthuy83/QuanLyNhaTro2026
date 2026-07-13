using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public class KhachThueService(KhachThueRepository repository)
{
    public async Task<KhachThue?> TimHoSoTrungCccdAsync(string? cccd, int? excludeId = null)
    {
        var normalized = cccd?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        return await repository.GetByCccdAsync(normalized, excludeId);
    }
}
