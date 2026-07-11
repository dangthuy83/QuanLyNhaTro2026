using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class LichSuHinhThucDichVuRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<LichSuHinhThucDichVu>> GetByDichVuAsync(int dichVuId)
        => await _db.QueryAsync<LichSuHinhThucDichVu>(
            "SELECT * FROM LichSuHinhThucDichVu WHERE DichVuId=@DichVuId ORDER BY KyApDung DESC", new { DichVuId = dichVuId });

    public async Task<(string Loai, string Cach)> ResolveAsync(int dichVuId, int thang, int nam, DichVu fallback)
    {
        var ky = new DateTime(nam, thang, 1);
        var row = await _db.QueryFirstOrDefaultAsync<LichSuHinhThucDichVu>(
            "SELECT * FROM LichSuHinhThucDichVu WHERE DichVuId=@DichVuId AND KyApDung<=@Ky ORDER BY KyApDung DESC LIMIT 1",
            new { DichVuId = dichVuId, Ky = ky });
        if (row != null) return (row.LoaiTinhPhiMoi, row.CachTinhCoDinhMoi);
        var first = await _db.QueryFirstOrDefaultAsync<LichSuHinhThucDichVu>(
            "SELECT * FROM LichSuHinhThucDichVu WHERE DichVuId=@DichVuId ORDER BY KyApDung LIMIT 1", new { DichVuId = dichVuId });
        return first == null ? (fallback.LoaiTinhPhi, fallback.CachTinhCoDinh) : (first.LoaiTinhPhiCu, first.CachTinhCoDinhCu);
    }

    public async Task<decimal?> GetChiSoDauChuyenDoiAsync(int dichVuId, int phongId, int thang, int nam)
        => await _db.ExecuteScalarAsync<decimal?>("""
            SELECT cs.ChiSoDau FROM ChiSoDauChuyenDoiDichVu cs
            JOIN LichSuHinhThucDichVu ls ON ls.Id=cs.LichSuHinhThucDichVuId
            WHERE ls.DichVuId=@DichVuId AND cs.PhongId=@PhongId AND ls.KyApDung=@Ky
            """, new { DichVuId = dichVuId, PhongId = phongId, Ky = new DateTime(nam, thang, 1) });
}
