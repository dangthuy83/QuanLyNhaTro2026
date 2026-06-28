using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class ThanhToanRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<ThanhToan>> GetByHoaDonAsync(int hoaDonId)
        => await _db.QueryAsync<ThanhToan>(
            "SELECT * FROM ThanhToan WHERE HoaDonId = @HoaDonId ORDER BY NgayThu",
            new { HoaDonId = hoaDonId });

    /// <summary>
    /// INSERT ThanhToan — PHẢI truyền conn + tx từ ngoài vào
    /// vì phải chạy cùng transaction với UpdateSoTienDaThu trên HoaDon
    /// </summary>
    public async Task<int> InsertAsync(IDbConnection conn, IDbTransaction tx, ThanhToan tt)
    {
        const string sql = """
            INSERT INTO ThanhToan (HoaDonId, SoTien, NgayThu, HinhThuc, GhiChu, NgayTao)
            VALUES (@HoaDonId, @SoTien, @NgayThu, @HinhThuc, @GhiChu, NOW());
            SELECT LAST_INSERT_ID();
            """;
        return await conn.ExecuteScalarAsync<int>(sql, tt, transaction: tx);
    }

    public async Task DeleteAsync(int id)
        => await _db.ExecuteAsync("DELETE FROM ThanhToan WHERE Id = @Id", new { Id = id });

    public async Task<decimal> GetTongDaThuAsync(int hoaDonId)
        => await _db.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(SoTien), 0) FROM ThanhToan WHERE HoaDonId = @HoaDonId",
            new { HoaDonId = hoaDonId });
}
