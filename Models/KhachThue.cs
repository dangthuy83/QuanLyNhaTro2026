namespace QuanLyNhaTro.Models;

public class KhachThue
{
    public int Id { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? CCCD { get; set; }
    public string? SoDienThoai { get; set; }
    public DateTime? NgaySinh { get; set; }
    public DateTime? NgayCapCCCD { get; set; }
    public string? NgheNghiep { get; set; }
    public string? LoaiXe { get; set; }
    public string? BienSoXe { get; set; }
    public string? QueQuan { get; set; }
    public string? AnhCCCDMatTruoc { get; set; }
    public string? AnhCCCDMatSau { get; set; }
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }
}
