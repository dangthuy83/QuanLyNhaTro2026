namespace QuanLyNhaTro.Models;

public class ChiSoNgoaiHopDong
{
    public const string LoaiBinhThuong = ChiSoDienNuoc.LoaiBinhThuong;
    public const string LoaiReset = ChiSoDienNuoc.LoaiReset;

    public int Id { get; set; }
    public int PhongId { get; set; }
    public int DichVuId { get; set; }
    public decimal TuChiSo { get; set; }
    public decimal DenChiSo { get; set; }
    public string LoaiGhiNhan { get; set; } = LoaiBinhThuong;
    public decimal? ChiSoTruocReset { get; set; }
    public decimal? ChiSoSauReset { get; set; }
    public string? LyDoDieuChinh { get; set; }
    public bool LaReset => LoaiGhiNhan == LoaiReset;
    public decimal SanLuong => LaReset && ChiSoTruocReset.HasValue
        ? (ChiSoTruocReset.Value - TuChiSo) + (DenChiSo - (ChiSoSauReset ?? 0))
        : DenChiSo - TuChiSo;
    public DateTime NgayGhiNhan { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }
    public string? TenNha { get; set; }

    public Phong? Phong { get; set; }
    public DichVu? DichVu { get; set; }
}
