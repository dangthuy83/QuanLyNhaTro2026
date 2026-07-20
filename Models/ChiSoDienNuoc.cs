namespace QuanLyNhaTro.Models;

public class ChiSoDienNuoc
{
    public const string LoaiBinhThuong = "BinhThuong";
    public const string LoaiReset = "Reset";

    public int Id { get; set; }
    public int? HopDongId { get; set; }
    public int PhongId { get; set; }
    public int DichVuId { get; set; }
    public int Thang { get; set; }
    public int Nam { get; set; }
    public decimal ChiSoDau { get; set; }
    public decimal ChiSoCuoi { get; set; }
    public string LoaiGhiNhan { get; set; } = LoaiBinhThuong;
    public decimal? ChiSoTruocReset { get; set; }
    public decimal? ChiSoSauReset { get; set; }
    public string? LyDoDieuChinh { get; set; }
    public bool LaReset => LoaiGhiNhan == LoaiReset;
    public bool HopLe => LaReset
        ? ChiSoTruocReset.HasValue
            && ChiSoTruocReset.Value >= ChiSoDau
            && ChiSoCuoi >= (ChiSoSauReset ?? 0)
        : ChiSoCuoi >= ChiSoDau;
    public decimal SoLuongTieuThu => HopLe
        ? LaReset
            ? (ChiSoTruocReset!.Value - ChiSoDau) + (ChiSoCuoi - (ChiSoSauReset ?? 0))
            : ChiSoCuoi - ChiSoDau
        : 0;
    public DateTime? NgayDoc { get; set; }
    public string? GhiChu { get; set; }
    public bool DaDungTrenHoaDon { get; set; }

    public DichVu? DichVu { get; set; }
    public HopDong? HopDong { get; set; }
}
