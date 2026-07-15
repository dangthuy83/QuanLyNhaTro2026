using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaTro.Models;

public class TraPhongViewModel
{
    public int HopDongId { get; set; }
    public string TenPhong { get; set; } = "";
    public string TenKhachChinh { get; set; } = "";
    public decimal TienCoc { get; set; }

    [Required]
    public DateTime NgayTraPhong { get; set; } = DateTime.Today;

    public string? GhiChu { get; set; }

    // Preview
    public bool CoTheTraPhong { get; set; } = true;
    public string? LyDoChanTraPhong { get; set; }
    public int? HoaDonKyTraPhongId { get; set; }
    public bool CanSinhHoaDonMoi { get; set; }
    public int SoNgayO { get; set; }
    public int SoNgayTrongThang { get; set; }
    public decimal TienPhongProRata { get; set; }
    public decimal TongTienDichVuThangCuoi { get; set; }
    public decimal TongTienPhatSinhChuaXuLy { get; set; }
    public decimal TongNoConLai { get; set; }
    public decimal TienTruNoTuCoc { get; set; }
    public decimal TienHoanCoc { get; set; }
    public decimal KhachConNoThem { get; set; }
}

public class KetQuaTraPhongViewModel
{
    public int PhongId { get; set; }
    public string TenPhong { get; set; } = "";
    public string TenKhachChinh { get; set; } = "";
    public DateTime NgayTraPhong { get; set; }
    public int? HoaDonCuoiId { get; set; }
    public decimal TienCoc { get; set; }
    public decimal TongNoConLai { get; set; }
    public decimal TongTienPhatSinhConLai { get; set; }
    public decimal TienTruNoTuCoc { get; set; }
    public decimal TienHoanCoc { get; set; }
    public decimal KhachConNoThem { get; set; }
    public bool CoNoTon { get; set; }
}
