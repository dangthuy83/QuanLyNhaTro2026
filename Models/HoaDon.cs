namespace QuanLyNhaTro.Models;

public class HoaDon
{
    public int Id { get; set; }
    public int HopDongId { get; set; }
    public int Thang { get; set; }
    public int Nam { get; set; }
    public DateTime NgayLap { get; set; }

    // Snapshot — ghi cứng lúc lập, KHÔNG tính lại sau
    public decimal TienPhong { get; set; }
    public decimal TongTienDichVu { get; set; }
    public decimal TienNoKyTruoc { get; set; }  // Âm = khách đang dư
    public decimal TongCong { get; set; }       // = TienPhong + TongTienDichVu + TienNoKyTruoc

    // Thanh toán
    public decimal SoTienDaThu { get; set; }    // Denormalized sum từ ThanhToan — update cùng transaction
    public string TrangThaiThanhToan { get; set; } = "ChuaThu"; // ChuaThu | ThuMotPhan | DaThu

    // Hóa đơn không trọn tháng (NULL = trọn tháng)
    public int? SoNgayO { get; set; }
    public int? SoNgayTrongThang { get; set; }

    // Chuyển phòng
    public int? HoaDonGhepId { get; set; }      // Liên kết 2 hóa đơn tháng chuyển phòng

    public string? GhiChu { get; set; }

    // Navigation
    public HopDong? HopDong { get; set; }
    public List<ChiTietHoaDon> ChiTiet { get; set; } = [];
    public List<ThanhToan> DanhSachThanhToan { get; set; } = [];
}
