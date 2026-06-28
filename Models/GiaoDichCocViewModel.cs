namespace QuanLyNhaTro.Models;

public class GiaoDichCocViewModel
{
    public HopDong HopDong { get; set; } = new();
    public decimal SoDuCoc { get; set; }
    public decimal ChenhLechCoc => HopDong.TienCoc - SoDuCoc;
    public bool LaHopDongChuyenPhong => HopDong.HopDongTruocId.HasValue;
    public bool CanXuLyChenhLechCoc => LaHopDongChuyenPhong && !HopDong.DaXuLyChenhLechCoc;
    public IEnumerable<GiaoDichCoc> GiaoDich { get; set; } = [];
}
