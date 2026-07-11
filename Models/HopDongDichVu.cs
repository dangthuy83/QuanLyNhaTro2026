namespace QuanLyNhaTro.Models;

public class HopDongDichVu
{
    public int Id { get; set; }
    public int HopDongId { get; set; }
    public int PhongDichVuId { get; set; }
    public DateTime KyBatDau { get; set; }
    public DateTime? KyKetThuc { get; set; }
    public DateTime NgayTao { get; set; }

    public HopDong? HopDong { get; set; }
    public PhongDichVu? PhongDichVu { get; set; }
}
