using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public sealed class MoSoService(
    IDbConnection db,
    HopDongRepository hopDongRepo,
    HopDongKhachThueRepository hopDongKhachRepo,
    PhongDichVuRepository phongDichVuRepo,
    HopDongDichVuRepository hopDongDichVuRepo,
    GiaoDichCocService giaoDichCocService,
    PhongLifecycleService phongLifecycle)
{
    public async Task<int> TaoDotMoSoAsync(DotMoSo dot)
    {
        ValidateDotMoSo(dot);
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        try
        {
            return await conn.ExecuteScalarAsync<int>(
                """
                INSERT INTO DotMoSo(NgayChot,TenNguon,Sha256,NguoiDuyet,GhiChu)
                VALUES(@NgayChot,@TenNguon,@Sha256,@NguoiDuyet,@GhiChu);
                SELECT LAST_INSERT_ID();
                """, new
                {
                    NgayChot = dot.NgayChot.Date,
                    TenNguon = dot.TenNguon.Trim(),
                    Sha256 = dot.Sha256.Trim().ToUpperInvariant(),
                    NguoiDuyet = dot.NguoiDuyet.Trim(),
                    dot.GhiChu
                });
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new InvalidOperationException("Nguon mo so nay da duoc ghi nhan; khong duoc import lai.", ex);
        }
    }

    public async Task<int> TaoHopDongAsync(MoSoHopDongRequest request)
    {
        ValidateRequest(request);
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var dot = await conn.QueryFirstOrDefaultAsync<DotMoSo>(
                "SELECT * FROM DotMoSo WHERE Id=@Id FOR UPDATE",
                new { Id = request.DotMoSoId }, tx)
                ?? throw new InvalidOperationException("Khong tim thay dot mo so.");
            var hopDong = request.HopDong;
            if (hopDong.NgayBatDau.Date > dot.NgayChot.Date)
                throw new InvalidOperationException("Hop dong mo so phai bat dau khong sau ngay chot.");
            if (hopDong.NgayKetThuc.HasValue && hopDong.NgayKetThuc.Value.Date < dot.NgayChot.Date)
                throw new InvalidOperationException("Hop dong da ket thuc truoc ngay chot, khong thuoc danh sach dang van hanh.");

            var phong = await phongLifecycle.KhoaPhongAsync(conn, tx, hopDong.PhongId);
            PhongLifecycleService.DamBaoKhongDangSua(phong);
            if (await hopDongRepo.CoChongKhoangAsync(
                    conn, tx, hopDong.PhongId, hopDong.NgayBatDau, hopDong.NgayKetThuc))
                throw new InvalidOperationException("Phong da co hop dong chiem dung trong khoang thoi gian nay.");

            var selectedIds = request.PhongDichVuIds.Distinct().ToHashSet();
            var services = await phongDichVuRepo.GetSelectedForPhongAsync(
                conn, tx, hopDong.PhongId, selectedIds, requireActive: true);
            if (services.Count != selectedIds.Count)
                throw new InvalidOperationException("Danh sach dich vu co muc khong thuoc phong hoac da ngung ap dung.");
            var required = await phongDichVuRepo.GetRequiredIdsForPhongAsync(conn, tx, hopDong.PhongId);
            if (required.Except(selectedIds).Any())
                throw new InvalidOperationException("Phai chon day du cac dich vu bat buoc cua phong.");

            var meterServiceIds = services
                .Where(x => x.DichVu?.LoaiTinhPhi == "TheoChiSo")
                .Select(x => x.DichVuId)
                .ToHashSet();
            var suppliedMeterIds = request.ChiSo.Select(x => x.DichVuId).ToHashSet();
            if (!meterServiceIds.SetEquals(suppliedMeterIds))
                throw new InvalidOperationException("Moi dich vu theo chi so phai co dung mot moc chi so mo so.");

            hopDong.TrangThai = hopDong.NgayBatDau.Date > DateTime.Today
                ? "ChoHieuLuc"
                : "DangHieuLuc";
            var hopDongId = await hopDongRepo.InsertAsync(conn, tx, hopDong);

            await conn.ExecuteAsync(
                """
                INSERT INTO HopDongMoSo(DotMoSoId,HopDongId,NguonThamChieu)
                VALUES(@DotMoSoId,@HopDongId,@NguonThamChieu)
                """, new
                {
                    request.DotMoSoId,
                    HopDongId = hopDongId,
                    NguonThamChieu = request.NguonThamChieu.Trim()
                }, tx);

            foreach (var tenantId in request.KhachThueIds.Distinct())
            {
                await hopDongKhachRepo.InsertAsync(
                    conn, tx, hopDongId, tenantId,
                    hopDong.NgayBatDau, hopDong.NgayKetThuc,
                    tenantId == request.KhachDaiDienId);
            }
            await hopDongDichVuRepo.InsertManyAsync(
                conn, tx, hopDongId, services.Select(x => x.Id), hopDong.NgayBatDau);

            await giaoDichCocService.GhiNhanSoDuMoSoAsync(
                conn, tx, hopDongId, request.SoDuCocThucTe,
                dot.NgayChot, request.DotMoSoId, request.NguonThamChieu);

            foreach (var debt in request.CongNo)
            {
                await conn.ExecuteAsync(
                    """
                    INSERT INTO CongNoMoSo
                        (DotMoSoId,HopDongId,SoTien,DenKyThang,DenKyNam,MaChungTu,NguonThamChieu)
                    VALUES
                        (@DotMoSoId,@HopDongId,@SoTien,@DenKyThang,@DenKyNam,@MaChungTu,@NguonThamChieu)
                    """, new
                    {
                        request.DotMoSoId,
                        HopDongId = hopDongId,
                        debt.SoTien,
                        debt.DenKyThang,
                        debt.DenKyNam,
                        MaChungTu = debt.MaChungTu.Trim(),
                        NguonThamChieu = debt.NguonThamChieu.Trim()
                    }, tx);
            }

            foreach (var meter in request.ChiSo)
            {
                await conn.ExecuteAsync(
                    """
                    INSERT INTO ChiSoMoSo
                        (DotMoSoId,HopDongId,PhongId,DichVuId,NgayChot,ChiSo,NguonThamChieu)
                    VALUES
                        (@DotMoSoId,@HopDongId,@PhongId,@DichVuId,@NgayChot,@ChiSo,@NguonThamChieu)
                    """, new
                    {
                        request.DotMoSoId,
                        HopDongId = hopDongId,
                        hopDong.PhongId,
                        meter.DichVuId,
                        NgayChot = dot.NgayChot.Date,
                        meter.ChiSo,
                        NguonThamChieu = meter.NguonThamChieu.Trim()
                    }, tx);
            }

            await PhongLifecycleService.DongBoTrangThaiTheoNgayAsync(
                conn, tx, hopDong.PhongId, DateTime.Today);
            await tx.CommitAsync();
            return hopDongId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static void ValidateDotMoSo(DotMoSo dot)
    {
        if (!BusinessDataLimits.IsValidBusinessDate(dot.NgayChot))
            throw new InvalidOperationException("Ngay chot mo so phai nam trong dai 2000-2100.");
        if (string.IsNullOrWhiteSpace(dot.TenNguon) || string.IsNullOrWhiteSpace(dot.NguoiDuyet))
            throw new InvalidOperationException("Dot mo so phai co ten nguon va nguoi duyet.");
        if (!Regex.IsMatch(dot.Sha256?.Trim() ?? "", "^[0-9A-Fa-f]{64}$"))
            throw new InvalidOperationException("SHA-256 nguon mo so khong hop le.");
    }

    private static void ValidateRequest(MoSoHopDongRequest request)
    {
        var hopDong = request.HopDong ?? throw new InvalidOperationException("Thieu hop dong mo so.");
        if (request.DotMoSoId <= 0 || string.IsNullOrWhiteSpace(request.NguonThamChieu))
            throw new InvalidOperationException("Hop dong mo so phai co dot va nguon tham chieu.");
        if (!BusinessDataLimits.IsValidBusinessDate(hopDong.NgayBatDau)
            || (hopDong.NgayKetThuc.HasValue && !BusinessDataLimits.IsValidBusinessDate(hopDong.NgayKetThuc.Value)))
            throw new InvalidOperationException("Ngay hop dong phai nam trong dai 2000-2100.");
        if (hopDong.NgayKetThuc.HasValue && hopDong.NgayKetThuc.Value.Date < hopDong.NgayBatDau.Date)
            throw new InvalidOperationException("Ngay ket thuc khong duoc truoc ngay bat dau.");
        if (hopDong.TienThueThoaThuan <= 0 || hopDong.TienCoc < 0 || request.SoDuCocThucTe < 0)
            throw new InvalidOperationException("Tien thue phai duong; coc thoa thuan va so du coc thuc te khong duoc am.");
        if (hopDong.NgayThanhToanHangThang is < 1 or > 31)
            throw new InvalidOperationException("Ngay thanh toan hang thang phai tu 1 den 31.");
        var tenants = request.KhachThueIds.Distinct().ToHashSet();
        if (tenants.Count == 0 || !tenants.Contains(request.KhachDaiDienId))
            throw new InvalidOperationException("Hop dong mo so phai co dung dai dien trong danh sach cu tru.");
        if (request.CongNo.Any(x => x.SoTien <= 0
            || !BusinessDataLimits.IsValidPeriod(x.DenKyThang, x.DenKyNam)
            || string.IsNullOrWhiteSpace(x.MaChungTu)
            || string.IsNullOrWhiteSpace(x.NguonThamChieu)))
            throw new InvalidOperationException("Cong no mo so phai duong, co ky, chung tu va nguon tham chieu.");
        if (request.ChiSo.Any(x => x.ChiSo < 0 || string.IsNullOrWhiteSpace(x.NguonThamChieu))
            || request.ChiSo.Select(x => x.DichVuId).Distinct().Count() != request.ChiSo.Count)
            throw new InvalidOperationException("Chi so mo so phai khong am, khong trung dich vu va co nguon tham chieu.");
    }
}
