namespace QuanLyNhaTro.Models;

public class DashboardViewModel
{
    // ── Thống kê phòng ───────────────────────────────────────────────────────
    public int TongSoPhong       { get; set; }
    public int SoPhongDangThue   { get; set; }
    public int SoPhongTrong      { get; set; }
    public int SoPhongDangSuaChua { get; set; }

    // ── Thống kê hóa đơn tháng hiện tại ─────────────────────────────────────
    public int     Thang             { get; set; }
    public int     Nam               { get; set; }
    public int     SoHoaDonChuaThu   { get; set; }
    public int     SoHoaDonDaThu     { get; set; }
    public decimal TongPhaiThu       { get; set; }  // Tổng TongCong kỳ này
    public decimal TongDaThu         { get; set; }  // Tổng SoTienDaThu kỳ này
    public decimal TongConLai        => TongPhaiThu - TongDaThu;

    // ── Danh sách actionable ─────────────────────────────────────────────────
    public List<HoaDon>  HoaDonChuaThu  { get; set; } = [];
    public List<Phong>   PhongTrong     { get; set; } = [];
    public List<HopDong> HopDongSapHetHan { get; set; } = []; // Còn ≤ 30 ngày
}
