namespace QuanLyNhaTro.Models;

public class PhieuThuHtmlViewModel
{
    public HoaDon HoaDon { get; set; } = new();
    public List<ChiTietHoaDon> ChiTiet { get; set; } = [];
    public List<KhoanPhatSinhHopDong> KhoanPhatSinh { get; set; } = [];
    public List<ThanhToan> LichSuThanhToan { get; set; } = [];

    public decimal ConLai => HoaDon.SoTienConLai;
    public decimal TienThucThu => HoaDon.TienThucThu;
    public decimal ButToanPhiTienMat => HoaDon.ButToanPhiTienMat;
    public bool CoButToanPhiTienMat => HoaDon.ButToanPhiTienMat > 0;
    public string TenKhach => HoaDon.TenKhachDaiDienSnapshot;
}
