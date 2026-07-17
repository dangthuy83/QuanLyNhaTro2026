using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class HopDongKhachThueRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<HopDongKhachThue>> GetByHopDongAsync(int hopDongId)
    {
        const string sql = """
            SELECT hdkt.*, kt.Id, kt.HoTen, kt.SoDienThoai, kt.CCCD
            FROM HopDongKhachThue hdkt
            INNER JOIN KhachThue kt ON kt.Id = hdkt.KhachThueId
            WHERE hdkt.HopDongId = @HopDongId
            ORDER BY hdkt.NgayBatDau DESC, hdkt.LaDaiDien DESC, kt.HoTen
            """;
        return await _db.QueryAsync<HopDongKhachThue, KhachThue, HopDongKhachThue>(
            sql,
            (hdkt, kt) => { hdkt.KhachThue = kt; return hdkt; },
            new { HopDongId = hopDongId },
            splitOn: "Id");
    }

    public async Task InsertAsync(
        int hopDongId,
        int khachThueId,
        DateTime ngayBatDau,
        DateTime? ngayKetThucDuKien,
        bool laKhachChinh = false)
    {
        const string sql = """
            INSERT INTO HopDongKhachThue
                (HopDongId, KhachThueId, NgayBatDau, NgayKetThucDuKien, NgayKetThuc, LaDaiDien)
            VALUES
                (@HopDongId, @KhachThueId, @NgayBatDau, @NgayKetThucDuKien, NULL, @LaDaiDien)
            """;
        await _db.ExecuteAsync(sql, new
        {
            HopDongId = hopDongId,
            KhachThueId = khachThueId,
            NgayBatDau = ngayBatDau.Date,
            NgayKetThucDuKien = ngayKetThucDuKien?.Date,
            LaDaiDien = laKhachChinh
        });
    }

    public async Task InsertAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int hopDongId,
        int khachThueId,
        DateTime ngayBatDau,
        DateTime? ngayKetThucDuKien,
        bool laKhachChinh = false)
    {
        const string sql = """
            INSERT INTO HopDongKhachThue
                (HopDongId, KhachThueId, NgayBatDau, NgayKetThucDuKien, NgayKetThuc, LaDaiDien)
            VALUES
                (@HopDongId, @KhachThueId, @NgayBatDau, @NgayKetThucDuKien, NULL, @LaDaiDien)
            """;
        await conn.ExecuteAsync(
            sql,
            new
            {
                HopDongId = hopDongId,
                KhachThueId = khachThueId,
                NgayBatDau = ngayBatDau.Date,
                NgayKetThucDuKien = ngayKetThucDuKien?.Date,
                LaDaiDien = laKhachChinh
            },
            transaction: tx);
    }

    public async Task InsertAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int hopDongId,
        CuTruMoSoInput cuTru)
    {
        const string sql = """
            INSERT INTO HopDongKhachThue
                (HopDongId, KhachThueId, NgayBatDau, NgayKetThucDuKien, NgayKetThuc, LaDaiDien)
            VALUES
                (@HopDongId, @KhachThueId, @NgayBatDau, @NgayKetThucDuKien, @NgayKetThuc, @LaDaiDien)
            """;
        await conn.ExecuteAsync(sql, new
        {
            HopDongId = hopDongId,
            cuTru.KhachThueId,
            NgayBatDau = cuTru.NgayBatDau.Date,
            NgayKetThucDuKien = cuTru.NgayKetThucDuKien?.Date,
            NgayKetThuc = cuTru.NgayKetThuc?.Date,
            cuTru.LaDaiDien
        }, transaction: tx);
    }

    public async Task<IEnumerable<HopDongKhachThue>> GetByKhachThueAsync(int khachThueId)
    {
        const string sql = """
            SELECT hdkt.*, hd.TrangThai AS TrangThaiHopDong, p.TenPhong, n.TenNha
            FROM HopDongKhachThue hdkt
            INNER JOIN HopDong hd ON hd.Id = hdkt.HopDongId
            INNER JOIN Phong p ON p.Id = hd.PhongId
            INNER JOIN Nha n ON n.Id = p.NhaId
            WHERE hdkt.KhachThueId = @KhachThueId
            ORDER BY hdkt.NgayBatDau DESC, hdkt.Id DESC
            """;
        return await _db.QueryAsync<HopDongKhachThue>(sql, new { KhachThueId = khachThueId });
    }

}
