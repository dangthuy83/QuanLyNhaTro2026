using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class NhaRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<Nha>> GetAllAsync()
        => await _db.QueryAsync<Nha>("SELECT * FROM Nha ORDER BY TenNha");

    public async Task<Nha?> GetByIdAsync(int id)
        => await _db.QueryFirstOrDefaultAsync<Nha>(
            "SELECT * FROM Nha WHERE Id = @Id", new { Id = id });

    public async Task<int> InsertAsync(Nha nha)
    {
        const string sql = """
            INSERT INTO Nha (TenNha, DiaChi, GhiChu, NgayTao)
            VALUES (@TenNha, @DiaChi, @GhiChu, NOW());
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, nha);
    }

    public async Task UpdateAsync(Nha nha)
    {
        const string sql = """
            UPDATE Nha SET TenNha = @TenNha, DiaChi = @DiaChi, GhiChu = @GhiChu
            WHERE Id = @Id
            """;
        await _db.ExecuteAsync(sql, nha);
    }

    public async Task<int> CountPhongAsync(int id)
        => await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Phong WHERE NhaId = @Id",
            new { Id = id });

    public async Task DeleteAsync(int id)
        => await _db.ExecuteAsync("DELETE FROM Nha WHERE Id = @Id", new { Id = id });
}
