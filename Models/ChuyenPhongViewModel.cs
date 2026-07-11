using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaTro.Models;

public class ChuyenPhongViewModel
{
    public int HopDongCuId { get; set; }
    public string TenPhongCu { get; set; } = "";
    public decimal TienCocCu { get; set; }

    [Required(ErrorMessage = "Chọn phòng mới")]
    public int PhongMoiId { get; set; }

    [Required]
    public DateTime NgayChuyenDi { get; set; } = DateTime.Today;

    [Required, Range(1, double.MaxValue, ErrorMessage = "Nhập tiền thuê")]
    public decimal TienThueMoi { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal TienCocMoi { get; set; }

    public DateTime NgayBatDauMoi => NgayChuyenDi.AddDays(1);

    public int[] PhongDichVuIds { get; set; } = [];

    public IEnumerable<Phong> DsPhongTrong { get; set; } = [];
}
