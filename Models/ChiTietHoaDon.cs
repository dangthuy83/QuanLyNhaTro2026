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
    public string TenDichVuSnapshot { get; set; } = string.Empty;
    public string DonViTinhSnapshot { get; set; } = string.Empty;
    public DateTime KySuDung { get; set; }
    public DateTime? NgayDocSnapshot { get; set; }
    public decimal? ChiSoDauSnapshot { get; set; }
    public decimal? ChiSoCuoiSnapshot { get; set; }
    public string? LoaiGhiNhanSnapshot { get; set; }
    public decimal? ChiSoTruocResetSnapshot { get; set; }
    public decimal? ChiSoSauResetSnapshot { get; set; }
    public string? LyDoDieuChinhSnapshot { get; set; }

    public string TenDichVu => TenDichVuSnapshot;
    public bool LaDichVuTheoChiSo => ChiSoDienNuocId.HasValue;
    public DichVu? DichVu { get; set; }
}
