namespace QuanLyNhaTro.Models;

public class DichVu
{
    public const string LoaiCoDinh = "CoDinh";
    public const string LoaiTheoChiSo = "TheoChiSo";
    public const string CachTinhTheoPhong = "TheoPhong";
    public const string CachTinhTheoNguoi = "TheoNguoi";

    public int Id { get; set; }
    public string TenDichVu { get; set; } = string.Empty;
    public string LoaiTinhPhi { get; set; } = LoaiCoDinh; // CoDinh | TheoChiSo
    public string CachTinhCoDinh { get; set; } = CachTinhTheoPhong; // TheoPhong | TheoNguoi
    public decimal DonGiaMacDinh { get; set; }
    public bool BatBuocKhiThue { get; set; }
    public string LoaiTinhPhiHienThi => LoaiTinhPhi switch
    {
        LoaiCoDinh => "Cố định",
        LoaiTheoChiSo => "Theo chỉ số",
        _ => LoaiTinhPhi
    };
    public string CachTinhCoDinhHienThi => CachTinhCoDinh switch
    {
        CachTinhTheoPhong => "Theo phòng",
        CachTinhTheoNguoi => "Theo người",
        _ => CachTinhCoDinh
    };
    public string? DonViTinh { get; set; }
}
