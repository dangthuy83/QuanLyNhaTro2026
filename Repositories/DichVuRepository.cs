using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class DichVuRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<DichVu>> GetAllAsync(bool? hienThi = null)
        => await _db.QueryAsync<DichVu>("SELECT * FROM DichVu ORDER BY TenDichVu");

    public async Task<DichVu?> GetByIdAsync(int id)
        => await _db.QueryFirstOrDefaultAsync<DichVu>(
            "SELECT * FROM DichVu WHERE Id = @Id", new { Id = id });

    public async Task<int> InsertAsync(DichVu dichVu)
    {
        const string sql = """
            INSERT INTO DichVu (TenDichVu, LoaiTinhPhi, DonViTinh, DonGiaMacDinh)
            VALUES (@TenDichVu, @LoaiTinhPhi, @DonViTinh, @DonGiaMacDinh);
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, dichVu);
    }

    public async Task UpdateAsync(DichVu dichVu)
    {
        const string sql = """
            UPDATE DichVu SET TenDichVu = @TenDichVu, LoaiTinhPhi = @LoaiTinhPhi,
                DonViTinh = @DonViTinh,
                DonGiaMacDinh = @DonGiaMacDinh
            WHERE Id = @Id
            """;
        await _db.ExecuteAsync(sql, dichVu);
    }
}
