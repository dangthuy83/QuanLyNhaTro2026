namespace QuanLyNhaTro.Models;

public class PhieuThuHtmlViewModel
{
    public HoaDon HoaDon { get; set; } = new();
    public HopDong HopDong { get; set; } = new();
    public Phong Phong { get; set; } = new();
    public List<KhachThue> DanhSachKhach { get; set; } = [];
    public List<ChiTietHoaDon> ChiTiet { get; set; } = [];
    public List<ThanhToan> LichSuThanhToan { get; set; } = [];

    public decimal ConLai => HoaDon.TongCong - HoaDon.SoTienDaThu;
    public bool CoButToanPhiTienMat => LichSuThanhToan.Any(tt => tt.HinhThuc is "KetChuyenNo" or "TruCoc");
    public string TenKhach => DanhSachKhach.Count == 0
        ? "(chưa có khách)"
        : string.Join(", ", DanhSachKhach.Select(k => k.HoTen));
}
