namespace QuanLyNhaTro.Models;

public class LichSuThayDoiGia
{
    public int Id { get; set; }
    public string LoaiDoiTuong { get; set; } = string.Empty; // HopDong | DichVu
    public int DoiTuongId { get; set; }                      // HopDongId hoặc PhongDichVuId
    public decimal GiaCu { get; set; }
    public decimal GiaMoi { get; set; }

    // Kỳ áp dụng — dùng Thang/Nam thay vì ngày để tránh off-by-one postpaid
    public int ThangApDung { get; set; }
    public int NamApDung { get; set; }

    public string? LyDo { get; set; }
    public DateTime NgayTao { get; set; }
}
