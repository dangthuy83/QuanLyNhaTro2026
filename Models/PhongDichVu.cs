namespace QuanLyNhaTro.Models;

public class PhongDichVu
{
    public int Id { get; set; }
    public int PhongId { get; set; }
    public int DichVuId { get; set; }
    public decimal DonGia { get; set; }
    public bool DangApDung { get; set; } = true;

    public DichVu? DichVu { get; set; }
    public Phong? Phong { get; set; }
}
