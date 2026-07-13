using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class KhachThueRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<KhachThue>> GetAllAsync()
        => await _db.QueryAsync<KhachThue>("SELECT * FROM KhachThue ORDER BY HoTen");

    public async Task<IEnumerable<KhachThue>> GetForIndexAsync(
        string? tuKhoa,
        string? trangThai,
        int? phongId)
    {
        const string sql = """
            SELECT kt.*,
                (
                    SELECT CONCAT(n.TenNha, ' / ', p.TenPhong)
                    FROM HopDongKhachThue x
                    INNER JOIN HopDong hd ON hd.Id = x.HopDongId
                    INNER JOIN Phong p ON p.Id = hd.PhongId
                    INNER JOIN Nha n ON n.Id = p.NhaId
                    WHERE x.KhachThueId = kt.Id
                      AND x.NgayBatDau <= CURDATE()
                      AND (x.NgayKetThuc IS NULL OR x.NgayKetThuc >= CURDATE())
                    ORDER BY x.NgayBatDau DESC, x.Id DESC LIMIT 1
                ) AS PhongHienTai,
                (
                    SELECT x.NgayKetThucDuKien
                    FROM HopDongKhachThue x
                    WHERE x.KhachThueId = kt.Id
                      AND x.NgayBatDau <= CURDATE()
                      AND (x.NgayKetThuc IS NULL OR x.NgayKetThuc >= CURDATE())
                    ORDER BY x.NgayBatDau DESC, x.Id DESC LIMIT 1
                ) AS NgayKetThucDuKienHienTai,
                CASE
                    WHEN EXISTS(SELECT 1 FROM HopDongKhachThue x WHERE x.KhachThueId=kt.Id AND x.NgayBatDau<=CURDATE() AND (x.NgayKetThuc IS NULL OR x.NgayKetThuc>=CURDATE())) THEN 'DangO'
                    WHEN EXISTS(SELECT 1 FROM HopDongKhachThue x WHERE x.KhachThueId=kt.Id AND x.NgayBatDau>CURDATE()) THEN 'SapDen'
                    WHEN EXISTS(SELECT 1 FROM HopDongKhachThue x WHERE x.KhachThueId=kt.Id) THEN 'DaRoi'
                    ELSE 'ChuaCuTru'
                END AS TrangThaiCuTru
            FROM KhachThue kt
            WHERE (@TuKhoa IS NULL OR @TuKhoa = ''
                   OR kt.HoTen LIKE CONCAT('%', @TuKhoa, '%')
                   OR kt.SoDienThoai LIKE CONCAT('%', @TuKhoa, '%')
                   OR kt.CCCD LIKE CONCAT('%', @TuKhoa, '%')
                   OR kt.BienSoXe LIKE CONCAT('%', @TuKhoa, '%'))
              AND (@TrangThai IS NULL OR @TrangThai = '' OR @TrangThai = 'TatCa' OR
                   CASE
                       WHEN EXISTS(SELECT 1 FROM HopDongKhachThue x WHERE x.KhachThueId=kt.Id AND x.NgayBatDau<=CURDATE() AND (x.NgayKetThuc IS NULL OR x.NgayKetThuc>=CURDATE())) THEN 'DangO'
                       WHEN EXISTS(SELECT 1 FROM HopDongKhachThue x WHERE x.KhachThueId=kt.Id AND x.NgayBatDau>CURDATE()) THEN 'SapDen'
                       WHEN EXISTS(SELECT 1 FROM HopDongKhachThue x WHERE x.KhachThueId=kt.Id) THEN 'DaRoi'
                       ELSE 'ChuaCuTru'
                   END = @TrangThai)
              AND (@PhongId IS NULL OR EXISTS(
                    SELECT 1 FROM HopDongKhachThue x
                    INNER JOIN HopDong hd ON hd.Id=x.HopDongId
                    WHERE x.KhachThueId=kt.Id AND hd.PhongId=@PhongId
                      AND x.NgayBatDau<=CURDATE()
                      AND (x.NgayKetThuc IS NULL OR x.NgayKetThuc>=CURDATE())))
            ORDER BY kt.HoTen, kt.Id
            """;
        return await _db.QueryAsync<KhachThue>(sql, new { TuKhoa = tuKhoa?.Trim(), TrangThai = trangThai, PhongId = phongId });
    }

    public async Task<IEnumerable<KhachThue>> SearchAsync(string? term, int limit = 20)
    {
        term = term?.Trim();
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2) return [];
        const string sql = """
            SELECT kt.*
            FROM KhachThue kt
            WHERE kt.HoTen LIKE CONCAT('%', @Term, '%')
               OR kt.SoDienThoai LIKE CONCAT('%', @Term, '%')
               OR kt.CCCD LIKE CONCAT('%', @Term, '%')
               OR kt.BienSoXe LIKE CONCAT('%', @Term, '%')
            ORDER BY CASE WHEN kt.HoTen LIKE CONCAT(@Term, '%') THEN 0 ELSE 1 END, kt.HoTen, kt.Id
            LIMIT @Limit
            """;
        return await _db.QueryAsync<KhachThue>(sql, new { Term = term, Limit = Math.Clamp(limit, 1, 20) });
    }

    public async Task<IEnumerable<KhachThue>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var values = ids.Distinct().ToArray();
        if (values.Length == 0) return [];
        return await _db.QueryAsync<KhachThue>(
            "SELECT * FROM KhachThue WHERE Id IN @Ids ORDER BY HoTen", new { Ids = values });
    }

    public async Task<KhachThue?> GetByCccdAsync(string cccd, int? excludeId = null)
        => await _db.QueryFirstOrDefaultAsync<KhachThue>(
            """
            SELECT * FROM KhachThue
            WHERE CCCD = @CCCD AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
            ORDER BY Id LIMIT 1
            """,
            new { CCCD = cccd.Trim(), ExcludeId = excludeId });

    public async Task<KhachThue?> GetByIdAsync(int id)
        => await _db.QueryFirstOrDefaultAsync<KhachThue>(
            "SELECT * FROM KhachThue WHERE Id = @Id", new { Id = id });

    public async Task<IEnumerable<KhachThue>> GetByHopDongAsync(int hopDongId)
    {
        const string sql = """
            SELECT DISTINCT kt.* FROM KhachThue kt
            INNER JOIN HopDongKhachThue hdkt ON hdkt.KhachThueId = kt.Id
            WHERE hdkt.HopDongId = @HopDongId
            ORDER BY kt.HoTen
            """;
        return await _db.QueryAsync<KhachThue>(sql, new { HopDongId = hopDongId });
    }

    public async Task<int> InsertAsync(KhachThue khach)
    {
        const string sql = """
            INSERT INTO KhachThue
                (HoTen, CCCD, SoDienThoai, NgaySinh, NgayCapCCCD, NgheNghiep, LoaiXe, BienSoXe,
                 QueQuan, AnhCCCDMatTruoc, AnhCCCDMatSau, GhiChu, NgayTao)
            VALUES
                (@HoTen, @CCCD, @SoDienThoai, @NgaySinh, @NgayCapCCCD, @NgheNghiep, @LoaiXe, @BienSoXe,
                 @QueQuan, @AnhCCCDMatTruoc, @AnhCCCDMatSau, @GhiChu, NOW());
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, khach);
    }

    public async Task UpdateAsync(KhachThue khach)
    {
        const string sql = """
            UPDATE KhachThue SET
                HoTen = @HoTen,
                CCCD = @CCCD,
                SoDienThoai = @SoDienThoai,
                NgaySinh = @NgaySinh,
                NgayCapCCCD = @NgayCapCCCD,
                NgheNghiep = @NgheNghiep,
                LoaiXe = @LoaiXe,
                BienSoXe = @BienSoXe,
                QueQuan = @QueQuan,
                AnhCCCDMatTruoc = @AnhCCCDMatTruoc,
                AnhCCCDMatSau = @AnhCCCDMatSau,
                GhiChu = @GhiChu
            WHERE Id = @Id
            """;
        await _db.ExecuteAsync(sql, khach);
    }

    public async Task DeleteAsync(int id)
        => await _db.ExecuteAsync("DELETE FROM KhachThue WHERE Id = @Id", new { Id = id });
}
