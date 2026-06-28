namespace QuanLyNhaTro.Models;

public class Phong
{
    public int Id { get; set; }
    public int NhaId { get; set; }
    public string TenPhong { get; set; } = string.Empty;
    public decimal? DienTich { get; set; }
    public decimal GiaThueMacDinh { get; set; }       // Giá niêm yết, chỉ dùng làm tham chiếu
    public string TrangThai { get; set; } = "Trong"; // Trong | DangThue | DangSuaChua
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }

    // Navigation (không map DB, dùng trong ViewModel nếu cần)
    public Nha? Nha { get; set; }
}
