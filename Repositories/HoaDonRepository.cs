using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class HoaDonRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<HoaDon?> GetByIdAsync(int id)
        => await _db.QueryFirstOrDefaultAsync<HoaDon>(
            "SELECT * FROM HoaDon WHERE Id = @Id", new { Id = id });

    public async Task<HoaDon?> GetByIdForUpdateAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int id)
        => await conn.QueryFirstOrDefaultAsync<HoaDon>(
            "SELECT * FROM HoaDon WHERE Id = @Id FOR UPDATE",
            new { Id = id },
            transaction: tx);

    public async Task<HoaDon?> GetByIdAsync(IDbConnection conn, IDbTransaction tx, int id)
        => await conn.QueryFirstOrDefaultAsync<HoaDon>(
            "SELECT * FROM HoaDon WHERE Id = @Id",
            new { Id = id },
            transaction: tx);

    /// <summary>Hóa đơn theo hợp đồng, sắp xếp mới nhất trước</summary>
    public async Task<IEnumerable<HoaDon>> GetByHopDongAsync(int hopDongId)
        => await _db.QueryAsync<HoaDon>(
            "SELECT * FROM HoaDon WHERE HopDongId = @HopDongId ORDER BY Nam DESC, Thang DESC",
            new { HopDongId = hopDongId });

    /// <summary>Hóa đơn của 1 kỳ cụ thể</summary>
    public async Task<HoaDon?> GetByHopDongKyAsync(int hopDongId, int thang, int nam)
        => await _db.QueryFirstOrDefaultAsync<HoaDon>(
            "SELECT * FROM HoaDon WHERE HopDongId = @HopDongId AND Thang = @Thang AND Nam = @Nam",
            new { HopDongId = hopDongId, Thang = thang, Nam = nam });

    public async Task<HoaDon?> GetByHopDongKyAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int hopDongId,
        int thang,
        int nam)
        => await conn.QueryFirstOrDefaultAsync<HoaDon>(
            "SELECT * FROM HoaDon WHERE HopDongId = @HopDongId AND Thang = @Thang AND Nam = @Nam",
            new { HopDongId = hopDongId, Thang = thang, Nam = nam },
            transaction: tx);

    public async Task<HoaDon?> GetByHopDongKyForUpdateAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int hopDongId,
        int thang,
        int nam)
        => await conn.QueryFirstOrDefaultAsync<HoaDon>(
            "SELECT * FROM HoaDon WHERE HopDongId = @HopDongId AND Thang = @Thang AND Nam = @Nam FOR UPDATE",
            new { HopDongId = hopDongId, Thang = thang, Nam = nam },
            transaction: tx);

    /// <summary>Hóa đơn kỳ trước (dùng lấy nợ tồn)</summary>
    public async Task<HoaDon?> GetKyTruocAsync(int hopDongId, int thang, int nam)
    {
        const string sql = """
            SELECT * FROM HoaDon
            WHERE HopDongId = @HopDongId
              AND (Nam < @Nam OR (Nam = @Nam AND Thang < @Thang))
            ORDER BY Nam DESC, Thang DESC
            LIMIT 1
            """;
        return await _db.QueryFirstOrDefaultAsync<HoaDon>(sql,
            new { HopDongId = hopDongId, Thang = thang, Nam = nam });
    }

    /// <summary>Danh sách hóa đơn chưa thu / thu một phần (trang thu tiền)</summary>
    public async Task<IEnumerable<HoaDon>> GetChuaThuAsync()
    {
        const string sql = """
            SELECT hd.*
            FROM HoaDon hd
            WHERE hd.TrangThaiThanhToan IN ('ChuaThu', 'ThuMotPhan')
            ORDER BY hd.Nam, hd.Thang, hd.TenPhongSnapshot
            """;
        return await _db.QueryAsync<HoaDon>(sql);
    }

    public async Task<int> InsertAsync(HoaDon hd)
    {
        const string sql = """
            INSERT INTO HoaDon
                (HopDongId, Thang, Nam, NgayLap, NgayDenHan, TienPhong, TongTienDichVu,
                 TongTienPhatSinh, TienNoKyTruoc, TongCong, SoTienDaThu, TrangThaiThanhToan,
                 SoNgayO, SoNgayTrongThang, HoaDonGhepId, GhiChu,
                 NhaIdSnapshot, TenNhaSnapshot, PhongIdSnapshot, TenPhongSnapshot,
                 KhachDaiDienIdSnapshot, TenKhachDaiDienSnapshot, CccdKhachDaiDienSnapshot)
            VALUES
                (@HopDongId, @Thang, @Nam, @NgayLap, @NgayDenHan, @TienPhong, @TongTienDichVu,
                 @TongTienPhatSinh, @TienNoKyTruoc, @TongCong, @SoTienDaThu, @TrangThaiThanhToan,
                 @SoNgayO, @SoNgayTrongThang, @HoaDonGhepId, @GhiChu,
                 @NhaIdSnapshot, @TenNhaSnapshot, @PhongIdSnapshot, @TenPhongSnapshot,
                 @KhachDaiDienIdSnapshot, @TenKhachDaiDienSnapshot, @CccdKhachDaiDienSnapshot);
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, hd);
    }

    public async Task<int> InsertAsync(IDbConnection conn, IDbTransaction tx, HoaDon hd)
    {
        const string sql = """
            INSERT INTO HoaDon
                (HopDongId, Thang, Nam, NgayLap, NgayDenHan, TienPhong, TongTienDichVu,
                 TongTienPhatSinh, TienNoKyTruoc, TongCong, SoTienDaThu, TrangThaiThanhToan,
                 SoNgayO, SoNgayTrongThang, HoaDonGhepId, GhiChu,
                 NhaIdSnapshot, TenNhaSnapshot, PhongIdSnapshot, TenPhongSnapshot,
                 KhachDaiDienIdSnapshot, TenKhachDaiDienSnapshot, CccdKhachDaiDienSnapshot)
            VALUES
                (@HopDongId, @Thang, @Nam, @NgayLap, @NgayDenHan, @TienPhong, @TongTienDichVu,
                 @TongTienPhatSinh, @TienNoKyTruoc, @TongCong, @SoTienDaThu, @TrangThaiThanhToan,
                 @SoNgayO, @SoNgayTrongThang, @HoaDonGhepId, @GhiChu,
                 @NhaIdSnapshot, @TenNhaSnapshot, @PhongIdSnapshot, @TenPhongSnapshot,
                 @KhachDaiDienIdSnapshot, @TenKhachDaiDienSnapshot, @CccdKhachDaiDienSnapshot);
            SELECT LAST_INSERT_ID();
            """;
        return await conn.ExecuteScalarAsync<int>(sql, hd, transaction: tx);
    }

    /// <summary>
    /// Cập nhật SoTienDaThu + TrangThaiThanhToan — luôn gọi trong cùng transaction với ThanhToan
    /// </summary>
    public async Task UpdateSoTienDaThuAsync(IDbConnection conn, IDbTransaction tx, int hoaDonId, decimal soTienDaThu, string trangThai)
        => await conn.ExecuteAsync(
            "UPDATE HoaDon SET SoTienDaThu = @SoTienDaThu, TrangThaiThanhToan = @TrangThai WHERE Id = @Id",
            new { Id = hoaDonId, SoTienDaThu = soTienDaThu, TrangThai = trangThai },
            transaction: tx);

    public async Task DeleteAsync(int id)
        => await _db.ExecuteAsync("DELETE FROM HoaDon WHERE Id = @Id", new { Id = id });

    public async Task DeleteAsync(IDbConnection conn, IDbTransaction tx, int id)
        => await conn.ExecuteAsync(
            "DELETE FROM HoaDon WHERE Id = @Id",
            new { Id = id },
            transaction: tx);

    public async Task UnlinkHoaDonGhepAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int id,
        int? hoaDonGhepId)
    {
        await conn.QueryAsync<int>(
            """
            SELECT Id
            FROM HoaDon
            WHERE HoaDonGhepId = @Id OR (@HoaDonGhepId IS NOT NULL AND Id = @HoaDonGhepId)
            ORDER BY Id
            FOR UPDATE
            """,
            new { Id = id, HoaDonGhepId = hoaDonGhepId },
            transaction: tx);

        await conn.ExecuteAsync(
            "UPDATE HoaDon SET HoaDonGhepId = NULL WHERE HoaDonGhepId = @Id OR Id = @Id",
            new { Id = id },
            transaction: tx);
    }


// ── Bổ sung Phase 2–3 ────────────────────────────────────────────────────────

    // ── Bổ sung Phase 2–3 ────────────────────────────────────────────────────

    /// <summary>Hóa đơn theo hợp đồng + tháng/năm cụ thể.</summary>
    public async Task<HoaDon?> GetByHopDongThangNamAsync(int hopDongId, int thang, int nam)
        => await _db.QueryFirstOrDefaultAsync<HoaDon>(
            "SELECT * FROM HoaDon WHERE HopDongId=@HopDongId AND Thang=@Thang AND Nam=@Nam LIMIT 1",
            new { HopDongId = hopDongId, Thang = thang, Nam = nam });

    /// <summary>Hóa đơn cuối cùng của hợp đồng.</summary>
    public async Task<HoaDon?> GetHoaDonCuoiCungAsync(int hopDongId)
        => await _db.QueryFirstOrDefaultAsync<HoaDon>(
            "SELECT * FROM HoaDon WHERE HopDongId=@HopDongId ORDER BY Nam DESC, Thang DESC LIMIT 1",
            new { HopDongId = hopDongId });

    /// <summary>Tổng nợ còn lại của 1 hợp đồng (tất cả kỳ chưa thanh toán đủ).</summary>
    public async Task<decimal> GetTongNoConLaiAsync(int hopDongId)
        => await _db.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(TongCong - SoTienDaThu),0) FROM HoaDon WHERE HopDongId=@HopDongId AND TongCong > SoTienDaThu",
            new { HopDongId = hopDongId });

    /// <summary>Query công nợ tồn đọng — dùng cho BaoCaoController.</summary>
    public async Task<IEnumerable<BaoCaoCongNoViewModel>> GetCongNoAsync()
    {
        const string sql = """
            SELECT
                hd.NhaIdSnapshot AS NhaId,
                hd.TenNhaSnapshot AS TenNha,
                hd.TenPhongSnapshot AS TenPhong,
                hd.TenKhachDaiDienSnapshot AS TenKhachChinh,
                k.SoDienThoai,
                hd.Id          AS HoaDonId,
                hd.Thang,
                hd.Nam,
                hd.TongCong,
                hd.SoTienDaThu,
                hd.NgayDenHan,
                hop.TrangThai  AS TrangThaiHopDong,
                GREATEST(0, DATEDIFF(CURDATE(), hd.NgayDenHan)) AS SoNgayQuaHan
            FROM HoaDon hd
            JOIN HopDong hop ON hd.HopDongId = hop.Id
            LEFT JOIN KhachThue k ON k.Id = hd.KhachDaiDienIdSnapshot
            WHERE hd.SoTienDaThu < hd.TongCong AND hd.TongCong > 0
            ORDER BY hop.TrangThai DESC, hd.Nam, hd.Thang, hd.TenPhongSnapshot
            """;
        return await _db.QueryAsync<BaoCaoCongNoViewModel>(sql);
    }
}
