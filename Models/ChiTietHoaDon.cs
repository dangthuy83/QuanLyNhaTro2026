namespace QuanLyNhaTro.Models;

public class ChiTietHoaDon
{
    public int Id { get; set; }
    public int HoaDonId { get; set; }
    public int DichVuId { get; set; }
    public int? ChiSoDienNuocId { get; set; }
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }

    public string TenDichVu => DichVu?.TenDichVu ?? "";
    public DichVu? DichVu { get; set; }
}
