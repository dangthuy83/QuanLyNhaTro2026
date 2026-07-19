using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class HopDongRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<HopDong>> GetAllAsync()
    {
        const string sql = """
            SELECT hd.*,
                   p.Id AS PhongSplitId, p.Id, p.NhaId, p.TenPhong, p.DienTich,
                   p.GiaThueMacDinh, p.TrangThai, p.GhiChu, p.NgayTao,
                   n.Id AS NhaSplitId, n.Id, n.TenNha, n.DiaChi, n.GhiChu, n.NgayTao,
                   kt.Id AS KhachSplitId, kt.Id, kt.HoTen, kt.CCCD, kt.SoDienThoai,
                   kt.NgaySinh, kt.NgayCapCCCD, kt.NgheNghiep, kt.LoaiXe, kt.BienSoXe,
                   kt.QueQuan, kt.AnhCCCDMatTruoc, kt.AnhCCCDMatSau, kt.GhiChu, kt.NgayTao
            FROM HopDong hd
            INNER JOIN Phong p ON p.Id = hd.PhongId
            INNER JOIN Nha n ON n.Id = p.NhaId
            LEFT JOIN HopDongKhachThue hdkt ON hdkt.Id = (
                SELECT resident.Id
                FROM HopDongKhachThue resident
                WHERE resident.HopDongId = hd.Id AND resident.LaDaiDien = 1
                ORDER BY resident.NgayBatDau DESC, resident.Id DESC
                LIMIT 1)
            LEFT JOIN KhachThue kt ON kt.Id = hdkt.KhachThueId
            ORDER BY hd.NgayBatDau DESC
            """;
        return await _db.QueryAsync<HopDong, Phong, Nha, KhachThue, HopDong>(
            sql,
            (hd, p, n, khach) =>
            {
                p.Nha = n;
                hd.Phong = p;
                hd.KhachDaiDien = khach?.Id > 0 ? khach : null;
                return hd;
            },
            splitOn: "PhongSplitId,NhaSplitId,KhachSplitId");
    }

    public async Task<HopDong?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT hd.*,
                   p.Id AS PhongSplitId, p.Id, p.NhaId, p.TenPhong, p.DienTich,
                   p.GiaThueMacDinh, p.TrangThai, p.GhiChu, p.NgayTao,
                   n.Id AS NhaSplitId, n.Id, n.TenNha, n.DiaChi, n.GhiChu, n.NgayTao,
                   kt.Id AS KhachSplitId, kt.Id, kt.HoTen, kt.CCCD, kt.SoDienThoai,
                   kt.NgaySinh, kt.NgayCapCCCD, kt.NgheNghiep, kt.LoaiXe, kt.BienSoXe,
                   kt.QueQuan, kt.AnhCCCDMatTruoc, kt.AnhCCCDMatSau, kt.GhiChu, kt.NgayTao
            FROM HopDong hd
            INNER JOIN Phong p ON p.Id = hd.PhongId
            INNER JOIN Nha n ON n.Id = p.NhaId
            LEFT JOIN HopDongKhachThue hdkt ON hdkt.Id = (
                SELECT resident.Id
                FROM HopDongKhachThue resident
                WHERE resident.HopDongId = hd.Id AND resident.LaDaiDien = 1
                ORDER BY resident.NgayBatDau DESC, resident.Id DESC
                LIMIT 1)
            LEFT JOIN KhachThue kt ON kt.Id = hdkt.KhachThueId
            WHERE hd.Id = @Id
            """;
        var rows = await _db.QueryAsync<HopDong, Phong, Nha, KhachThue, HopDong>(
            sql,
            (hd, p, n, khach) =>
            {
                p.Nha = n;
                hd.Phong = p;
                hd.KhachDaiDien = khach?.Id > 0 ? khach : null;
                return hd;
            },
            new { Id = id },
            splitOn: "PhongSplitId,NhaSplitId,KhachSplitId");
        return rows.FirstOrDefault();
    }

    /// <summary>Hợp đồng đang hiệu lực của 1 phòng</summary>
    public async Task<HopDong?> GetDangHieuLucByPhongAsync(int phongId)
        => await _db.QueryFirstOrDefaultAsync<HopDong>(
            """
            SELECT * FROM HopDong
            WHERE PhongId = @PhongId
              AND TrangThai IN ('ChoHieuLuc', 'DangHieuLuc')
              AND NgayBatDau <= @Ngay
              AND (NgayKetThuc IS NULL OR NgayKetThuc >= @Ngay)
            ORDER BY NgayBatDau DESC, Id DESC
            LIMIT 1
            """,
            new { PhongId = phongId, Ngay = DateTime.Today });

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
                 NgayThanhToanHangThang, TrangThai, HopDongTruocId, DaXuLyChenhLechCoc, GhiChu, NgayTao)
            VALUES
                (@PhongId, @NgayBatDau, @NgayKetThuc, @TienThueThoaThuan, @TienCoc,
                 @NgayThanhToanHangThang, @TrangThai, @HopDongTruocId, @DaXuLyChenhLechCoc, @GhiChu, NOW());
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, hd);
    }

    public async Task<int> InsertAsync(IDbConnection conn, IDbTransaction tx, HopDong hd)
    {
        const string sql = """
            INSERT INTO HopDong
                (PhongId, NgayBatDau, NgayKetThuc, TienThueThoaThuan, TienCoc,
                 NgayThanhToanHangThang, TrangThai, HopDongTruocId, DaXuLyChenhLechCoc, GhiChu, NgayTao)
            VALUES
                (@PhongId, @NgayBatDau, @NgayKetThuc, @TienThueThoaThuan, @TienCoc,
                 @NgayThanhToanHangThang, @TrangThai, @HopDongTruocId, @DaXuLyChenhLechCoc, @GhiChu, NOW());
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
                NgayThanhToanHangThang = @NgayThanhToanHangThang,
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
                NgayThanhToanHangThang = @NgayThanhToanHangThang,
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
                NgayThanhToanHangThang = @NgayThanhToanHangThang,
                GhiChu = @GhiChu
            WHERE Id = @Id
            """,
            hd,
            transaction: tx);

    public async Task UpdateNgayThanhToanVaGhiChuAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int id,
        int ngayThanhToanHangThang,
        string? ghiChu)
        => await conn.ExecuteAsync(
            "UPDATE HopDong SET NgayThanhToanHangThang = @NgayThanhToanHangThang, GhiChu = @GhiChu WHERE Id = @Id",
            new { Id = id, NgayThanhToanHangThang = ngayThanhToanHangThang, GhiChu = ghiChu },
            transaction: tx);

    public async Task UpdateTrangThaiAsync(int id, string trangThai)
        => await _db.ExecuteAsync(
            "UPDATE HopDong SET TrangThai = @TrangThai WHERE Id = @Id",
            new { Id = id, TrangThai = trangThai });
}
