namespace QuanLyNhaTro.Models;

public class CapNhatDichVuHopDongViewModel
{
    public int HopDongId { get; set; }
    public string TenPhong { get; set; } = string.Empty;
    public int ThangApDung { get; set; }
    public int NamApDung { get; set; }
    public int[] PhongDichVuIds { get; set; } = [];
    public List<PhongDichVu> DanhSachDichVu { get; set; } = [];
}
