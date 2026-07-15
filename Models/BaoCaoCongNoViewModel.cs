namespace QuanLyNhaTro.Models;

public class BaoCaoCongNoViewModel
{
    public int NhaId { get; set; }
    public string TenNha { get; set; } = "";
    public string TenPhong { get; set; } = "";
    public string TenKhachChinh { get; set; } = "";
    public string? SoDienThoai { get; set; }
    public int HoaDonId { get; set; }
    public int Thang { get; set; }
    public int Nam { get; set; }
    public decimal TongCong { get; set; }
    public decimal SoTienDaThu { get; set; }
    public decimal ConLai => TongCong - SoTienDaThu;
    public string TrangThaiHopDong { get; set; } = "";
    public bool DangOHienTai => TrangThaiHopDong == "DangHieuLuc";
    public DateTime NgayDenHan { get; set; }
    public int SoNgayQuaHan { get; set; }
}
