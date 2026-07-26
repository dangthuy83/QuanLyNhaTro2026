using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaTro.Models;

public class DongHopDongTruocCutoverViewModel
{
    public int HopDongId { get; set; }
    public string TenPhong { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime NgayTraPhong { get; set; } = new(2026, 6, 30);

    [Required]
    [DataType(DataType.Date)]
    public DateTime KyTienPhongDaThanhToanDen { get; set; } = new(2026, 6, 1);

    [Required]
    [DataType(DataType.Date)]
    public DateTime KyDichVuDaThanhToanDen { get; set; } = new(2026, 6, 1);

    [Range(typeof(decimal), "0", "999999999999")]
    public decimal CongNoXacNhan { get; set; }

    [Range(typeof(decimal), "0.01", "999999999999")]
    public decimal SoTienHoanCoc { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime NgayHoanCoc { get; set; } = new(2026, 7, 3);

    [Required, StringLength(255)]
    public string NguonDoiChieu { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string LyDoCutover { get; set; } = "Đóng hợp đồng trước cutover 08/2026";

    public bool XacNhanKhongConCongNo { get; set; }
    public bool XacNhanKhongTaoChiSoCuoi { get; set; }
    public bool DaThucHien { get; set; }
}
