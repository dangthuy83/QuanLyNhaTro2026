using System.ComponentModel.DataAnnotations;

namespace QuanLyNhaTro.Models;

public class ThuChi
{
    public int Id { get; set; }

    /// <summary>Thu | Chi</summary>
    [Required, RegularExpression("^(Thu|Chi)$", ErrorMessage = "Loại giao dịch không hợp lệ.")]
    public string LoaiGiaoDich { get; set; } = "Chi";

    /// <summary>Danh mục: Sửa chữa, Mua sắm, Điện chung...</summary>
    [Required, StringLength(100)]
    public string DanhMuc { get; set; } = "";

    [Range(typeof(decimal), "1", "999999999999", ErrorMessage = "Số tiền phải lớn hơn 0.")]
    public decimal SoTien { get; set; }
    [BusinessDate]
    public DateTime NgayPhatSinh { get; set; } = DateTime.Today;
    [StringLength(500)]
    public string? NoiDung { get; set; }

    /// <summary>NULL = thu/chi chung, không gắn phòng cụ thể</summary>
    public int? PhongId { get; set; }

    public string? GhiChu { get; set; }
    public int? ThuChiGocId { get; set; }
    public bool LaDieuChinh => ThuChiGocId.HasValue;
}

public sealed class BusinessDateAttribute : ValidationAttribute
{
    public BusinessDateAttribute() => ErrorMessage = "Ngày giao dịch phải thuộc năm 2000-2100.";
    public override bool IsValid(object? value)
        => value is DateTime date && BusinessDataLimits.IsValidBusinessDate(date);
}

public sealed class ThuChiKySo
{
    public int Nam { get; set; }
    public int Thang { get; set; }
    public string TrangThai { get; set; } = "Mo";
    public DateTime? KhoaLuc { get; set; }
    public string? GhiChu { get; set; }
    public bool DaKhoa => TrangThai == "DaKhoa";
}
