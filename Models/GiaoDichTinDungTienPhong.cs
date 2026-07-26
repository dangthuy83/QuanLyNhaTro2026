namespace QuanLyNhaTro.Models;

public class GiaoDichTinDungTienPhong
{
    public long Id { get; set; }
    public int HopDongId { get; set; }
    public int? HoaDonId { get; set; }
    public int? HopDongLienQuanId { get; set; }
    public string LoaiGiaoDich { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
    public decimal SoDuSauGiaoDich { get; set; }
    public DateTime NgayGiaoDich { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string LyDo { get; set; } = string.Empty;
    public string NguoiThucHien { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }
}
