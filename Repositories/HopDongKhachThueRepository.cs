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
            ORDER BY hdkt.LaDaiDien DESC, kt.HoTen
            """;
        return await _db.QueryAsync<HopDongKhachThue, KhachThue, HopDongKhachThue>(
            sql,
            (hdkt, kt) => { hdkt.KhachThue = kt; return hdkt; },
            new { HopDongId = hopDongId },
            splitOn: "Id");
    }

    public async Task InsertAsync(int hopDongId, int khachThueId, bool laKhachChinh = false)
    {
        const string sql = """
            INSERT INTO HopDongKhachThue (HopDongId, KhachThueId, LaDaiDien)
            VALUES (@HopDongId, @KhachThueId, @LaDaiDien)
            """;
        await _db.ExecuteAsync(sql, new { HopDongId = hopDongId, KhachThueId = khachThueId, LaDaiDien = laKhachChinh });
    }

    public async Task InsertAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int hopDongId,
        int khachThueId,
        bool laKhachChinh = false)
    {
        const string sql = """
            INSERT INTO HopDongKhachThue (HopDongId, KhachThueId, LaDaiDien)
            VALUES (@HopDongId, @KhachThueId, @LaDaiDien)
            """;
        await conn.ExecuteAsync(
            sql,
            new { HopDongId = hopDongId, KhachThueId = khachThueId, LaDaiDien = laKhachChinh },
            transaction: tx);
    }

    public async Task DeleteByHopDongAsync(int hopDongId)
        => await _db.ExecuteAsync(
            "DELETE FROM HopDongKhachThue WHERE HopDongId = @HopDongId",
            new { HopDongId = hopDongId });

    public async Task DeleteByHopDongAsync(IDbConnection conn, IDbTransaction tx, int hopDongId)
        => await conn.ExecuteAsync(
            "DELETE FROM HopDongKhachThue WHERE HopDongId = @HopDongId",
            new { HopDongId = hopDongId },
            transaction: tx);

    public async Task DeleteAsync(int id)
        => await _db.ExecuteAsync(
            "DELETE FROM HopDongKhachThue WHERE Id = @Id", new { Id = id });
}
