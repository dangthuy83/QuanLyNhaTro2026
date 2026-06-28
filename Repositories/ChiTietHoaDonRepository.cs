using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class ChiTietHoaDonRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<ChiTietHoaDon>> GetByHoaDonAsync(int hoaDonId)
    {
        const string sql = """
            SELECT ct.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.DonViTinh
            FROM ChiTietHoaDon ct
            INNER JOIN DichVu dv ON dv.Id = ct.DichVuId
            WHERE ct.HoaDonId = @HoaDonId
            """;
        return await _db.QueryAsync<ChiTietHoaDon, DichVu, ChiTietHoaDon>(
            sql,
            (ct, dv) => { ct.DichVu = dv; return ct; },
            new { HoaDonId = hoaDonId },
            splitOn: "Id");
    }

    public async Task InsertAsync(ChiTietHoaDon ct)
    {
        const string sql = """
            INSERT INTO ChiTietHoaDon
                (HoaDonId, DichVuId, SoLuong, DonGia, ThanhTien, ChiSoDienNuocId)
            VALUES
                (@HoaDonId, @DichVuId, @SoLuong, @DonGia, @ThanhTien, @ChiSoDienNuocId)
            """;
        await _db.ExecuteAsync(sql, ct);
    }

    public async Task InsertAsync(IDbConnection conn, IDbTransaction tx, ChiTietHoaDon ct)
    {
        const string sql = """
            INSERT INTO ChiTietHoaDon
                (HoaDonId, DichVuId, SoLuong, DonGia, ThanhTien, ChiSoDienNuocId)
            VALUES
                (@HoaDonId, @DichVuId, @SoLuong, @DonGia, @ThanhTien, @ChiSoDienNuocId)
            """;
        await conn.ExecuteAsync(sql, ct, transaction: tx);
    }

    public async Task DeleteByHoaDonAsync(int hoaDonId)
        => await _db.ExecuteAsync(
            "DELETE FROM ChiTietHoaDon WHERE HoaDonId = @HoaDonId",
            new { HoaDonId = hoaDonId });

    public async Task DeleteByHoaDonAsync(IDbConnection conn, IDbTransaction tx, int hoaDonId)
        => await conn.ExecuteAsync(
            "DELETE FROM ChiTietHoaDon WHERE HoaDonId = @HoaDonId",
            new { HoaDonId = hoaDonId },
            transaction: tx);
}
