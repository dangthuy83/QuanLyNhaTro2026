using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class ChiSoNgoaiHopDongRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<ChiSoNgoaiHopDong>> GetAllAsync(int? phongId = null, int? dichVuId = null)
    {
        const string sql = """
            SELECT cs.*, p.Id AS PhongSplitId, p.Id, p.NhaId, p.TenPhong, p.TrangThai,
                   dv.Id AS DichVuSplitId, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.DonViTinh
            FROM ChiSoNgoaiHopDong cs
            INNER JOIN Phong p ON p.Id = cs.PhongId
            INNER JOIN DichVu dv ON dv.Id = cs.DichVuId
            WHERE (@PhongId IS NULL OR cs.PhongId = @PhongId)
              AND (@DichVuId IS NULL OR cs.DichVuId = @DichVuId)
            ORDER BY cs.NgayGhiNhan DESC, cs.Id DESC
            """;

        return await _db.QueryAsync<ChiSoNgoaiHopDong, Phong, DichVu, ChiSoNgoaiHopDong>(
            sql,
            (cs, phong, dichVu) =>
            {
                cs.Phong = phong;
                cs.DichVu = dichVu;
                return cs;
            },
            new { PhongId = phongId, DichVuId = dichVuId },
            splitOn: "PhongSplitId,DichVuSplitId");
    }

    public async Task<ChiSoNgoaiHopDong?> GetByIdAsync(int id)
        => await _db.QueryFirstOrDefaultAsync<ChiSoNgoaiHopDong>(
            "SELECT * FROM ChiSoNgoaiHopDong WHERE Id = @Id",
            new { Id = id });

    public async Task<ChiSoNgoaiHopDong?> GetLatestBeforePeriodAsync(
        int phongId,
        int dichVuId,
        int thang,
        int nam)
    {
        var cutoffExclusive = new DateTime(nam, thang, 1).AddMonths(1);
        const string sql = """
            SELECT *
            FROM ChiSoNgoaiHopDong
            WHERE PhongId = @PhongId
              AND DichVuId = @DichVuId
              AND NgayGhiNhan < @CutoffExclusive
            ORDER BY NgayGhiNhan DESC, Id DESC
            LIMIT 1
            """;

        return await _db.QueryFirstOrDefaultAsync<ChiSoNgoaiHopDong>(
            sql,
            new { PhongId = phongId, DichVuId = dichVuId, CutoffExclusive = cutoffExclusive });
    }

    public async Task<ChiSoNgoaiHopDong?> GetLatestBeforeOrOnDateAsync(
        int phongId,
        int dichVuId,
        DateTime cutoffDate)
    {
        const string sql = """
            SELECT *
            FROM ChiSoNgoaiHopDong
            WHERE PhongId = @PhongId
              AND DichVuId = @DichVuId
              AND NgayGhiNhan <= @CutoffDate
            ORDER BY NgayGhiNhan DESC, Id DESC
            LIMIT 1
            """;

        return await _db.QueryFirstOrDefaultAsync<ChiSoNgoaiHopDong>(
            sql,
            new { PhongId = phongId, DichVuId = dichVuId, CutoffDate = cutoffDate.Date });
    }

    public async Task<int> InsertAsync(ChiSoNgoaiHopDong item)
    {
        const string sql = """
            INSERT INTO ChiSoNgoaiHopDong
                (PhongId, DichVuId, TuChiSo, DenChiSo, NgayGhiNhan, LyDo, GhiChu, NgayTao)
            VALUES
                (@PhongId, @DichVuId, @TuChiSo, @DenChiSo, @NgayGhiNhan, @LyDo, @GhiChu, NOW());
            SELECT LAST_INSERT_ID();
            """;

        return await _db.ExecuteScalarAsync<int>(sql, item);
    }

    public async Task DeleteAsync(int id)
        => await _db.ExecuteAsync("DELETE FROM ChiSoNgoaiHopDong WHERE Id = @Id", new { Id = id });
}
