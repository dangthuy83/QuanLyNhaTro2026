namespace QuanLyNhaTro.Models;

public class GiaoDichCocViewModel
{
    public HopDong HopDong { get; set; } = new();
    public decimal SoDuCoc { get; set; }
    public IEnumerable<GiaoDichCoc> GiaoDich { get; set; } = [];
}
