using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class KiemTraDuLieuRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<ReconcileIssue>> GetReconcileIssuesAsync()
    {
        const string sql = """
            SELECT 'HopDongThieuQuyetToan' Loai,
                   CONCAT('Hợp đồng đã kết thúc nhưng thiếu hóa đơn kỳ trả ', MONTH(hd.NgayTraPhongThucTe), '/', YEAR(hd.NgayTraPhongThucTe), '.') LyDo,
                   'HopDong' DoiTuong, hd.Id DoiTuongId, 'HopDong' LinkController, 'Details' LinkAction
            FROM HopDong hd
            LEFT JOIN HoaDon finalHd ON finalHd.HopDongId=hd.Id
              AND finalHd.Thang=MONTH(hd.NgayTraPhongThucTe) AND finalHd.Nam=YEAR(hd.NgayTraPhongThucTe)
            WHERE hd.TrangThai='DaKetThuc' AND hd.NgayTraPhongThucTe IS NOT NULL AND finalHd.Id IS NULL

            UNION ALL
            SELECT 'HoaDonThanhToan', CONCAT('SoTienDaThu=',FORMAT(x.SoTienDaThu,0),' nhưng SUM(ThanhToan)=',FORMAT(x.PaymentSum,0),'.'),
                   'HoaDon',x.Id,'HoaDon','Details'
            FROM (SELECT hd.Id,hd.SoTienDaThu,COALESCE(SUM(tt.SoTien),0) PaymentSum FROM HoaDon hd LEFT JOIN ThanhToan tt ON tt.HoaDonId=hd.Id GROUP BY hd.Id,hd.SoTienDaThu) x
            WHERE x.SoTienDaThu<>x.PaymentSum

            UNION ALL
            SELECT 'LedgerCoc', CONCAT('SoDuSauGiaoDich=',FORMAT(x.SoDuSauGiaoDich,0),' nhưng số dư chuỗi=',FORMAT(x.ExpectedBalance,0),'.'),
                   'HopDong',x.HopDongId,'GiaoDichCoc','Index'
            FROM (SELECT Id,HopDongId,SoDuSauGiaoDich,SUM(SoTien) OVER(PARTITION BY HopDongId ORDER BY NgayGiaoDich,Id ROWS UNBOUNDED PRECEDING) ExpectedBalance FROM GiaoDichCoc) x
            WHERE x.SoDuSauGiaoDich<>x.ExpectedBalance OR x.ExpectedBalance<0

            UNION ALL
            SELECT 'TrangThaiPhong', CONCAT('Snapshot phòng=',p.TrangThai,', số hợp đồng hiệu lực hôm nay=',COUNT(hd.Id),'.'),
                   'Phong',p.Id,'Phong','Details'
            FROM Phong p LEFT JOIN HopDong hd ON hd.PhongId=p.Id AND hd.TrangThai<>'DaHuy' AND CURDATE() BETWEEN hd.NgayBatDau AND COALESCE(hd.NgayKetThuc,'9999-12-31')
            GROUP BY p.Id,p.TrangThai
            HAVING (COUNT(hd.Id)>0 AND p.TrangThai<>'DangThue') OR (COUNT(hd.Id)=0 AND p.TrangThai NOT IN ('Trong','DangSuaChua')) OR COUNT(hd.Id)>1

            UNION ALL
            SELECT 'TongHoaDon', CONCAT('Tổng chi tiết=',FORMAT(x.DetailTotal,0),', snapshot dịch vụ=',FORMAT(x.TongTienDichVu,0),', tổng cộng=',FORMAT(x.TongCong,0),'.'),
                   'HoaDon',x.Id,'HoaDon','Details'
            FROM (SELECT hd.Id,hd.TienPhong,hd.TongTienDichVu,hd.TongTienPhatSinh,hd.TienNoKyTruoc,hd.TongCong,COALESCE(SUM(ct.ThanhTien),0) DetailTotal FROM HoaDon hd LEFT JOIN ChiTietHoaDon ct ON ct.HoaDonId=hd.Id GROUP BY hd.Id) x
            WHERE x.TongTienDichVu<>x.DetailTotal OR x.TongCong<>x.TienPhong+x.TongTienDichVu+x.TongTienPhatSinh+x.TienNoKyTruoc

            UNION ALL
            SELECT 'LienKetChiSo', 'Chi tiết hóa đơn liên kết chỉ số khác hợp đồng hoặc khác kỳ.',
                   'HoaDon',hd.Id,'HoaDon','Details'
            FROM ChiTietHoaDon ct JOIN HoaDon hd ON hd.Id=ct.HoaDonId JOIN ChiSoDienNuoc cs ON cs.Id=ct.ChiSoDienNuocId
            WHERE cs.HopDongId<>hd.HopDongId OR cs.Thang<>hd.Thang OR cs.Nam<>hd.Nam

            UNION ALL
            SELECT 'LienKetKhoanPhatSinh', 'Khoản phát sinh liên kết hóa đơn có trạng thái/snapshot/scope bất thường.',
                   'HoaDon',hd.Id,'HoaDon','Details'
            FROM KhoanPhatSinhHopDong k JOIN HoaDon hd ON hd.Id=k.HoaDonId
            WHERE k.TrangThai<>'DaDuaVaoHoaDon' OR k.HopDongId<>hd.HopDongId OR k.MoTaHoaDonSnapshot IS NULL OR k.SoTienHoaDonSnapshot IS NULL

            UNION ALL
            SELECT 'MoSoCoc', 'So du coc mo so thieu dot hoac nguon tham chieu.',
                   'HopDong',gd.HopDongId,'GiaoDichCoc','Index'
            FROM GiaoDichCoc gd
            WHERE gd.LoaiGiaoDich='SoDuMoSo'
              AND (gd.DotMoSoId IS NULL OR NULLIF(TRIM(gd.NguonThamChieu),'') IS NULL
                   OR gd.PhuongThuc IS NOT NULL OR gd.HoaDonId IS NOT NULL)

            UNION ALL
            SELECT 'MoSoCongNo', 'Cong no mo so gan sai hop dong hoac lon hon no ky truoc tren hoa don tiep nhan.',
                   'HoaDon',hd.Id,'HoaDon','Details'
            FROM HoaDon hd
            INNER JOIN CongNoMoSo cn ON cn.HoaDonTiepNhanId=hd.Id
            GROUP BY hd.Id,hd.HopDongId,hd.TienNoKyTruoc
            HAVING MIN(cn.HopDongId)<>hd.HopDongId OR MAX(cn.HopDongId)<>hd.HopDongId
                OR SUM(cn.SoTien)>hd.TienNoKyTruoc

            UNION ALL
            SELECT 'MoSoChiSo', 'Moc chi so mo so khong khop phong/hop dong/dich vu dang ky.',
                   'HopDong',cs.HopDongId,'HopDong','Details'
            FROM ChiSoMoSo cs
            INNER JOIN HopDong hop ON hop.Id=cs.HopDongId
            WHERE cs.PhongId<>hop.PhongId
               OR NOT EXISTS (
                    SELECT 1 FROM HopDongDichVu hdv
                    INNER JOIN PhongDichVu pdv ON pdv.Id=hdv.PhongDichVuId
                    INNER JOIN DichVu dv ON dv.Id=pdv.DichVuId
                    WHERE hdv.HopDongId=cs.HopDongId AND pdv.DichVuId=cs.DichVuId
                      AND dv.LoaiTinhPhi='TheoChiSo')

            UNION ALL
            SELECT 'MoSoDaiDien', 'Hop dong mo so khong co dung mot dai dien tai ngay chot.',
                   'HopDong',hm.HopDongId,'HopDong','Details'
            FROM HopDongMoSo hm
            INNER JOIN DotMoSo dot ON dot.Id=hm.DotMoSoId
            LEFT JOIN HopDongKhachThue hdkt ON hdkt.HopDongId=hm.HopDongId
              AND hdkt.LaDaiDien=1 AND hdkt.NgayBatDau<=dot.NgayChot
              AND (hdkt.NgayKetThuc IS NULL OR hdkt.NgayKetThuc>=dot.NgayChot)
            GROUP BY hm.HopDongId
            HAVING COUNT(hdkt.Id)<>1
            ORDER BY Loai,DoiTuongId
            """;
        return await _db.QueryAsync<ReconcileIssue>(sql);
    }

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
