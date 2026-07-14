using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaTro.Models;

public class ThayDoiGiaViewModel
{
    public string LoaiDoiTuong { get; set; } = "HopDong"; // HopDong | DichVu
    public int DoiTuongId { get; set; }
    public string TenDoiTuong { get; set; } = "";
    public decimal GiaHienTai { get; set; }

    [Required, Range(1, double.MaxValue, ErrorMessage = "Nhập giá mới")]
    public decimal GiaMoi { get; set; }

    [Required, Range(1, 12)]
    public int ThangApDung { get; set; }

    [Required, Range(BusinessDataLimits.MinYear, BusinessDataLimits.MaxYear)]
    public int NamApDung { get; set; }

    [MaxLength(255)]
    public string? GhiChu { get; set; }

    public IEnumerable<LichSuThayDoiGia> LichSu { get; set; } = [];
}
