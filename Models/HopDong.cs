using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaTro.Models;

public class HopDong
{
    public int Id { get; set; }
    public int PhongId { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public decimal TienThueThoaThuan { get; set; }  // Giá thỏa thuận — dùng LayGiaApDung khi lập HĐơn
    public decimal TienCoc { get; set; }
    [Range(1, 31, ErrorMessage = "Ngày thanh toán hàng tháng phải từ 1 đến 31.")]
    public int NgayThanhToanHangThang { get; set; } = 5;
    public string TrangThai { get; set; } = "DangHieuLuc"; // ChoHieuLuc | DangHieuLuc | DaKetThuc | DaHuy | DaChuyenPhong
    public int? HopDongTruocId { get; set; }               // Liên kết khi chuyển phòng
    public bool DaXuLyChenhLechCoc { get; set; } = false;  // Đã xử lý chênh lệch cọc chưa
    public DateTime? NgayTraPhongThucTe { get; set; }
    public decimal? TienCocHoanLai { get; set; }
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }

    // Navigation
    public Phong? Phong { get; set; }
    public HopDong? HopDongTruoc { get; set; }
    public KhachThue? KhachDaiDien { get; set; }
    public List<KhachThue> DanhSachKhach { get; set; } = [];
}
