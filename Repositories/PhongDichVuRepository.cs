using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class PhongDichVuRepository(IDbConnection db) : BaseRepository(db)
{
    /// <summary>Lấy toàn bộ dịch vụ của 1 phòng (kèm thông tin DichVu)</summary>
    public async Task<IEnumerable<PhongDichVu>> GetByPhongAsync(int phongId)
    {
        const string sql = """
            SELECT pdv.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.CachTinhCoDinh, dv.DonViTinh, dv.DonGiaMacDinh
            FROM PhongDichVu pdv
            INNER JOIN DichVu dv ON dv.Id = pdv.DichVuId
            WHERE pdv.PhongId = @PhongId AND pdv.DangApDung = 1
            ORDER BY dv.TenDichVu
            """;
        return await _db.QueryAsync<PhongDichVu, DichVu, PhongDichVu>(
            sql,
            (pdv, dv) => { pdv.DichVu = dv; return pdv; },
            new { PhongId = phongId },
            splitOn: "Id");
    }

    public async Task<int> InsertAsync(PhongDichVu pdv)
    {
        const string sql = """
            INSERT INTO PhongDichVu (PhongId, DichVuId, DonGia, DangApDung)
            VALUES (@PhongId, @DichVuId, @DonGia, @DangApDung);
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, pdv);
    }

    public async Task UpdateDonGiaAsync(int id, decimal donGiaMoi)
        => await _db.ExecuteAsync(
            "UPDATE PhongDichVu SET DonGia = @DonGia WHERE Id = @Id",
            new { Id = id, DonGia = donGiaMoi });

    public async Task SetApDungAsync(int id, bool apDung)
        => await _db.ExecuteAsync(
            "UPDATE PhongDichVu SET DangApDung = @DangApDung WHERE Id = @Id",
            new { Id = id, DangApDung = apDung });

    // ── Bổ sung Phase 3 ──────────────────────────────────────────────────────

    public async Task<PhongDichVu?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT pdv.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.CachTinhCoDinh, dv.DonViTinh, dv.DonGiaMacDinh
            FROM PhongDichVu pdv
            JOIN DichVu dv ON pdv.DichVuId = dv.Id
            WHERE pdv.Id = @Id
            """;
        var rows = await _db.QueryAsync<PhongDichVu, DichVu, PhongDichVu>(
            sql,
            (pdv, dv) => { pdv.DichVu = dv; return pdv; },
            new { Id = id }, splitOn: "Id");
        return rows.FirstOrDefault();
    }
}
