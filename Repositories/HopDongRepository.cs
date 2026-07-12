using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class HopDongRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<HopDong>> GetAllAsync()
    {
        const string sql = """
            SELECT hd.*, p.Id, p.NhaId, p.TenPhong, p.TrangThai
            FROM HopDong hd
            INNER JOIN Phong p ON p.Id = hd.PhongId
            ORDER BY hd.NgayBatDau DESC
            """;
        return await _db.QueryAsync<HopDong, Phong, HopDong>(
            sql,
            (hd, p) => { hd.Phong = p; return hd; },
            splitOn: "Id");
    }

    public async Task<HopDong?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT hd.*, p.Id, p.NhaId, p.TenPhong, p.GiaThueMacDinh, p.TrangThai
            FROM HopDong hd
            INNER JOIN Phong p ON p.Id = hd.PhongId
            WHERE hd.Id = @Id
            """;
        var rows = await _db.QueryAsync<HopDong, Phong, HopDong>(
            sql,
            (hd, p) => { hd.Phong = p; return hd; },
            new { Id = id },
            splitOn: "Id");
        return rows.FirstOrDefault();
    }

    /// <summary>Hợp đồng đang hiệu lực của 1 phòng</summary>
    public async Task<HopDong?> GetDangHieuLucByPhongAsync(int phongId)
        => await _db.QueryFirstOrDefaultAsync<HopDong>(
            "SELECT * FROM HopDong WHERE PhongId = @PhongId AND TrangThai = 'DangHieuLuc' LIMIT 1",
            new { PhongId = phongId });

    public async Task<HopDong?> GetDangHieuLucByPhongAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int phongId)
        => await conn.QueryFirstOrDefaultAsync<HopDong>(
            "SELECT * FROM HopDong WHERE PhongId = @PhongId AND TrangThai = 'DangHieuLuc' LIMIT 1",
            new { PhongId = phongId },
            transaction: tx);

    public async Task<bool> CoChongKhoangAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int phongId,
        DateTime ngayBatDau,
        DateTime? ngayKetThuc,
        int? excludeId = null)
        => await conn.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM HopDong
                WHERE PhongId = @PhongId
                  AND TrangThai <> 'DaHuy'
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
                  AND NgayBatDau <= COALESCE(@NgayKetThuc, '9999-12-31')
                  AND COALESCE(NgayKetThuc, '9999-12-31') >= @NgayBatDau)
            """,
            new { PhongId = phongId, NgayBatDau = ngayBatDau.Date, NgayKetThuc = ngayKetThuc?.Date, ExcludeId = excludeId },
            transaction: tx);

    public async Task<HopDong?> GetByPhongAndDateAsync(int phongId, DateTime ngay)
        => await _db.QueryFirstOrDefaultAsync<HopDong>(
            """
            SELECT *
            FROM HopDong
            WHERE PhongId = @PhongId
              AND TrangThai <> 'DaHuy'
              AND NgayBatDau <= @Ngay
              AND (NgayKetThuc IS NULL OR NgayKetThuc >= @Ngay)
            ORDER BY NgayBatDau DESC, Id DESC
            LIMIT 1
            """,
            new { PhongId = phongId, Ngay = ngay.Date });

    public async Task<int> InsertAsync(HopDong hd)
    {
        const string sql = """
            INSERT INTO HopDong
                (PhongId, NgayBatDau, NgayKetThuc, TienThueThoaThuan, TienCoc,
                 TrangThai, HopDongTruocId, DaXuLyChenhLechCoc, GhiChu, NgayTao)
            VALUES
                (@PhongId, @NgayBatDau, @NgayKetThuc, @TienThueThoaThuan, @TienCoc,
                 @TrangThai, @HopDongTruocId, @DaXuLyChenhLechCoc, @GhiChu, NOW());
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, hd);
    }

    public async Task<int> InsertAsync(IDbConnection conn, IDbTransaction tx, HopDong hd)
    {
        const string sql = """
            INSERT INTO HopDong
                (PhongId, NgayBatDau, NgayKetThuc, TienThueThoaThuan, TienCoc,
                 TrangThai, HopDongTruocId, DaXuLyChenhLechCoc, GhiChu, NgayTao)
            VALUES
                (@PhongId, @NgayBatDau, @NgayKetThuc, @TienThueThoaThuan, @TienCoc,
                 @TrangThai, @HopDongTruocId, @DaXuLyChenhLechCoc, @GhiChu, NOW());
            SELECT LAST_INSERT_ID();
            """;
        return await conn.ExecuteScalarAsync<int>(sql, hd, transaction: tx);
    }

    public async Task UpdateAsync(HopDong hd)
    {
        const string sql = """
            UPDATE HopDong SET
                NgayBatDau = @NgayBatDau, NgayKetThuc = @NgayKetThuc,
                TienThueThoaThuan = @TienThueThoaThuan, TienCoc = @TienCoc,
                TrangThai = @TrangThai, DaXuLyChenhLechCoc = @DaXuLyChenhLechCoc,
                GhiChu = @GhiChu
            WHERE Id = @Id
            """;
        await _db.ExecuteAsync(sql, hd);
    }

    public async Task UpdateAsync(IDbConnection conn, IDbTransaction tx, HopDong hd)
    {
        const string sql = """
            UPDATE HopDong SET
                NgayBatDau = @NgayBatDau, NgayKetThuc = @NgayKetThuc,
                TienThueThoaThuan = @TienThueThoaThuan, TienCoc = @TienCoc,
                TrangThai = @TrangThai, DaXuLyChenhLechCoc = @DaXuLyChenhLechCoc,
                GhiChu = @GhiChu
            WHERE Id = @Id
            """;
        await conn.ExecuteAsync(sql, hd, transaction: tx);
    }

    public async Task UpdateEditableAsync(IDbConnection conn, IDbTransaction tx, HopDong hd)
        => await conn.ExecuteAsync(
            """
            UPDATE HopDong SET
                NgayBatDau = @NgayBatDau,
                NgayKetThuc = @NgayKetThuc,
                TienCoc = @TienCoc,
                GhiChu = @GhiChu
            WHERE Id = @Id
            """,
            hd,
            transaction: tx);

    public async Task UpdateGhiChuAsync(IDbConnection conn, IDbTransaction tx, int id, string? ghiChu)
        => await conn.ExecuteAsync(
            "UPDATE HopDong SET GhiChu = @GhiChu WHERE Id = @Id",
            new { Id = id, GhiChu = ghiChu },
            transaction: tx);

    public async Task UpdateTrangThaiAsync(int id, string trangThai)
        => await _db.ExecuteAsync(
            "UPDATE HopDong SET TrangThai = @TrangThai WHERE Id = @Id",
            new { Id = id, TrangThai = trangThai });
}
