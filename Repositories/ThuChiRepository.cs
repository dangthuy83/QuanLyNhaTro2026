using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class ThuChiRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<ThuChi>> GetAllAsync(
        int? nam = null, int? thang = null, string? loai = null)
    {
        var sql = "SELECT * FROM ThuChi WHERE 1=1";
        var p = new DynamicParameters();

        if (nam.HasValue)   { sql += " AND YEAR(NgayPhatSinh) = @Nam";    p.Add("Nam", nam); }
        if (thang.HasValue) { sql += " AND MONTH(NgayPhatSinh) = @Thang"; p.Add("Thang", thang); }
        if (!string.IsNullOrEmpty(loai)) { sql += " AND LoaiGiaoDich = @Loai"; p.Add("Loai", loai); }

        sql += " ORDER BY NgayPhatSinh DESC";
        return await _db.QueryAsync<ThuChi>(sql, p);
    }

    public async Task<ThuChi?> GetByIdAsync(int id)
        => await _db.QueryFirstOrDefaultAsync<ThuChi>(
            "SELECT * FROM ThuChi WHERE Id = @Id", new { Id = id });

    public async Task<ThuChiKySo?> GetKySoAsync(int thang, int nam)
        => await _db.QueryFirstOrDefaultAsync<ThuChiKySo>(
            "SELECT * FROM ThuChiKySo WHERE Thang=@Thang AND Nam=@Nam",
            new { Thang = thang, Nam = nam });

    public async Task<int> InsertAsync(ThuChi tc)
    {
        const string sql = """
            INSERT INTO ThuChi
                (LoaiGiaoDich, DanhMuc, SoTien, NgayPhatSinh, NoiDung, PhongId, GhiChu)
            VALUES
                (@LoaiGiaoDich, @DanhMuc, @SoTien, @NgayPhatSinh, @NoiDung, @PhongId, @GhiChu);
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, tc);
    }

    public async Task UpdateAsync(ThuChi tc)
    {
        const string sql = """
            UPDATE ThuChi SET
                LoaiGiaoDich = @LoaiGiaoDich, DanhMuc = @DanhMuc,
                SoTien = @SoTien, NgayPhatSinh = @NgayPhatSinh,
                NoiDung = @NoiDung, PhongId = @PhongId, GhiChu = @GhiChu
            WHERE Id = @Id
            """;
        await _db.ExecuteAsync(sql, tc);
    }

    public async Task DeleteAsync(int id)
        => await _db.ExecuteAsync("DELETE FROM ThuChi WHERE Id = @Id", new { Id = id });

    /// <summary>Tổng thu / chi theo tháng — dùng cho Dashboard và báo cáo.</summary>
    public async Task<(decimal TongThu, decimal TongChi)> GetTongTheoThangAsync(int thang, int nam)
    {
        const string sql = """
            SELECT LoaiGiaoDich, COALESCE(SUM(SoTien), 0) AS Tong
            FROM ThuChi
            WHERE MONTH(NgayPhatSinh) = @Thang AND YEAR(NgayPhatSinh) = @Nam
            GROUP BY LoaiGiaoDich
            """;
        var rows = await _db.QueryAsync<(string Loai, decimal Tong)>(sql, new { Thang = thang, Nam = nam });
        decimal thu = 0, chi = 0;
        foreach (var r in rows)
        {
            if (r.Loai == "Thu") thu = r.Tong;
            else chi = r.Tong;
        }
        return (thu, chi);
    }
}
