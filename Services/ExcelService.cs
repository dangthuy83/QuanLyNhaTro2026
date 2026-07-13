using ClosedXML.Excel;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Services;

public class ExcelService
{
    // ── Xuất phiếu thu 1 hóa đơn ─────────────────────────────────────────────
    public byte[] XuatPhieuThu(
        HoaDon hoaDon,
        IEnumerable<ChiTietHoaDon> chiTiet,
        IEnumerable<KhoanPhatSinhHopDong> khoanPhatSinh,
        IEnumerable<ThanhToan> lichSuThanhToan)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Phiếu Thu");

        ws.Cell("A1").Value = "PHIẾU THU TIỀN PHÒNG TRỌ";
        ws.Range("A1:F1").Merge().Style
            .Font.SetBold(true).Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        ws.Cell("A2").Value = $"Kỳ: Tháng {hoaDon.Thang}/{hoaDon.Nam}";
        ws.Range("A2:F2").Merge().Style
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        int row = 4;
        ws.Cell(row, 1).Value = "Phòng:";
        ws.Cell(row, 2).Value = hoaDon.TenPhongSnapshot;
        ws.Cell(row, 4).Value = "Ngày lập:";
        ws.Cell(row, 5).Value = hoaDon.NgayLap.ToString("dd/MM/yyyy");
        row++;
        ws.Cell(row, 1).Value = "Khách thuê:";
        ws.Cell(row, 2).Value = hoaDon.TenKhachDaiDienSnapshot;
        ws.Cell(row, 4).Value = "CCCD:";
        ws.Cell(row, 5).Value = hoaDon.CccdKhachDaiDienSnapshot ?? "";
        row += 2;

        string[] headers = ["STT", "Dịch vụ / Mục", "ĐVT", "Số lượng", "Đơn giá", "Thành tiền"];
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        ws.Row(row).Style.Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.LightGray);
        row++;

        int stt = 1;
        // Dòng tiền phòng
        ws.Cell(row, 1).Value = stt++;
        ws.Cell(row, 2).Value = hoaDon.SoNgayO.HasValue
            ? $"Tiền phòng ({hoaDon.SoNgayO}/{hoaDon.SoNgayTrongThang} ngày)"
            : "Tiền phòng";
        ws.Cell(row, 3).Value = "Tháng";
        ws.Cell(row, 4).Value = 1;
        ws.Cell(row, 5).Value = hoaDon.TienPhong;
        ws.Cell(row, 6).Value = hoaDon.TienPhong;
        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
        ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
        row++;

        // Chi tiết dịch vụ
        foreach (var ct in chiTiet)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 2).Value = ct.TenDichVuSnapshot;
            ws.Cell(row, 3).Value = ct.DonViTinhSnapshot;
            ws.Cell(row, 4).Value = ct.SoLuong;
            ws.Cell(row, 5).Value = ct.DonGia;
            ws.Cell(row, 6).Value = ct.ThanhTien;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
            row++;
        }

        foreach (var khoan in khoanPhatSinh)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 2).Value = khoan.MoTaTrenHoaDon;
            ws.Cell(row, 3).Value = khoan.LoaiKhoan;
            ws.Cell(row, 4).Value = 1;
            ws.Cell(row, 5).Value = khoan.SoTienTrenHoaDon;
            ws.Cell(row, 6).Value = khoan.SoTienTrenHoaDon;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
            row++;
        }

        // Nợ kỳ trước
        if (hoaDon.TienNoKyTruoc != 0)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 2).Value = hoaDon.TienNoKyTruoc > 0 ? "Nợ kỳ trước" : "Thừa kỳ trước";
            ws.Cell(row, 6).Value = hoaDon.TienNoKyTruoc;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
            row++;
        }

        row++;
        ws.Cell(row, 5).Value = "TỔNG CỘNG:";
        ws.Cell(row, 5).Style.Font.SetBold(true);
        ws.Cell(row, 6).Value = hoaDon.TongCong;
        ws.Cell(row, 6).Style.Font.SetBold(true).NumberFormat.Format = "#,##0";
        row++;
        ws.Cell(row, 5).Value = "Đã thu:";
        ws.Cell(row, 6).Value = hoaDon.SoTienDaThu;
        ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
        row++;
        decimal conLai = hoaDon.TongCong - hoaDon.SoTienDaThu;
        ws.Cell(row, 5).Value = conLai > 0 ? "Còn lại:" : "Thừa:";
        ws.Cell(row, 6).Value = Math.Abs(conLai);
        ws.Cell(row, 6).Style.Font.SetBold(true)
            .Font.SetFontColor(conLai > 0 ? XLColor.Red : XLColor.Green)
            .NumberFormat.Format = "#,##0";

        // Lịch sử thanh toán
        if (lichSuThanhToan.Any())
        {
            row += 2;
            ws.Cell(row, 1).Value = "Lịch sử thanh toán:";
            ws.Cell(row, 1).Style.Font.SetBold(true);
            row++;
            foreach (var tt in lichSuThanhToan)
            {
                ws.Cell(row, 1).Value = tt.NgayThu.ToString("dd/MM/yyyy");
                ws.Cell(row, 2).Value = tt.SoTien.ToString("N0") + " đ";
                ws.Cell(row, 3).Value = tt.HinhThuc;
                ws.Cell(row, 4).Value = tt.GhiChu;
                row++;
            }
        }

        ws.Column(1).Width = 6;
        ws.Column(2).Width = 32;
        ws.Column(3).Width = 8;
        ws.Column(4).Width = 10;
        ws.Column(5).Width = 16;
        ws.Column(6).Width = 16;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Xuất sổ thu chi tháng ────────────────────────────────────────────────
    public byte[] XuatThuChi(
        IEnumerable<ThuChi> dsThuChi, int thang, int nam,
        decimal tongThu, decimal tongChi)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Thu Chi");

        ws.Cell("A1").Value = $"SỔ THU CHI — THÁNG {thang}/{nam}";
        ws.Range("A1:F1").Merge().Style
            .Font.SetBold(true).Font.SetFontSize(13)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        int row = 3;
        string[] headers = ["STT", "Ngày", "Loại", "Danh mục", "Nội dung", "Số tiền (đ)"];
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        ws.Row(row).Style.Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.LightGray);
        row++;

        int stt = 1;
        foreach (var tc in dsThuChi)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 2).Value = tc.NgayPhatSinh.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Value = tc.LoaiGiaoDich;
            ws.Cell(row, 4).Value = tc.DanhMuc;
            ws.Cell(row, 5).Value = tc.NoiDung;
            ws.Cell(row, 6).Value = tc.SoTien;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 3).Style.Font.SetFontColor(
                tc.LoaiGiaoDich == "Chi" ? XLColor.Red : XLColor.Green);
            row++;
        }

        row++;
        ws.Cell(row, 5).Value = "Tổng Thu:";
        ws.Cell(row, 6).Value = tongThu;
        ws.Cell(row, 5).Style.Font.SetBold(true);
        ws.Cell(row, 6).Style.Font.SetBold(true).Font.SetFontColor(XLColor.Green)
            .NumberFormat.Format = "#,##0";
        row++;
        ws.Cell(row, 5).Value = "Tổng Chi:";
        ws.Cell(row, 6).Value = tongChi;
        ws.Cell(row, 5).Style.Font.SetBold(true);
        ws.Cell(row, 6).Style.Font.SetBold(true).Font.SetFontColor(XLColor.Red)
            .NumberFormat.Format = "#,##0";
        row++;
        ws.Cell(row, 5).Value = "Cân đối:";
        ws.Cell(row, 6).Value = tongThu - tongChi;
        ws.Cell(row, 5).Style.Font.SetBold(true);
        ws.Cell(row, 6).Style.Font.SetBold(true).NumberFormat.Format = "#,##0";

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Xuất báo cáo công nợ ─────────────────────────────────────────────────
    public byte[] XuatExcelCongNo(IEnumerable<BaoCaoCongNoViewModel> ds)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Công Nợ");

        ws.Cell("A1").Value = $"BÁO CÁO CÔNG NỢ — {DateTime.Today:dd/MM/yyyy}";
        ws.Range("A1:I1").Merge().Style
            .Font.SetBold(true).Font.SetFontSize(13)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        int row = 3;
        string[] headers = ["Nhà", "Phòng", "Khách thuê", "SĐT", "Kỳ", "Tổng cộng", "Đã thu", "Còn lại", "Quá hạn"];
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        ws.Row(row).Style.Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.LightGray);
        row++;

        foreach (var x in ds)
        {
            ws.Cell(row, 1).Value = x.TenNha;
            ws.Cell(row, 2).Value = x.TenPhong;
            ws.Cell(row, 3).Value = x.TenKhachChinh;
            ws.Cell(row, 4).Value = x.SoDienThoai ?? "";
            ws.Cell(row, 5).Value = $"T{x.Thang}/{x.Nam}";
            ws.Cell(row, 6).Value = x.TongCong;
            ws.Cell(row, 7).Value = x.SoTienDaThu;
            ws.Cell(row, 8).Value = x.ConLai;
            ws.Cell(row, 9).Value = x.SoNgayQuaHan > 0 ? $"{x.SoNgayQuaHan} ngày" : "Chưa quá hạn";
            for (int c = 6; c <= 8; c++)
                ws.Cell(row, c).Style.NumberFormat.Format = "#,##0";
            if (!x.DangOHienTai)
                ws.Row(row).Style.Font.SetFontColor(XLColor.Gray);
            row++;
        }

        row++;
        ws.Cell(row, 7).Value = "TỔNG NỢ:";
        ws.Cell(row, 7).Style.Font.SetBold(true);
        ws.Cell(row, 8).Value = ds.Sum(x => x.ConLai);
        ws.Cell(row, 8).Style.Font.SetBold(true).NumberFormat.Format = "#,##0";

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
