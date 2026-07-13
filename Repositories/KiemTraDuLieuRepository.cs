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
                COUNT(DISTINCT CASE WHEN COALESCE(ls.LoaiTinhPhiMoi,firstls.LoaiTinhPhiCu,dv.LoaiTinhPhi) = 'TheoChiSo' THEN pdv.Id END) AS SoDichVuTheoChiSo,
                COUNT(DISTINCT CASE WHEN COALESCE(ls.LoaiTinhPhiMoi,firstls.LoaiTinhPhiCu,dv.LoaiTinhPhi) = 'TheoChiSo' AND cs.Id IS NOT NULL THEN pdv.Id END) AS SoChiSoTheoChiSo,
                COUNT(DISTINCT CASE WHEN COALESCE(ls.LoaiTinhPhiMoi,firstls.LoaiTinhPhiCu,dv.LoaiTinhPhi) = 'CoDinh' AND COALESCE(ls.CachTinhCoDinhMoi,firstls.CachTinhCoDinhCu,dv.CachTinhCoDinh) = 'TheoNguoi' THEN pdv.Id END) AS SoDichVuTheoNguoi,
                COUNT(DISTINCT CASE WHEN pdv.Id IS NOT NULL AND pdv.DonGia <= 0 THEN pdv.Id END) AS SoDichVuDonGiaKhongHopLe,
                GROUP_CONCAT(DISTINCT CASE WHEN COALESCE(ls.LoaiTinhPhiMoi,firstls.LoaiTinhPhiCu,dv.LoaiTinhPhi) = 'TheoChiSo' THEN dv.TenDichVu END ORDER BY dv.TenDichVu SEPARATOR ', ') AS DichVuTheoChiSo,
                GROUP_CONCAT(DISTINCT CASE WHEN pdv.Id IS NOT NULL AND pdv.DonGia <= 0 THEN dv.TenDichVu END ORDER BY dv.TenDichVu SEPARATOR ', ') AS DichVuDonGiaKhongHopLe,
                GROUP_CONCAT(DISTINCT CASE WHEN COALESCE(ls.LoaiTinhPhiMoi,firstls.LoaiTinhPhiCu,dv.LoaiTinhPhi) = 'CoDinh' AND COALESCE(ls.CachTinhCoDinhMoi,firstls.CachTinhCoDinhCu,dv.CachTinhCoDinh) = 'TheoNguoi' THEN dv.TenDichVu END ORDER BY dv.TenDichVu SEPARATOR ', ') AS DichVuTheoNguoi
            FROM HopDong hd
            INNER JOIN Phong p ON p.Id = hd.PhongId
            INNER JOIN Nha n ON n.Id = p.NhaId
            LEFT JOIN HopDongKhachThue hdkt
                ON hdkt.HopDongId = hd.Id
               AND hdkt.NgayBatDau <= @KyKetThuc
               AND (hdkt.NgayKetThuc IS NULL OR hdkt.NgayKetThuc >= @Ky)
            LEFT JOIN KhachThue kt ON kt.Id = hdkt.KhachThueId
            LEFT JOIN HopDongDichVu hdv
                ON hdv.HopDongId = hd.Id
               AND hdv.KyBatDau <= @Ky
               AND (hdv.KyKetThuc IS NULL OR @Ky < hdv.KyKetThuc)
            LEFT JOIN PhongDichVu pdv
                ON pdv.Id = hdv.PhongDichVuId
            LEFT JOIN DichVu dv ON dv.Id = pdv.DichVuId
            LEFT JOIN LichSuHinhThucDichVu ls ON ls.Id=(SELECT x.Id FROM LichSuHinhThucDichVu x WHERE x.DichVuId=dv.Id AND x.KyApDung<=@Ky ORDER BY x.KyApDung DESC LIMIT 1)
            LEFT JOIN LichSuHinhThucDichVu firstls ON firstls.Id=(SELECT x.Id FROM LichSuHinhThucDichVu x WHERE x.DichVuId=dv.Id ORDER BY x.KyApDung LIMIT 1)
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

        return await _db.QueryAsync<KiemTraDuLieuRow>(sql, new
        {
            Thang = thang,
            Nam = nam,
            NhaId = nhaId,
            Ky = new DateTime(nam, thang, 1),
            KyKetThuc = new DateTime(nam, thang, 1).AddMonths(1).AddDays(-1)
        });
    }
}
