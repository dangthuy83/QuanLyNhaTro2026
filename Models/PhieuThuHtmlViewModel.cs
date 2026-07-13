namespace QuanLyNhaTro.Models;

public class PhieuThuHtmlViewModel
{
    public HoaDon HoaDon { get; set; } = new();
    public List<ChiTietHoaDon> ChiTiet { get; set; } = [];
    public List<KhoanPhatSinhHopDong> KhoanPhatSinh { get; set; } = [];
    public List<ThanhToan> LichSuThanhToan { get; set; } = [];

    public decimal ConLai => HoaDon.TongCong - HoaDon.SoTienDaThu;
    public bool CoButToanPhiTienMat => LichSuThanhToan.Any(tt => tt.HinhThuc is "KetChuyenNo" or "TruCoc");
    public string TenKhach => HoaDon.TenKhachDaiDienSnapshot;
}
