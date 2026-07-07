namespace QuanLyNhaTro.Models;

public class GanDichVuHangLoatViewModel
{
    public int? NhaId { get; set; }
    public int? DichVuId { get; set; }
    public string TrangThai { get; set; } = "DangThue";
    public decimal DonGia { get; set; }
    public List<Nha> DanhSachNha { get; set; } = [];
    public List<DichVu> DanhSachDichVu { get; set; } = [];
    public List<GanDichVuHangLoatRow> Rows { get; set; } = [];

    public DichVu? DichVuDangChon => DanhSachDichVu.FirstOrDefault(x => x.Id == DichVuId);
    public bool LaDichVuTheoNguoi => DichVuDangChon?.LoaiTinhPhi == DichVu.LoaiCoDinh
        && DichVuDangChon.CachTinhCoDinh == DichVu.CachTinhTheoNguoi;
    public bool LaDichVuTheoPhong => DichVuDangChon?.LoaiTinhPhi == DichVu.LoaiCoDinh
        && DichVuDangChon.CachTinhCoDinh == DichVu.CachTinhTheoPhong;
    public bool LaDichVuTheoChiSo => DichVuDangChon?.LoaiTinhPhi == DichVu.LoaiTheoChiSo;
    public int SoPhongDaGan => Rows.Count(x => x.DaGan && x.DangApDung);
    public int SoPhongChuaGan => Rows.Count(x => !x.DaGan || !x.DangApDung);
    public int SoPhongDangThueThieuKhach => LaDichVuTheoNguoi
        ? Rows.Count(x => x.HopDongId.HasValue && x.SoKhach == 0)
        : 0;
}

public class GanDichVuHangLoatRow
{
    public int PhongId { get; set; }
    public string TenNha { get; set; } = "";
    public string TenPhong { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public int? HopDongId { get; set; }
    public int SoKhach { get; set; }
    public int? PhongDichVuId { get; set; }
    public decimal? DonGiaHienTai { get; set; }
    public bool DangApDung { get; set; }

    public bool DaGan => PhongDichVuId.HasValue;
}
