namespace QuanLyNhaTro.Models;

public sealed class MeterOverviewViewModel
{
    public int Thang { get; set; }
    public int Nam { get; set; }
    public List<MeterOverviewRowViewModel> Rows { get; set; } = [];
    public List<string> NhaOptions { get; set; } = [];
}

public sealed class MeterOverviewRowViewModel
{
    public int HopDongId { get; set; }
    public string TenNha { get; set; } = "";
    public string TenPhong { get; set; } = "";
    public string TenKhach { get; set; } = "";
    public string TrangThai { get; set; } = "ChuaNhap";
    public int SoDichVu { get; set; }
    public int SoDaNhap { get; set; }
    public bool CoReset { get; set; }
    public bool DaKhoa { get; set; }
    public List<MeterOverviewServiceViewModel> DichVus { get; set; } = [];
}

public sealed class MeterOverviewServiceViewModel
{
    public string TenDichVu { get; set; } = "";
    public string? DonViTinh { get; set; }
    public bool DaNhap { get; set; }
    public bool LaReset { get; set; }
    public bool DaKhoa { get; set; }
    public decimal? ChiSoDau { get; set; }
    public decimal? ChiSoCuoi { get; set; }
    public decimal? SanLuong { get; set; }
}
