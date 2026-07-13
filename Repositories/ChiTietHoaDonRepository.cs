using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class ChiTietHoaDonRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<ChiTietHoaDon>> GetByHoaDonAsync(int hoaDonId)
    {
        return await _db.QueryAsync<ChiTietHoaDon>(
            "SELECT * FROM ChiTietHoaDon WHERE HoaDonId = @HoaDonId ORDER BY Id",
            new { HoaDonId = hoaDonId });
    }

    public async Task InsertAsync(ChiTietHoaDon ct)
    {
        const string sql = """
            INSERT INTO ChiTietHoaDon
                (HoaDonId, DichVuId, SoLuong, DonGia, ThanhTien, ChiSoDienNuocId,
                 TenDichVuSnapshot, DonViTinhSnapshot)
            VALUES
                (@HoaDonId, @DichVuId, @SoLuong, @DonGia, @ThanhTien, @ChiSoDienNuocId,
                 @TenDichVuSnapshot, @DonViTinhSnapshot)
            """;
        await _db.ExecuteAsync(sql, ct);
    }

    public async Task InsertAsync(IDbConnection conn, IDbTransaction tx, ChiTietHoaDon ct)
    {
        const string sql = """
            INSERT INTO ChiTietHoaDon
                (HoaDonId, DichVuId, SoLuong, DonGia, ThanhTien, ChiSoDienNuocId,
                 TenDichVuSnapshot, DonViTinhSnapshot)
            VALUES
                (@HoaDonId, @DichVuId, @SoLuong, @DonGia, @ThanhTien, @ChiSoDienNuocId,
                 @TenDichVuSnapshot, @DonViTinhSnapshot)
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
