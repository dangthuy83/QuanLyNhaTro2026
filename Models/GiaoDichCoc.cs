namespace QuanLyNhaTro.Models;

public class GiaoDichCoc
{
    public int Id { get; set; }
    public int HopDongId { get; set; }
    public string LoaiGiaoDich { get; set; } = "";
    public decimal SoTien { get; set; }
    public decimal SoDuSauGiaoDich { get; set; }
    public DateTime NgayGiaoDich { get; set; } = DateTime.Today;
    public int? HoaDonId { get; set; }
    public string? PhuongThuc { get; set; }
    public int? DotMoSoId { get; set; }
    public string? NguonThamChieu { get; set; }
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }

    public HopDong? HopDong { get; set; }
    public HoaDon? HoaDon { get; set; }
}
