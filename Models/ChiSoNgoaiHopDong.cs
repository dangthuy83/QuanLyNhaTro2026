namespace QuanLyNhaTro.Models;

public class ChiSoNgoaiHopDong
{
    public int Id { get; set; }
    public int PhongId { get; set; }
    public int DichVuId { get; set; }
    public decimal TuChiSo { get; set; }
    public decimal DenChiSo { get; set; }
    public decimal SanLuong => DenChiSo - TuChiSo;
    public DateTime NgayGhiNhan { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }

    public Phong? Phong { get; set; }
    public DichVu? DichVu { get; set; }
}
