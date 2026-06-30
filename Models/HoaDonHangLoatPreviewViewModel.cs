namespace QuanLyNhaTro.Models;

public class HoaDonHangLoatPreviewViewModel
{
    public int Thang { get; set; }
    public int Nam { get; set; }
    public List<HoaDonDuKien> Rows { get; set; } = [];

    public int SoDongSanSang => Rows.Count(x => x.SanSangChot);
    public int SoDongCanKiemTra => Rows.Count(x => !x.SanSangChot);
    public decimal TongDuKien => Rows.Where(x => x.SanSangChot).Sum(x => x.TongCong);
}
