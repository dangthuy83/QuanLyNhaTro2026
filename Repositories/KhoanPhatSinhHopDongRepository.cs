using System.Data;
using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class KhoanPhatSinhHopDongRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<KhoanPhatSinhHopDong>> GetByHopDongAsync(int hopDongId)
        => await _db.QueryAsync<KhoanPhatSinhHopDong>(
            """
            SELECT *
            FROM KhoanPhatSinhHopDong
            WHERE HopDongId = @HopDongId
            ORDER BY NgayPhatSinh DESC, Id DESC
            """,
            new { HopDongId = hopDongId });

    public async Task<IEnumerable<KhoanPhatSinhHopDong>> GetByHoaDonAsync(int hoaDonId)
        => await _db.QueryAsync<KhoanPhatSinhHopDong>(
            """
            SELECT *
            FROM KhoanPhatSinhHopDong
            WHERE HoaDonId = @HoaDonId
            ORDER BY NgayPhatSinh, Id
            """,
            new { HoaDonId = hoaDonId });

    public async Task<List<KhoanPhatSinhHopDong>> GetChuaXuLyDenNgayAsync(
        int hopDongId,
        DateTime? denNgay)
    {
        var rows = await _db.QueryAsync<KhoanPhatSinhHopDong>(
            """
            SELECT *
            FROM KhoanPhatSinhHopDong
            WHERE HopDongId = @HopDongId
              AND TrangThai = 'ChuaXuLy'
              AND SoTien > SoTienDaXuLy
              AND (@DenNgay IS NULL OR NgayPhatSinh <= @DenNgay)
            ORDER BY NgayPhatSinh, Id
            """,
            new { HopDongId = hopDongId, DenNgay = denNgay?.Date });

        return rows.ToList();
    }

    public async Task<List<KhoanPhatSinhHopDong>> GetChuaXuLyDenNgayAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongId,
        DateTime? denNgay)
    {
        var rows = await conn.QueryAsync<KhoanPhatSinhHopDong>(
            """
            SELECT *
            FROM KhoanPhatSinhHopDong
            WHERE HopDongId = @HopDongId
              AND TrangThai = 'ChuaXuLy'
              AND SoTien > SoTienDaXuLy
              AND (@DenNgay IS NULL OR NgayPhatSinh <= @DenNgay)
            ORDER BY NgayPhatSinh, Id
            FOR UPDATE
            """,
            new { HopDongId = hopDongId, DenNgay = denNgay?.Date },
            tx);

        return rows.ToList();
    }

    public async Task<int> InsertAsync(KhoanPhatSinhHopDong khoan)
    {
        const string sql = """
            INSERT INTO KhoanPhatSinhHopDong
                (HopDongId, NgayPhatSinh, LoaiKhoan, MoTa, SoTien, SoTienDaXuLy, TrangThai, GhiChu)
            VALUES
                (@HopDongId, @NgayPhatSinh, @LoaiKhoan, @MoTa, @SoTien, @SoTienDaXuLy, @TrangThai, @GhiChu);
            SELECT LAST_INSERT_ID();
            """;

        return await _db.ExecuteScalarAsync<int>(sql, khoan);
    }

    public async Task HuyAsync(int id)
        => await _db.ExecuteAsync(
            """
            UPDATE KhoanPhatSinhHopDong
            SET TrangThai = 'DaHuy'
            WHERE Id = @Id AND TrangThai = 'ChuaXuLy'
            """,
            new { Id = id });

    public async Task GanVaoHoaDonAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        IEnumerable<int> ids,
        int hoaDonId)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return;

        await conn.ExecuteAsync(
            """
            UPDATE KhoanPhatSinhHopDong
            SET HoaDonId = @HoaDonId,
                TrangThai = 'DaDuaVaoHoaDon'
            WHERE Id IN @Ids
              AND TrangThai = 'ChuaXuLy'
            """,
            new { HoaDonId = hoaDonId, Ids = idList },
            tx);
    }

    public async Task TraVeChuaXuLyAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hoaDonId)
    {
        var trangThaiKhongTheHoanTac = (await conn.QueryAsync<int>(
            """
            SELECT Id
            FROM KhoanPhatSinhHopDong
            WHERE HoaDonId = @HoaDonId
              AND TrangThai <> 'DaDuaVaoHoaDon'
            FOR UPDATE
            """,
            new { HoaDonId = hoaDonId },
            tx)).Any();
        if (trangThaiKhongTheHoanTac)
            throw new InvalidOperationException("Hoa don co khoan phat sinh da duoc xu ly, khong the xoa.");

        await conn.ExecuteAsync(
            """
            UPDATE KhoanPhatSinhHopDong
            SET HoaDonId = NULL,
                TrangThai = 'ChuaXuLy'
            WHERE HoaDonId = @HoaDonId
              AND TrangThai = 'DaDuaVaoHoaDon'
            """,
            new { HoaDonId = hoaDonId },
            tx);
    }

    public async Task<decimal> ApDungTruCocAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongId,
        decimal soTien,
        DateTime denNgay)
    {
        if (soTien <= 0) return 0;

        var conLaiCanApDung = soTien;
        decimal daApDung = 0;
        var danhSach = await GetChuaXuLyDenNgayAsync(conn, tx, hopDongId, denNgay);

        foreach (var khoan in danhSach)
        {
            if (conLaiCanApDung <= 0) break;

            var soTienConLai = khoan.SoTienConLai;
            if (soTienConLai <= 0) continue;

            var apDung = Math.Min(soTienConLai, conLaiCanApDung);
            var soTienDaXuLyMoi = khoan.SoTienDaXuLy + apDung;
            var trangThaiMoi = soTienDaXuLyMoi >= khoan.SoTien
                ? KhoanPhatSinhHopDong.TrangThaiDaTruCoc
                : KhoanPhatSinhHopDong.TrangThaiChuaXuLy;

            await conn.ExecuteAsync(
                """
                UPDATE KhoanPhatSinhHopDong
                SET SoTienDaXuLy = @SoTienDaXuLy,
                    TrangThai = @TrangThai
                WHERE Id = @Id
                """,
                new { khoan.Id, SoTienDaXuLy = soTienDaXuLyMoi, TrangThai = trangThaiMoi },
                tx);

            daApDung += apDung;
            conLaiCanApDung -= apDung;
        }

        return daApDung;
    }
}
