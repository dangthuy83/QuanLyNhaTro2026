using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Services;

public class DongHopDongTruocCutoverService(
    IConfiguration configuration,
    PhongLifecycleService phongLifecycle)
{
    public async Task<DongHopDongTruocCutoverViewModel> TaoFormAsync(int hopDongId)
    {
        await using var conn = new MySqlConnection(configuration.GetConnectionString("DefaultConnection"));
        var row = await conn.QueryFirstOrDefaultAsync<CutoverFormRow>(
            """
            SELECT h.Id,p.TenPhong,
                   COALESCE((SELECT SUM(g.SoTien) FROM GiaoDichCoc g WHERE g.HopDongId=h.Id),0) SoDu,
                   a.Id AuditId,a.NgayTraPhong,a.KyTienPhongDaThanhToanDen,
                   a.KyDichVuDaThanhToanDen,a.CongNoXacNhan,a.SoTienHoanCoc,
                   a.NgayHoanCoc,a.NguonDoiChieu,a.LyDoCutover
            FROM HopDong h INNER JOIN Phong p ON p.Id=h.PhongId
            LEFT JOIN AuditDongHopDongTruocCutover a ON a.HopDongId=h.Id
            WHERE h.Id=@HopDongId
            """, new { HopDongId = hopDongId });
        if (row == null) throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        var daThucHien = row.AuditId.HasValue;
        return new DongHopDongTruocCutoverViewModel
        {
            HopDongId = row.Id,
            TenPhong = row.TenPhong,
            NgayTraPhong = row.NgayTraPhong ?? new DateTime(2026, 6, 30),
            KyTienPhongDaThanhToanDen = row.KyTienPhongDaThanhToanDen ?? new DateTime(2026, 6, 1),
            KyDichVuDaThanhToanDen = row.KyDichVuDaThanhToanDen ?? new DateTime(2026, 6, 1),
            CongNoXacNhan = row.CongNoXacNhan ?? 0,
            SoTienHoanCoc = row.SoTienHoanCoc ?? row.SoDu,
            NgayHoanCoc = row.NgayHoanCoc ?? new DateTime(2026, 7, 3),
            NguonDoiChieu = row.NguonDoiChieu ?? string.Empty,
            LyDoCutover = row.LyDoCutover ?? "Đóng hợp đồng trước cutover 08/2026",
            XacNhanKhongConCongNo = daThucHien,
            XacNhanKhongTaoChiSoCuoi = daThucHien,
            DaThucHien = daThucHien
        };
    }

    public async Task ThucHienAsync(DongHopDongTruocCutoverViewModel request, string nguoiThucHien)
    {
        if (!request.XacNhanKhongConCongNo || request.CongNoXacNhan != 0)
            throw new InvalidOperationException("Phải xác nhận công nợ bằng 0.");
        if (!request.XacNhanKhongTaoChiSoCuoi)
            throw new InvalidOperationException("Phải xác nhận không tạo chỉ số cuối/ngoài hợp đồng giả.");
        var kyTienPhong = BillingCollectionPeriodPolicy.NormalizeMonth(
            request.KyTienPhongDaThanhToanDen, nameof(request.KyTienPhongDaThanhToanDen));
        var kyDichVu = BillingCollectionPeriodPolicy.NormalizeMonth(
            request.KyDichVuDaThanhToanDen, nameof(request.KyDichVuDaThanhToanDen));
        if (request.NgayTraPhong.Date >= BillingCollectionPeriodPolicy.CutoverPeriod)
            throw new InvalidOperationException("Ngày trả phòng phải trước 01/08/2026.");
        if (string.IsNullOrWhiteSpace(request.NguonDoiChieu)
            || string.IsNullOrWhiteSpace(request.LyDoCutover)
            || string.IsNullOrWhiteSpace(nguoiThucHien))
            throw new InvalidOperationException("Nguồn đối chiếu, lý do và người thực hiện là bắt buộc.");

        await using var conn = new MySqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var hopDong = await conn.QueryFirstOrDefaultAsync<HopDong>(
                "SELECT * FROM HopDong WHERE Id=@Id FOR UPDATE",
                new { Id = request.HopDongId }, tx)
                ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
            await phongLifecycle.KhoaPhongAsync(conn, tx, hopDong.PhongId);

            if (await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM AuditDongHopDongTruocCutover WHERE HopDongId=@Id FOR UPDATE",
                    new { Id = hopDong.Id }, tx) > 0)
                throw new InvalidOperationException("Hợp đồng đã được đóng bằng workflow cutover; không thể chạy lại.");
            if (hopDong.TrangThai != "DangHieuLuc")
                throw new InvalidOperationException("Chỉ hợp đồng đang hiệu lực mới được đóng trước cutover.");
            if (request.NgayTraPhong.Date < hopDong.NgayBatDau.Date)
                throw new InvalidOperationException("Ngày trả phòng không hợp lệ.");

            var blockers = await conn.QuerySingleAsync<CutoverBlockers>(
                """
                SELECT
                  (SELECT COUNT(*) FROM HoaDon WHERE HopDongId=@Id AND SoTienDaThu<TongCong) HoaDonConNo,
                  (SELECT COUNT(*) FROM KhoanPhatSinhHopDong
                   WHERE HopDongId=@Id AND TrangThai NOT IN ('DaThu','DaTruCoc','DaHuy')) KhoanPhatSinhChuaXuLy,
                  (SELECT COUNT(*) FROM CongNoMoSo WHERE HopDongId=@Id AND HoaDonTiepNhanId IS NULL) CongNoMoSo,
                  (SELECT COUNT(*) FROM ChiSoDienNuoc WHERE HopDongId=@Id) ChiSoHopDong
                """, new { Id = hopDong.Id }, tx);
            if (blockers.HoaDonConNo + blockers.KhoanPhatSinhChuaXuLy + blockers.CongNoMoSo > 0)
                throw new InvalidOperationException("Hợp đồng còn hóa đơn/công nợ/khoản phát sinh chưa xử lý.");
            if (blockers.ChiSoHopDong > 0)
                throw new InvalidOperationException("Workflow cutover không áp dụng khi hợp đồng đã có chỉ số trong ứng dụng.");

            var soDuCoc = await conn.ExecuteScalarAsync<decimal>(
                "SELECT COALESCE(SUM(SoTien),0) FROM GiaoDichCoc WHERE HopDongId=@Id",
                new { Id = hopDong.Id }, tx);
            if (soDuCoc != request.SoTienHoanCoc || soDuCoc <= 0)
                throw new InvalidOperationException(
                    $"Tiền hoàn cọc phải khớp chính xác số dư ledger {soDuCoc:N0} đ.");

            await conn.ExecuteAsync(
                """
                UPDATE HopDong
                SET TrangThai='DaKetThuc',NgayKetThuc=@Ngay,NgayTraPhongThucTe=@Ngay,
                    TienCocHoanLai=@TienHoan,
                    GhiChu=CONCAT(COALESCE(GhiChu,''),' [Cutover: ',@LyDo,']')
                WHERE Id=@Id
                """,
                new
                {
                    Id = hopDong.Id,
                    Ngay = request.NgayTraPhong.Date,
                    TienHoan = request.SoTienHoanCoc,
                    LyDo = request.LyDoCutover.Trim()
                }, tx);
            await conn.ExecuteAsync(
                """
                UPDATE HopDongKhachThue
                SET NgayKetThuc=@Ngay
                WHERE HopDongId=@Id AND (NgayKetThuc IS NULL OR NgayKetThuc>@Ngay)
                """, new { Id = hopDong.Id, Ngay = request.NgayTraPhong.Date }, tx);
            var kyKetThucDichVu = new DateTime(
                request.NgayTraPhong.Year, request.NgayTraPhong.Month, 1).AddMonths(1);
            await conn.ExecuteAsync(
                """
                UPDATE HopDongDichVu
                SET KyKetThuc=@KyKetThuc
                WHERE HopDongId=@Id AND (KyKetThuc IS NULL OR KyKetThuc>@KyKetThuc)
                """, new { Id = hopDong.Id, KyKetThuc = kyKetThucDichVu }, tx);

            await conn.ExecuteAsync(
                """
                INSERT INTO GiaoDichCoc
                    (HopDongId,LoaiGiaoDich,SoTien,SoDuSauGiaoDich,NgayGiaoDich,
                     PhuongThuc,NguonDoiChieu,GhiChu)
                VALUES
                    (@Id,'HoanCoc',-@SoTien,0,@Ngay,NULL,@Nguon,@GhiChu)
                """,
                new
                {
                    Id = hopDong.Id,
                    SoTien = request.SoTienHoanCoc,
                    Ngay = request.NgayHoanCoc.Date,
                    Nguon = request.NguonDoiChieu.Trim(),
                    GhiChu = request.LyDoCutover.Trim()
                }, tx);

            await PhongLifecycleService.DongBoTrangThaiTheoNgayAsync(
                conn, tx, hopDong.PhongId, DateTime.Today);
            await conn.ExecuteAsync(
                """
                INSERT INTO AuditDongHopDongTruocCutover
                    (HopDongId,NgayTraPhong,KyTienPhongDaThanhToanDen,KyDichVuDaThanhToanDen,
                     CongNoXacNhan,SoTienHoanCoc,NgayHoanCoc,NguonDoiChieu,
                     NguoiThucHien,LyDoCutover,IdempotencyKey)
                VALUES
                    (@HopDongId,@NgayTraPhong,@KyTienPhong,@KyDichVu,0,@SoTienHoanCoc,
                     @NgayHoanCoc,@Nguon,@Nguoi,@LyDo,@Key)
                """,
                new
                {
                    HopDongId = hopDong.Id,
                    NgayTraPhong = request.NgayTraPhong.Date,
                    KyTienPhong = kyTienPhong,
                    KyDichVu = kyDichVu,
                    request.SoTienHoanCoc,
                    NgayHoanCoc = request.NgayHoanCoc.Date,
                    Nguon = request.NguonDoiChieu.Trim(),
                    Nguoi = nguoiThucHien.Trim(),
                    LyDo = request.LyDoCutover.Trim(),
                    Key = $"DONG_TRUOC_CUTOVER:{hopDong.Id}"
                }, tx);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private sealed class CutoverBlockers
    {
        public int HoaDonConNo { get; init; }
        public int KhoanPhatSinhChuaXuLy { get; init; }
        public int CongNoMoSo { get; init; }
        public int ChiSoHopDong { get; init; }
    }

    private sealed class CutoverFormRow
    {
        public int Id { get; init; }
        public string TenPhong { get; init; } = string.Empty;
        public decimal SoDu { get; init; }
        public long? AuditId { get; init; }
        public DateTime? NgayTraPhong { get; init; }
        public DateTime? KyTienPhongDaThanhToanDen { get; init; }
        public DateTime? KyDichVuDaThanhToanDen { get; init; }
        public decimal? CongNoXacNhan { get; init; }
        public decimal? SoTienHoanCoc { get; init; }
        public DateTime? NgayHoanCoc { get; init; }
        public string? NguonDoiChieu { get; init; }
        public string? LyDoCutover { get; init; }
    }
}
