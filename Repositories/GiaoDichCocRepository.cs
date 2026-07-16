using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class GiaoDichCocRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<GiaoDichCoc>> GetByHopDongAsync(int hopDongId)
        => await _db.QueryAsync<GiaoDichCoc>(
            """
            SELECT *
            FROM GiaoDichCoc
            WHERE HopDongId = @HopDongId
            ORDER BY NgayGiaoDich, Id
            """,
            new { HopDongId = hopDongId });

    public async Task<decimal> GetSoDuAsync(int hopDongId)
        => await _db.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(SoTien), 0) FROM GiaoDichCoc WHERE HopDongId = @HopDongId",
            new { HopDongId = hopDongId });

    public async Task<bool> HasAnyAsync(IDbConnection conn, IDbTransaction tx, int hopDongId)
        => await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM GiaoDichCoc WHERE HopDongId = @HopDongId",
            new { HopDongId = hopDongId },
            transaction: tx) > 0;

    public async Task<decimal> GetSoDuAsync(IDbConnection conn, IDbTransaction tx, int hopDongId)
        => await conn.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(SoTien), 0) FROM GiaoDichCoc WHERE HopDongId = @HopDongId",
            new { HopDongId = hopDongId },
            transaction: tx);

    public async Task<int> InsertAsync(IDbConnection conn, IDbTransaction tx, GiaoDichCoc giaoDich)
    {
        const string sql = """
            INSERT INTO GiaoDichCoc
                (HopDongId, LoaiGiaoDich, SoTien, SoDuSauGiaoDich,
                 NgayGiaoDich, HoaDonId, PhuongThuc, DotMoSoId,
                 NguonThamChieu, GhiChu, NgayTao)
            VALUES
                (@HopDongId, @LoaiGiaoDich, @SoTien, @SoDuSauGiaoDich,
                 @NgayGiaoDich, @HoaDonId, @PhuongThuc, @DotMoSoId,
                 @NguonThamChieu, @GhiChu, NOW());
            SELECT LAST_INSERT_ID();
            """;

        return await conn.ExecuteScalarAsync<int>(sql, giaoDich, transaction: tx);
    }
}
