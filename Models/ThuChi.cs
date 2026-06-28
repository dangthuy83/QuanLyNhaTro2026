namespace QuanLyNhaTro.Models;

public class ThuChi
{
    public int Id { get; set; }

    /// <summary>Thu | Chi</summary>
    public string LoaiGiaoDich { get; set; } = "Chi";

    /// <summary>Danh mục: Sửa chữa, Mua sắm, Điện chung...</summary>
    public string DanhMuc { get; set; } = "";

    public decimal SoTien { get; set; }
    public DateTime NgayPhatSinh { get; set; } = DateTime.Today;
    public string? NoiDung { get; set; }

    /// <summary>NULL = thu/chi chung, không gắn phòng cụ thể</summary>
    public int? PhongId { get; set; }

    public string? GhiChu { get; set; }
}
