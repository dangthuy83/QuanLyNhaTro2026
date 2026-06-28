namespace QuanLyNhaTro.Models;

public class HopDong
{
    public int Id { get; set; }
    public int PhongId { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public decimal TienThueThoaThuan { get; set; }  // Giá thỏa thuận — dùng LayGiaApDung khi lập HĐơn
    public decimal TienCoc { get; set; }
    public string TrangThai { get; set; } = "DangHieuLuc"; // DangHieuLuc | DaKetThuc | DaHuy | DaChuyenPhong
    public int? HopDongTruocId { get; set; }               // Liên kết khi chuyển phòng
    public bool DaXuLyChenhLechCoc { get; set; } = false;  // Đã xử lý chênh lệch cọc chưa
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }

    // Navigation
    public Phong? Phong { get; set; }
    public HopDong? HopDongTruoc { get; set; }
    public List<KhachThue> DanhSachKhach { get; set; } = [];
}
