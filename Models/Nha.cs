namespace QuanLyNhaTro.Models;

public class Nha
{
    public int Id { get; set; }
    public string TenNha { get; set; } = string.Empty;
    public string? DiaChi { get; set; }
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }
    public int SoPhong { get; set; }
}
