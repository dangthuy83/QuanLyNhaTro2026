using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class ChiSoDienNuocRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<ChiSoDienNuoc>> GetByHopDongKyAsync(int hopDongId, int thang, int nam)
    {
        const string sql = """
            SELECT cs.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.DonViTinh
            FROM ChiSoDienNuoc cs
            INNER JOIN HopDong hd ON hd.PhongId = cs.PhongId
            INNER JOIN DichVu dv ON dv.Id = cs.DichVuId
            WHERE hd.Id = @HopDongId AND cs.Thang = @Thang AND cs.Nam = @Nam
            """;
        return await _db.QueryAsync<ChiSoDienNuoc, DichVu, ChiSoDienNuoc>(
            sql,
            (cs, dv) => { cs.DichVu = dv; return cs; },
            new { HopDongId = hopDongId, Thang = thang, Nam = nam },
            splitOn: "Id");
    }

    public async Task<IEnumerable<ChiSoDienNuoc>> GetByPhongKyAsync(int phongId, int thang, int nam)
    {
        const string sql = """
            SELECT cs.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.DonViTinh
            FROM ChiSoDienNuoc cs
            INNER JOIN DichVu dv ON dv.Id = cs.DichVuId
            WHERE cs.PhongId = @PhongId AND cs.Thang = @Thang AND cs.Nam = @Nam
            """;
        return await _db.QueryAsync<ChiSoDienNuoc, DichVu, ChiSoDienNuoc>(
            sql,
            (cs, dv) => { cs.DichVu = dv; return cs; },
            new { PhongId = phongId, Thang = thang, Nam = nam },
            splitOn: "Id");
    }

    public async Task<ChiSoDienNuoc?> GetChiSoCuoiKyTruocAsync(int phongId, int dichVuId, int thang, int nam)
    {
        const string sql = """
            SELECT * FROM ChiSoDienNuoc
            WHERE PhongId = @PhongId AND DichVuId = @DichVuId
              AND (Nam < @Nam OR (Nam = @Nam AND Thang < @Thang))
            ORDER BY Nam DESC, Thang DESC
            LIMIT 1
            """;
        return await _db.QueryFirstOrDefaultAsync<ChiSoDienNuoc>(sql,
            new { PhongId = phongId, DichVuId = dichVuId, Thang = thang, Nam = nam });
    }

    public async Task<int> InsertAsync(ChiSoDienNuoc cs)
    {
        const string sql = """
            INSERT INTO ChiSoDienNuoc
                (PhongId, DichVuId, Thang, Nam, ChiSoDau, ChiSoCuoi,
                 LoaiGhiNhan, ChiSoTruocReset, ChiSoSauReset, LyDoDieuChinh,
                 NgayDoc, GhiChu)
            VALUES
                (@PhongId, @DichVuId, @Thang, @Nam, @ChiSoDau, @ChiSoCuoi,
                 @LoaiGhiNhan, @ChiSoTruocReset, @ChiSoSauReset, @LyDoDieuChinh,
                 @NgayDoc, @GhiChu);
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, cs);
    }

    public async Task UpdateAsync(ChiSoDienNuoc cs)
    {
        const string sql = """
            UPDATE ChiSoDienNuoc SET
                ChiSoDau = @ChiSoDau,
                ChiSoCuoi = @ChiSoCuoi,
                LoaiGhiNhan = @LoaiGhiNhan,
                ChiSoTruocReset = @ChiSoTruocReset,
                ChiSoSauReset = @ChiSoSauReset,
                LyDoDieuChinh = @LyDoDieuChinh,
                NgayDoc = @NgayDoc,
                GhiChu = @GhiChu
            WHERE Id = @Id
            """;
        await _db.ExecuteAsync(sql, cs);
    }

    public async Task DeleteAsync(int id)
        => await _db.ExecuteAsync("DELETE FROM ChiSoDienNuoc WHERE Id = @Id", new { Id = id });
}
