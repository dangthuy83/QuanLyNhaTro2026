namespace QuanLyNhaTro.Models;

public class DichVu
{
    public const string LoaiCoDinh = "CoDinh";
    public const string LoaiTheoChiSo = "TheoChiSo";

    public int Id { get; set; }
    public string TenDichVu { get; set; } = string.Empty;
    public string LoaiTinhPhi { get; set; } = LoaiCoDinh; // CoDinh | TheoChiSo
    public decimal DonGiaMacDinh { get; set; }
    public string LoaiTinhPhiHienThi => LoaiTinhPhi switch
    {
        LoaiCoDinh => "Cố định",
        LoaiTheoChiSo => "Theo chỉ số",
        _ => LoaiTinhPhi
    };
    public string? DonViTinh { get; set; }
}
