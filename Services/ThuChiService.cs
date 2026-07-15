using System.Data;
using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Services;

public sealed class ThuChiService(IDbConnection db)
{
    public async Task<int> CreateAsync(ThuChi item)
    {
        NormalizeAndValidate(item);
        var conn = await OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            if (item.ThuChiGocId.HasValue)
            {
                var original = await conn.QueryFirstOrDefaultAsync<ThuChi>(
                    "SELECT * FROM ThuChi WHERE Id=@Id FOR UPDATE", new { Id = item.ThuChiGocId }, tx)
                    ?? throw new InvalidOperationException("Giao dịch gốc cần điều chỉnh không còn tồn tại.");
                ThuChiKySo? originalPeriod = null;
                var periods = new[]
                {
                    (Year: original.NgayPhatSinh.Year, Month: original.NgayPhatSinh.Month),
                    (Year: item.NgayPhatSinh.Year, Month: item.NgayPhatSinh.Month)
                }.Distinct().OrderBy(x => x.Year).ThenBy(x => x.Month);
                foreach (var period in periods)
                {
                    var locked = await LockAsync(conn, tx, period.Month, period.Year);
                    if (period.Year == original.NgayPhatSinh.Year
                        && period.Month == original.NgayPhatSinh.Month)
                        originalPeriod = locked;
                    if (period.Year == item.NgayPhatSinh.Year
                        && period.Month == item.NgayPhatSinh.Month
                        && locked.DaKhoa)
                        throw new InvalidOperationException(
                            $"Tháng {period.Month}/{period.Year} đã khóa sổ; không thể ghi bút toán điều chỉnh.");
                }
                if (originalPeriod == null)
                    throw new InvalidOperationException("Không khóa được kỳ của giao dịch gốc.");
                if (!originalPeriod.DaKhoa)
                    throw new InvalidOperationException("Chỉ tạo điều chỉnh cho giao dịch thuộc tháng đã khóa; tháng mở hãy sửa trực tiếp.");
                if (string.IsNullOrWhiteSpace(item.GhiChu) || !item.GhiChu.Contains($"#{original.Id}", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Ghi chú điều chỉnh phải tham chiếu giao dịch gốc #{original.Id}.");
            }
            else
            {
                await RequireOpenAsync(conn, tx, item.NgayPhatSinh.Month, item.NgayPhatSinh.Year);
            }

            var id = await conn.ExecuteScalarAsync<int>("""
                INSERT INTO ThuChi
                    (LoaiGiaoDich,DanhMuc,SoTien,NgayPhatSinh,NoiDung,PhongId,GhiChu,ThuChiGocId)
                VALUES
                    (@LoaiGiaoDich,@DanhMuc,@SoTien,@NgayPhatSinh,@NoiDung,@PhongId,@GhiChu,@ThuChiGocId);
                SELECT LAST_INSERT_ID();
                """, item, tx);
            await tx.CommitAsync();
            return id;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task UpdateAsync(ThuChi item)
    {
        NormalizeAndValidate(item);
        var conn = await OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var current = await conn.QueryFirstOrDefaultAsync<ThuChi>(
                "SELECT * FROM ThuChi WHERE Id=@Id FOR UPDATE", new { item.Id }, tx)
                ?? throw new KeyNotFoundException("Không tìm thấy giao dịch thu chi.");
            if (current.ThuChiGocId.HasValue)
                throw new InvalidOperationException("Bút toán điều chỉnh không được sửa; hãy tạo điều chỉnh mới nếu cần.");

            var periods = new[]
            {
                (Year: current.NgayPhatSinh.Year, Month: current.NgayPhatSinh.Month),
                (Year: item.NgayPhatSinh.Year, Month: item.NgayPhatSinh.Month)
            }.Distinct().OrderBy(x => x.Year).ThenBy(x => x.Month);
            foreach (var period in periods)
                await RequireOpenAsync(conn, tx, period.Month, period.Year);

            await conn.ExecuteAsync("""
                UPDATE ThuChi SET LoaiGiaoDich=@LoaiGiaoDich,DanhMuc=@DanhMuc,
                    SoTien=@SoTien,NgayPhatSinh=@NgayPhatSinh,NoiDung=@NoiDung,
                    PhongId=@PhongId,GhiChu=@GhiChu
                WHERE Id=@Id
                """, item, tx);
            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task DeleteAsync(int id)
    {
        var conn = await OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var current = await conn.QueryFirstOrDefaultAsync<ThuChi>(
                "SELECT * FROM ThuChi WHERE Id=@Id FOR UPDATE", new { Id = id }, tx)
                ?? throw new KeyNotFoundException("Không tìm thấy giao dịch thu chi.");
            if (current.ThuChiGocId.HasValue)
                throw new InvalidOperationException("Bút toán điều chỉnh không được xóa.");
            if (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ThuChi WHERE ThuChiGocId=@Id", new { Id = id }, tx) > 0)
                throw new InvalidOperationException("Giao dịch đã có bút toán điều chỉnh nên không được xóa.");
            await RequireOpenAsync(conn, tx, current.NgayPhatSinh.Month, current.NgayPhatSinh.Year);
            await conn.ExecuteAsync("DELETE FROM ThuChi WHERE Id=@Id", new { Id = id }, tx);
            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task LockPeriodAsync(int thang, int nam, string? ghiChu)
    {
        if (!BusinessDataLimits.IsValidPeriod(thang, nam)) throw new InvalidOperationException("Kỳ khóa sổ không hợp lệ.");
        var conn = await OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var period = await LockAsync(conn, tx, thang, nam);
            if (period.DaKhoa) throw new InvalidOperationException($"Tháng {thang}/{nam} đã khóa sổ.");
            await conn.ExecuteAsync("""
                UPDATE ThuChiKySo SET TrangThai='DaKhoa',KhoaLuc=NOW(),GhiChu=@GhiChu
                WHERE Thang=@Thang AND Nam=@Nam
                """, new { Thang = thang, Nam = nam, GhiChu = Normalize(ghiChu) }, tx);
            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    private async Task<MySqlConnection> OpenAsync()
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        return conn;
    }

    private static async Task RequireOpenAsync(MySqlConnection conn, MySqlTransaction tx, int thang, int nam)
    {
        var period = await LockAsync(conn, tx, thang, nam);
        if (period.DaKhoa)
            throw new InvalidOperationException($"Tháng {thang}/{nam} đã khóa sổ; không thể ghi, sửa, xóa hoặc chuyển ngày giao dịch vào/ra kỳ này.");
    }

    private static async Task<ThuChiKySo> LockAsync(MySqlConnection conn, MySqlTransaction tx, int thang, int nam)
    {
        await conn.ExecuteAsync("""
            INSERT INTO ThuChiKySo(Nam,Thang,TrangThai) VALUES(@Nam,@Thang,'Mo')
            ON DUPLICATE KEY UPDATE Nam=VALUES(Nam)
            """, new { Nam = nam, Thang = thang }, tx);
        return await conn.QuerySingleAsync<ThuChiKySo>(
            "SELECT * FROM ThuChiKySo WHERE Nam=@Nam AND Thang=@Thang FOR UPDATE",
            new { Nam = nam, Thang = thang }, tx);
    }

    private static void NormalizeAndValidate(ThuChi item)
    {
        item.LoaiGiaoDich = item.LoaiGiaoDich?.Trim() ?? string.Empty;
        item.DanhMuc = item.DanhMuc?.Trim() ?? string.Empty;
        item.NoiDung = Normalize(item.NoiDung);
        item.GhiChu = Normalize(item.GhiChu);
        item.NgayPhatSinh = item.NgayPhatSinh.Date;
        if (item.LoaiGiaoDich is not ("Thu" or "Chi")) throw new InvalidOperationException("Loại giao dịch không hợp lệ.");
        if (string.IsNullOrWhiteSpace(item.DanhMuc)) throw new InvalidOperationException("Danh mục là bắt buộc.");
        if (item.SoTien <= 0) throw new InvalidOperationException("Số tiền phải lớn hơn 0.");
        if (!BusinessDataLimits.IsValidBusinessDate(item.NgayPhatSinh)) throw new InvalidOperationException("Ngày giao dịch không hợp lệ.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
