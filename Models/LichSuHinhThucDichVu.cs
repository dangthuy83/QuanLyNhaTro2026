namespace QuanLyNhaTro.Models;

public class LichSuHinhThucDichVu
{
    public int Id { get; set; }
    public int DichVuId { get; set; }
    public string LoaiTinhPhiCu { get; set; } = DichVu.LoaiCoDinh;
    public string CachTinhCoDinhCu { get; set; } = DichVu.CachTinhTheoPhong;
    public string LoaiTinhPhiMoi { get; set; } = DichVu.LoaiCoDinh;
    public string CachTinhCoDinhMoi { get; set; } = DichVu.CachTinhTheoPhong;
    public DateTime KyApDung { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }
}

public class ThayDoiHinhThucDichVuViewModel
{
    public int DichVuId { get; set; }
    public DichVu? DichVu { get; set; }
    public string LoaiTinhPhiMoi { get; set; } = Models.DichVu.LoaiCoDinh;
    public string CachTinhCoDinhMoi { get; set; } = Models.DichVu.CachTinhTheoPhong;
    public int ThangApDung { get; set; } = DateTime.Today.Month;
    public int NamApDung { get; set; } = DateTime.Today.Year;
    public string LyDo { get; set; } = string.Empty;
    public List<ChiSoDauChuyenDoiRow> PhongLienQuan { get; set; } = [];
    public List<LichSuHinhThucDichVu> LichSu { get; set; } = [];
}

public class ChiSoDauChuyenDoiRow
{
    public int PhongId { get; set; }
    public string TenPhong { get; set; } = string.Empty;
    public decimal? ChiSoDau { get; set; }
}
