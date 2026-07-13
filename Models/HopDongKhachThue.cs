namespace QuanLyNhaTro.Models;

/// <summary>Bảng liên kết N-N giữa HopDong và KhachThue</summary>
public class HopDongKhachThue
{
    public int Id { get; set; }
    public int HopDongId { get; set; }
    public int KhachThueId { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime? NgayKetThucDuKien { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public bool LaDaiDien { get; set; } = false; // Người đại diện ký hợp đồng

    // Navigation
    public KhachThue? KhachThue { get; set; }
    public HopDong? HopDong { get; set; }
    public string? TenPhong { get; set; }
    public string? TenNha { get; set; }
    public string? TrangThaiHopDong { get; set; }

    public bool DangCuTru => NgayBatDau.Date <= DateTime.Today
        && (!NgayKetThuc.HasValue || NgayKetThuc.Value.Date >= DateTime.Today);
}
