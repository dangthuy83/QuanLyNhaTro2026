using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class ChiSoDienNuocRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<ChiSoDienNuoc>> GetByHopDongKyAsync(int hopDongId, int thang, int nam)
    {
        const string sql = """
            SELECT cs.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.CachTinhCoDinh, dv.DonViTinh
            FROM ChiSoDienNuoc cs
            INNER JOIN HopDong hd ON hd.PhongId = cs.PhongId
            INNER JOIN DichVu dv ON dv.Id = cs.DichVuId
            WHERE hd.Id = @HopDongId
              AND cs.Thang = @Thang
              AND cs.Nam = @Nam
              AND (cs.HopDongId = hd.Id OR cs.HopDongId IS NULL)
            ORDER BY CASE WHEN cs.HopDongId = hd.Id THEN 0 ELSE 1 END, cs.Id DESC
            """;
        return await _db.QueryAsync<ChiSoDienNuoc, DichVu, ChiSoDienNuoc>(
            sql,
            (cs, dv) => { cs.DichVu = dv; return cs; },
            new { HopDongId = hopDongId, Thang = thang, Nam = nam },
            splitOn: "Id");
    }

    public async Task<IEnumerable<ChiSoDienNuoc>> GetByPhongKyAsync(
        int phongId,
        int thang,
        int nam,
        int? hopDongId = null)
    {
        var sql = hopDongId.HasValue
            ? """
              SELECT cs.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.CachTinhCoDinh, dv.DonViTinh
              FROM ChiSoDienNuoc cs
              INNER JOIN DichVu dv ON dv.Id = cs.DichVuId
              WHERE cs.PhongId = @PhongId
                AND cs.Thang = @Thang
                AND cs.Nam = @Nam
                AND (cs.HopDongId = @HopDongId OR cs.HopDongId IS NULL)
              ORDER BY CASE WHEN cs.HopDongId = @HopDongId THEN 0 ELSE 1 END, cs.Id DESC
              """
            : """
              SELECT cs.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.CachTinhCoDinh, dv.DonViTinh
              FROM ChiSoDienNuoc cs
              INNER JOIN DichVu dv ON dv.Id = cs.DichVuId
              WHERE cs.PhongId = @PhongId
                AND cs.Thang = @Thang
                AND cs.Nam = @Nam
                AND cs.HopDongId IS NULL
              ORDER BY cs.Id DESC
              """;

        return await _db.QueryAsync<ChiSoDienNuoc, DichVu, ChiSoDienNuoc>(
            sql,
            (cs, dv) => { cs.DichVu = dv; return cs; },
            new { PhongId = phongId, Thang = thang, Nam = nam, HopDongId = hopDongId },
            splitOn: "Id");
    }

    public async Task<ChiSoDienNuoc?> GetChiSoCuoiKyTruocAsync(
        int phongId,
        int dichVuId,
        int thang,
        int nam,
        int? hopDongId = null)
    {
        var sql = hopDongId.HasValue
            ? """
              SELECT * FROM ChiSoDienNuoc
              WHERE PhongId = @PhongId
                AND DichVuId = @DichVuId
                AND HopDongId = @HopDongId
                AND (Nam < @Nam OR (Nam = @Nam AND Thang < @Thang))
              ORDER BY Nam DESC, Thang DESC, Id DESC
              LIMIT 1
              """
            : """
              SELECT * FROM ChiSoDienNuoc
              WHERE PhongId = @PhongId
                AND DichVuId = @DichVuId
                AND (Nam < @Nam OR (Nam = @Nam AND Thang < @Thang))
              ORDER BY Nam DESC, Thang DESC, Id DESC
              LIMIT 1
              """;

        return await _db.QueryFirstOrDefaultAsync<ChiSoDienNuoc>(sql,
            new { PhongId = phongId, DichVuId = dichVuId, Thang = thang, Nam = nam, HopDongId = hopDongId });
    }

    public async Task<ChiSoDienNuoc?> GetLatestBeforeOrOnDateAsync(
        int phongId,
        int dichVuId,
        DateTime cutoffDate,
        int? excludeHopDongId = null)
    {
        const string sql = """
            SELECT *
            FROM ChiSoDienNuoc
            WHERE PhongId = @PhongId
              AND DichVuId = @DichVuId
              AND (@ExcludeHopDongId IS NULL OR HopDongId IS NULL OR HopDongId <> @ExcludeHopDongId)
              AND COALESCE(NgayDoc, LAST_DAY(STR_TO_DATE(CONCAT(Nam, '-', LPAD(Thang, 2, '0'), '-01'), '%Y-%m-%d'))) <= @CutoffDate
            ORDER BY COALESCE(NgayDoc, LAST_DAY(STR_TO_DATE(CONCAT(Nam, '-', LPAD(Thang, 2, '0'), '-01'), '%Y-%m-%d'))) DESC,
                     Nam DESC, Thang DESC, Id DESC
            LIMIT 1
            """;

        return await _db.QueryFirstOrDefaultAsync<ChiSoDienNuoc>(
            sql,
            new
            {
                PhongId = phongId,
                DichVuId = dichVuId,
                CutoffDate = cutoffDate.Date,
                ExcludeHopDongId = excludeHopDongId
            });
    }

    public async Task<ChiSoDienNuoc?> GetLatestAsync(int phongId, int dichVuId)
    {
        const string sql = """
            SELECT *
            FROM ChiSoDienNuoc
            WHERE PhongId = @PhongId
              AND DichVuId = @DichVuId
            ORDER BY COALESCE(NgayDoc, LAST_DAY(STR_TO_DATE(CONCAT(Nam, '-', LPAD(Thang, 2, '0'), '-01'), '%Y-%m-%d'))) DESC,
                     Nam DESC, Thang DESC, Id DESC
            LIMIT 1
            """;

        return await _db.QueryFirstOrDefaultAsync<ChiSoDienNuoc>(
            sql,
            new { PhongId = phongId, DichVuId = dichVuId });
    }

    public async Task<int> InsertAsync(ChiSoDienNuoc cs)
    {
        const string sql = """
            INSERT INTO ChiSoDienNuoc
                (HopDongId, PhongId, DichVuId, Thang, Nam, ChiSoDau, ChiSoCuoi,
                 LoaiGhiNhan, ChiSoTruocReset, ChiSoSauReset, LyDoDieuChinh,
                 NgayDoc, GhiChu)
            VALUES
                (@HopDongId, @PhongId, @DichVuId, @Thang, @Nam, @ChiSoDau, @ChiSoCuoi,
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
                HopDongId = @HopDongId,
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
