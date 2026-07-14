namespace QuanLyNhaTro.Models;

public class PhongReconcileViewModel
{
    public DateTime NgayDoiChieu { get; set; }
    public List<PhongReconcileRow> Rows { get; set; } = [];

    public int TongSoPhong => Rows.Count;
    public int SoPhongLechTrangThai => Rows.Count(x => x.LechTrangThai);
    public int SoPhongDangSuaXungDot => Rows.Count(x => x.DangSuaXungDot);
    public int SoPhongNhieuHopDong => Rows.Count(x => x.SoHopDongHieuLuc > 1 || x.CoOverlapHopDong);
    public int SoPhongCoLichSu => Rows.Count(x => x.CoDuLieuNghiepVu);
    public int SoPhongTrangThaiLa => Rows.Count(x => x.TrangThaiLa);
    public bool CoBatThuong => Rows.Any(x => x.CanXuLy);
}

public class PhongReconcileRow
{
    public int PhongId { get; set; }
    public int NhaId { get; set; }
    public string TenNha { get; set; } = "";
    public string TenPhong { get; set; } = "";
    public string TrangThaiSnapshot { get; set; } = "";
    public int SoHopDongHieuLuc { get; set; }
    public int SoHopDongTuongLai { get; set; }
    public bool CoOverlapHopDong { get; set; }
    public bool CoDuLieuNghiepVu { get; set; }

    public bool TrangThaiLa => TrangThaiSnapshot is not ("Trong" or "DangThue" or "DangSuaChua");
    public string TrangThaiTheoNgay => SoHopDongHieuLuc > 0
        ? "DangThue"
        : TrangThaiSnapshot == "DangSuaChua"
            ? "DangSuaChua"
            : "Trong";
    public bool LechTrangThai => !TrangThaiLa && TrangThaiSnapshot != TrangThaiTheoNgay;
    public bool DangSuaXungDot => TrangThaiSnapshot == "DangSuaChua"
        && (SoHopDongHieuLuc > 0 || SoHopDongTuongLai > 0);
    public bool CanXuLy => TrangThaiLa || LechTrangThai || DangSuaXungDot
        || SoHopDongHieuLuc > 1 || CoOverlapHopDong;
}
