using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class KhachThueRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<KhachThue>> GetAllAsync()
        => await _db.QueryAsync<KhachThue>("SELECT * FROM KhachThue ORDER BY HoTen");

    public async Task<KhachThue?> GetByIdAsync(int id)
        => await _db.QueryFirstOrDefaultAsync<KhachThue>(
            "SELECT * FROM KhachThue WHERE Id = @Id", new { Id = id });

    public async Task<IEnumerable<KhachThue>> GetByHopDongAsync(int hopDongId)
    {
        const string sql = """
            SELECT kt.* FROM KhachThue kt
            INNER JOIN HopDongKhachThue hdkt ON hdkt.KhachThueId = kt.Id
            WHERE hdkt.HopDongId = @HopDongId
            ORDER BY hdkt.LaDaiDien DESC, kt.HoTen
            """;
        return await _db.QueryAsync<KhachThue>(sql, new { HopDongId = hopDongId });
    }

    public async Task<int> InsertAsync(KhachThue khach)
    {
        const string sql = """
            INSERT INTO KhachThue
                (HoTen, CCCD, SoDienThoai, NgaySinh, QueQuan, AnhCCCDMatTruoc, AnhCCCDMatSau, GhiChu, NgayTao)
            VALUES
                (@HoTen, @CCCD, @SoDienThoai, @NgaySinh, @QueQuan, @AnhCCCDMatTruoc, @AnhCCCDMatSau, @GhiChu, NOW());
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, khach);
    }

    public async Task UpdateAsync(KhachThue khach)
    {
        const string sql = """
            UPDATE KhachThue SET
                HoTen = @HoTen,
                CCCD = @CCCD,
                SoDienThoai = @SoDienThoai,
                NgaySinh = @NgaySinh,
                QueQuan = @QueQuan,
                AnhCCCDMatTruoc = @AnhCCCDMatTruoc,
                AnhCCCDMatSau = @AnhCCCDMatSau,
                GhiChu = @GhiChu
            WHERE Id = @Id
            """;
        await _db.ExecuteAsync(sql, khach);
    }

    public async Task DeleteAsync(int id)
        => await _db.ExecuteAsync("DELETE FROM KhachThue WHERE Id = @Id", new { Id = id });
}
