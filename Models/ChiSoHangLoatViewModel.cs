namespace QuanLyNhaTro.Models;

public class ChiSoHangLoatViewModel
{
    public int Thang { get; set; }
    public int Nam { get; set; }
    public List<ChiSoHangLoatRowViewModel> Rows { get; set; } = [];
}

public class ChiSoHangLoatRowViewModel
{
    public bool Luu { get; set; } = true;
    public int PhongId { get; set; }
    public int HopDongId { get; set; }
    public DateTime? NgayBatDauHopDong { get; set; }
    public string TenPhong { get; set; } = "";
    public int DichVuId { get; set; }
    public string TenDichVu { get; set; } = "";
    public string? DonViTinh { get; set; }
    public int ChiSoId { get; set; }
    public decimal ChiSoDau { get; set; }
    public decimal ChiSoCuoi { get; set; }
    public DateTime? NgayDoc { get; set; }
    public bool ChoNhapChiSoDau { get; set; }
    public string LoaiGhiNhan { get; set; } = ChiSoDienNuoc.LoaiBinhThuong;
    public decimal? ChiSoTruocReset { get; set; }
    public decimal? ChiSoSauReset { get; set; }
    public string? LyDoDieuChinh { get; set; }
    public bool DaNhap { get; set; }
    public decimal? SanLuongHienTai { get; set; }
}
