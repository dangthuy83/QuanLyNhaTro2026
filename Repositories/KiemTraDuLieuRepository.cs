using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class KiemTraDuLieuRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<KiemTraDuLieuRow>> GetRowsAsync(int thang, int nam, int? nhaId)
    {
        const string sql = """
            SELECT
                hd.Id AS HopDongId,
                p.Id AS PhongId,
                n.Id AS NhaId,
                n.TenNha,
                p.TenPhong,
                GROUP_CONCAT(DISTINCT kt.HoTen ORDER BY kt.HoTen SEPARATOR ', ') AS TenKhach,
                hd.NgayBatDau,
                hd.NgayKetThuc,
                COUNT(DISTINCT hdkt.KhachThueId) AS SoKhach,
                COUNT(DISTINCT pdv.Id) AS SoDichVuDangApDung,
                COUNT(DISTINCT CASE WHEN dv.LoaiTinhPhi = 'TheoChiSo' THEN pdv.Id END) AS SoDichVuTheoChiSo,
                COUNT(DISTINCT CASE WHEN dv.LoaiTinhPhi = 'TheoChiSo' AND cs.Id IS NOT NULL THEN pdv.Id END) AS SoChiSoTheoChiSo,
                COUNT(DISTINCT CASE WHEN dv.LoaiTinhPhi = 'CoDinh' AND dv.CachTinhCoDinh = 'TheoNguoi' THEN pdv.Id END) AS SoDichVuTheoNguoi,
                COUNT(DISTINCT CASE WHEN pdv.Id IS NOT NULL AND pdv.DonGia <= 0 THEN pdv.Id END) AS SoDichVuDonGiaKhongHopLe,
                GROUP_CONCAT(DISTINCT CASE WHEN dv.LoaiTinhPhi = 'TheoChiSo' THEN dv.TenDichVu END ORDER BY dv.TenDichVu SEPARATOR ', ') AS DichVuTheoChiSo,
                GROUP_CONCAT(DISTINCT CASE WHEN pdv.Id IS NOT NULL AND pdv.DonGia <= 0 THEN dv.TenDichVu END ORDER BY dv.TenDichVu SEPARATOR ', ') AS DichVuDonGiaKhongHopLe,
                GROUP_CONCAT(DISTINCT CASE WHEN dv.LoaiTinhPhi = 'CoDinh' AND dv.CachTinhCoDinh = 'TheoNguoi' THEN dv.TenDichVu END ORDER BY dv.TenDichVu SEPARATOR ', ') AS DichVuTheoNguoi
            FROM HopDong hd
            INNER JOIN Phong p ON p.Id = hd.PhongId
            INNER JOIN Nha n ON n.Id = p.NhaId
            LEFT JOIN HopDongKhachThue hdkt ON hdkt.HopDongId = hd.Id
            LEFT JOIN KhachThue kt ON kt.Id = hdkt.KhachThueId
            LEFT JOIN PhongDichVu pdv
                ON pdv.PhongId = p.Id
               AND pdv.DangApDung = 1
            LEFT JOIN DichVu dv ON dv.Id = pdv.DichVuId
            LEFT JOIN ChiSoDienNuoc cs
                ON cs.PhongId = p.Id
               AND cs.DichVuId = pdv.DichVuId
               AND cs.Thang = @Thang
               AND cs.Nam = @Nam
               AND (cs.HopDongId = hd.Id OR cs.HopDongId IS NULL)
            WHERE hd.TrangThai = 'DangHieuLuc'
              AND (@NhaId IS NULL OR p.NhaId = @NhaId)
            GROUP BY
                hd.Id, p.Id, n.Id, n.TenNha, p.TenPhong,
                hd.NgayBatDau, hd.NgayKetThuc
            ORDER BY n.TenNha, p.TenPhong, hd.Id
            """;

        return await _db.QueryAsync<KiemTraDuLieuRow>(sql, new { Thang = thang, Nam = nam, NhaId = nhaId });
    }
}
