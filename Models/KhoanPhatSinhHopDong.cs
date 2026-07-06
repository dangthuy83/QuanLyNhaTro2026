namespace QuanLyNhaTro.Models;

public class KhoanPhatSinhHopDong
{
    public const string TrangThaiChuaXuLy = "ChuaXuLy";
    public const string TrangThaiDaDuaVaoHoaDon = "DaDuaVaoHoaDon";
    public const string TrangThaiDaThu = "DaThu";
    public const string TrangThaiDaTruCoc = "DaTruCoc";
    public const string TrangThaiDaHuy = "DaHuy";

    public int Id { get; set; }
    public int HopDongId { get; set; }
    public int? HoaDonId { get; set; }
    public DateTime NgayPhatSinh { get; set; } = DateTime.Today;
    public string LoaiKhoan { get; set; } = "Khac";
    public string MoTa { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
    public decimal SoTienDaXuLy { get; set; }
    public string TrangThai { get; set; } = TrangThaiChuaXuLy;
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }

    public decimal SoTienConLai => Math.Max(0, SoTien - SoTienDaXuLy);
    public bool ConPhaiXuLy => TrangThai == TrangThaiChuaXuLy && SoTienConLai > 0;

    public HopDong? HopDong { get; set; }
    public HoaDon? HoaDon { get; set; }
}
