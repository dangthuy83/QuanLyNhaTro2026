namespace QuanLyNhaTro.Models;

public class KiemTraDuLieuViewModel
{
    public int Thang { get; set; }
    public int Nam { get; set; }
    public int? NhaId { get; set; }
    public string? TuKhoa { get; set; }
    public string TrangThaiDong { get; set; } = "TatCa";
    public List<Nha> DanhSachNha { get; set; } = [];
    public List<KiemTraDuLieuRow> Rows { get; set; } = [];
    public List<ReconcileIssue> CanhBaoDoiSoat { get; set; } = [];

    public int SoDongSanSang => Rows.Count(x => x.SanSangVanHanh);
    public int SoDongCanXuLy => Rows.Count(x => x.CanXuLy);
    public int SoDongThieuKhach => Rows.Count(x => x.ThieuKhach);
    public int SoDongThieuDichVu => Rows.Count(x => x.ThieuDichVu);
    public int SoDongThieuDonGia => Rows.Count(x => x.ThieuDonGia);
    public int SoDongThieuChiSo => Rows.Count(x => x.ThieuChiSo);
    public int SoDongDaCoHoaDon => Rows.Count(x => x.DaCoHoaDon);
}

public class ReconcileIssue
{
    public string Loai { get; set; } = "";
    public string LyDo { get; set; } = "";
    public string DoiTuong { get; set; } = "";
    public int DoiTuongId { get; set; }
    public string? LinkController { get; set; }
    public string? LinkAction { get; set; }
}

public class KiemTraDuLieuRow
{
    public int HopDongId { get; set; }
    public int PhongId { get; set; }
    public int NhaId { get; set; }
    public string TenNha { get; set; } = "";
    public string TenPhong { get; set; } = "";
    public string? TenKhach { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public int SoKhach { get; set; }
    public int SoDichVuDangApDung { get; set; }
    public int SoDichVuTheoChiSo { get; set; }
    public int SoChiSoTheoChiSo { get; set; }
    public int SoDichVuTheoNguoi { get; set; }
    public int SoDichVuDonGiaKhongHopLe { get; set; }
    public string? DichVuTheoChiSo { get; set; }
    public string? DichVuDonGiaKhongHopLe { get; set; }
    public string? DichVuTheoNguoi { get; set; }
    public HoaDonDuKien? DuKien { get; set; }

    public bool ThieuKhach => SoKhach == 0;
    public bool ThieuDichVu => SoDichVuDangApDung == 0 || DuKien?.ThieuDichVu == true;
    public bool ThieuDonGia => SoDichVuDonGiaKhongHopLe > 0;
    public bool ThieuChiSo => SoDichVuTheoChiSo > SoChiSoTheoChiSo || DuKien?.ThieuChiSo == true;
    public bool DaCoHoaDon => DuKien?.CoHoaDonDaCo == true;
    public bool CoLoiHoaDon => DuKien?.Loi.Count > 0;
    public bool CoNoKyTruoc => DuKien?.CoNoKyTruoc == true;
    public bool SanSangChot => DuKien?.SanSangChot == true;
    public bool SanSangVanHanh => !ThieuKhach && !ThieuDichVu && !ThieuDonGia && !ThieuChiSo && !CoLoiHoaDon;
    public bool CanXuLy => !SanSangVanHanh;
}
