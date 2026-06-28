namespace QuanLyNhaTro.Models;

public class ThanhToan
{
    public int Id { get; set; }
    public int HoaDonId { get; set; }
    public decimal SoTien { get; set; }
    public DateTime NgayThu { get; set; }
    public string? HinhThuc { get; set; }   // TienMat | ChuyenKhoan
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }
}
