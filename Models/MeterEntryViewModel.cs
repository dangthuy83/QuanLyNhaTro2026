namespace QuanLyNhaTro.Models;

public sealed class MeterEntryViewModel
{
    public int Thang { get; set; }
    public int Nam { get; set; }
    public string PostAction { get; set; } = "Nhap";
    public string CancelUrl { get; set; } = "";
    public HopDong? HopDong { get; set; }
    public Phong? Phong { get; set; }
    public int? ReturnHopDongId { get; set; }
    public List<DichVu> DichVuTheoChiSo { get; set; } = [];
    public Dictionary<int, ChiSoDienNuoc> ChiSoHienTai { get; set; } = [];
    public Dictionary<int, decimal> ChiSoDauTheoDichVu { get; set; } = [];
    public Dictionary<int, bool> ChoNhapChiSoDauTheoDichVu { get; set; } = [];
    public Dictionary<int, string> NguonChiSoDauTheoDichVu { get; set; } = [];
    public bool NgoaiHopDong => HopDong == null;
    public bool CoDuLieuDaKhoa => ChiSoHienTai.Values.Any(item => item.DaDungTrenHoaDon);
}
