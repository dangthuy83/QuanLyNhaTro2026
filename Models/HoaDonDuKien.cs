namespace QuanLyNhaTro.Models;

public class HoaDonDuKien
{
    public int HopDongId { get; set; }
    public int Thang { get; set; }
    public int Nam { get; set; }
    public HopDong? HopDong { get; set; }
    public HoaDon? HoaDonDaCo { get; set; }
    public decimal TienPhong { get; set; }
    public decimal TongTienDichVu { get; set; }
    public decimal TongTienPhatSinh { get; set; }
    public decimal TienNoKyTruoc { get; set; }
    public decimal TongCong { get; set; }
    public int? SoNgayO { get; set; }
    public int? SoNgayTrongThang { get; set; }
    public List<HoaDonDuKienChiTiet> ChiTiet { get; set; } = [];
    public List<HoaDonDuKienKhoanPhatSinh> KhoanPhatSinh { get; set; } = [];
    public List<string> CanhBao { get; set; } = [];
    public List<string> Loi { get; set; } = [];

    public bool CoHoaDonDaCo => HoaDonDaCo != null;
    public bool CoNoKyTruoc => TienNoKyTruoc > 0;
    public bool ThieuDichVu => CanhBao.Any(x =>
        x.Contains("dịch vụ", StringComparison.OrdinalIgnoreCase)
        || x.Contains("dich vu", StringComparison.OrdinalIgnoreCase));
    public bool ThieuChiSo => Loi.Any(x =>
        x.Contains("chỉ số", StringComparison.OrdinalIgnoreCase)
        || x.Contains("chi so", StringComparison.OrdinalIgnoreCase));
    public bool SanSangChot => !CoHoaDonDaCo && Loi.Count == 0;
}

public class HoaDonDuKienKhoanPhatSinh
{
    public int Id { get; set; }
    public DateTime NgayPhatSinh { get; set; }
    public string LoaiKhoan { get; set; } = "";
    public string MoTa { get; set; } = "";
    public decimal SoTien { get; set; }
    public decimal SoTienConLai { get; set; }
}

public class HoaDonDuKienChiTiet
{
    public int DichVuId { get; set; }
    public int? PhongDichVuId { get; set; }
    public int? ChiSoDienNuocId { get; set; }
    public string TenDichVu { get; set; } = "";
    public string LoaiTinhPhi { get; set; } = "";
    public string CachTinhCoDinh { get; set; } = "";
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
    public bool CanChiSo => LoaiTinhPhi == "TheoChiSo";
    public bool CoChiSo => !CanChiSo || ChiSoDienNuocId.HasValue;
}
