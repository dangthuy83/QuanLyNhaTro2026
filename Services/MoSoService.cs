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

    public async Task ValidateImportBatchAsync(MoSoImportBatch batch)
    {
        ValidateBatchShape(batch);
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await conn.ExecuteAsync("SET TRANSACTION READ ONLY");
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        try
        {
            await ValidateBatchAgainstDatabaseAsync(conn, tx, batch, lockRooms: false);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }

    public async Task<IReadOnlyList<int>> ApplyImportBatchAsync(
        MoSoImportBatch batch,
        int? rehearsalFailureAfterContracts = null)
    {
        ValidateBatchShape(batch);
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await ValidateBatchAgainstDatabaseAsync(conn, tx, batch, lockRooms: true);
            var dotId = await conn.ExecuteScalarAsync<int>(
                """
                INSERT INTO DotMoSo(NgayChot,TenNguon,Sha256,NguoiDuyet,GhiChu)
                VALUES(@NgayChot,@TenNguon,@Sha256,@NguoiDuyet,@GhiChu);
                SELECT LAST_INSERT_ID();
                """, new
                {
                    NgayChot = batch.DotMoSo.NgayChot.Date,
                    TenNguon = batch.DotMoSo.TenNguon.Trim(),
                    Sha256 = batch.DotMoSo.Sha256.Trim().ToUpperInvariant(),
                    NguoiDuyet = batch.DotMoSo.NguoiDuyet.Trim(),
                    batch.DotMoSo.GhiChu
                }, tx);

            var ids = new List<int>();
            foreach (var request in batch.HopDong.OrderBy(x => x.HopDong.PhongId))
            {
                request.DotMoSoId = dotId;
                ids.Add(await TaoHopDongTrongBatchAsync(conn, tx, request, batch.DotMoSo.NgayChot));
                if (rehearsalFailureAfterContracts == ids.Count)
                    throw new InvalidOperationException("REHEARSAL simulated crash before commit.");
            }

            await tx.CommitAsync();
            return ids;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            await tx.RollbackAsync();
            throw new InvalidOperationException(
                "Nguon mo so hoac tham chieu trong batch da ton tai; khong duoc import lai.", ex);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<int> TaoHopDongTrongBatchAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        MoSoHopDongRequest request,
        DateTime ngayChot)
    {
        var hopDong = request.HopDong;
        var selectedIds = GetPhongDichVuIds(request);
        var services = await phongDichVuRepo.GetSelectedForPhongAsync(
            conn, tx, hopDong.PhongId, selectedIds, requireActive: true);

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

        foreach (var cuTru in GetCuTru(request).OrderBy(x => x.NgayBatDau).ThenBy(x => x.KhachThueId))
            await hopDongKhachRepo.InsertAsync(conn, tx, hopDongId, cuTru);

        await hopDongDichVuRepo.InsertManyAsync(
            conn, tx, hopDongId, services.Select(x => x.Id), hopDong.NgayBatDau);
        await giaoDichCocService.GhiNhanSoDuMoSoAsync(
            conn, tx, hopDongId, request.SoDuCocThucTe, ngayChot,
            request.DotMoSoId, request.SoDuCocNguonThamChieu);

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
                    NgayChot = ngayChot.Date,
                    meter.ChiSo,
                    NguonThamChieu = meter.NguonThamChieu.Trim()
                }, tx);
        }

        await PhongLifecycleService.DongBoTrangThaiTheoNgayAsync(
            conn, tx, hopDong.PhongId, DateTime.Today);
        return hopDongId;
    }

    private async Task ValidateBatchAgainstDatabaseAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        MoSoImportBatch batch,
        bool lockRooms)
    {
        var openingTableCount = await conn.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA=DATABASE()
              AND TABLE_NAME IN ('DotMoSo','HopDongMoSo','CongNoMoSo','ChiSoMoSo')
            """, transaction: tx);
        if (openingTableCount != 4)
            throw new InvalidOperationException("Migration 12 chua duoc ap day du; importer khong duoc ghi.");

        if (await conn.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM DotMoSo WHERE Sha256=@Sha256)",
                new { Sha256 = batch.DotMoSo.Sha256.Trim().ToUpperInvariant() }, tx))
            throw new InvalidOperationException("SHA-256 nguon nay da duoc import; replay bi chan.");

        foreach (var request in batch.HopDong.OrderBy(x => x.HopDong.PhongId))
        {
            var hopDong = request.HopDong;
            var roomSql = "SELECT * FROM Phong WHERE Id=@Id" + (lockRooms ? " FOR UPDATE" : "");
            var phong = await conn.QueryFirstOrDefaultAsync<Phong>(roomSql, new { Id = hopDong.PhongId }, tx)
                ?? throw new InvalidOperationException("Co hop dong tham chieu phong khong ton tai.");
            PhongLifecycleService.DamBaoKhongDangSua(phong);
            if (await hopDongRepo.CoChongKhoangAsync(
                    conn, tx, hopDong.PhongId, hopDong.NgayBatDau, hopDong.NgayKetThuc))
                throw new InvalidOperationException("Co phong da co hop dong giao khoang voi du lieu import.");

            var cuTru = GetCuTru(request);
            var tenantIds = cuTru.Select(x => x.KhachThueId).Distinct().ToArray();
            var tenantCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM KhachThue WHERE Id IN @Ids", new { Ids = tenantIds }, tx);
            if (tenantCount != tenantIds.Length)
                throw new InvalidOperationException("Co lich su cu tru tham chieu khach khong ton tai.");

            var selectedIds = GetPhongDichVuIds(request);
            var services = await phongDichVuRepo.GetSelectedForPhongAsync(
                conn, tx, hopDong.PhongId, selectedIds, requireActive: true);
            if (services.Count != selectedIds.Count)
                throw new InvalidOperationException("Co dich vu khong thuoc phong hoac da ngung ap dung.");
            var required = await phongDichVuRepo.GetRequiredIdsForPhongAsync(conn, tx, hopDong.PhongId);
            if (required.Except(selectedIds).Any())
                throw new InvalidOperationException("Thieu dich vu bat buoc cua phong.");
            var meterServiceIds = services.Where(x => x.DichVu?.LoaiTinhPhi == "TheoChiSo")
                .Select(x => x.DichVuId).ToHashSet();
            if (!meterServiceIds.SetEquals(request.ChiSo.Select(x => x.DichVuId)))
                throw new InvalidOperationException("Dich vu theo chi so phai co dung mot moc mo so.");
        }
    }

    private static void ValidateBatchShape(MoSoImportBatch batch)
    {
        if (batch == null) throw new InvalidOperationException("Thieu batch mo so.");
        ValidateDotMoSo(batch.DotMoSo);
        if (batch.HopDong.Count == 0)
            throw new InvalidOperationException("Batch mo so phai co it nhat mot hop dong.");
        if (batch.HopDong.Select(x => x.NguonThamChieu.Trim()).Distinct(StringComparer.Ordinal).Count()
            != batch.HopDong.Count)
            throw new InvalidOperationException("Nguon tham chieu hop dong bi trung trong batch.");
        if (batch.HopDong.GroupBy(x => x.HopDong.PhongId).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Mot batch khong duoc co nhieu hop dong dang van hanh cho cung phong.");

        var allRefs = new List<string>();
        foreach (var request in batch.HopDong)
        {
            ValidateImportRequest(request, batch.DotMoSo.NgayChot);
            allRefs.Add(request.NguonThamChieu.Trim());
            allRefs.Add(request.SoDuCocNguonThamChieu.Trim());
            allRefs.AddRange(request.DichVu.Select(x => x.NguonThamChieu.Trim()));
            allRefs.AddRange(request.CuTru.Select(x => x.NguonThamChieu.Trim()));
            allRefs.AddRange(request.CongNo.Select(x => x.NguonThamChieu.Trim()));
            allRefs.AddRange(request.ChiSo.Select(x => x.NguonThamChieu.Trim()));
        }
        if (allRefs.Any(string.IsNullOrWhiteSpace)
            || allRefs.Distinct(StringComparer.Ordinal).Count() != allRefs.Count)
            throw new InvalidOperationException("Moi chung tu/nguon tham chieu phai co ma rieng, khong rong va khong trung trong batch.");
    }

    private static void ValidateImportRequest(MoSoHopDongRequest request, DateTime ngayChot)
    {
        var hopDong = request.HopDong ?? throw new InvalidOperationException("Thieu hop dong mo so.");
        if (string.IsNullOrWhiteSpace(request.NguonThamChieu)
            || string.IsNullOrWhiteSpace(request.SoDuCocNguonThamChieu))
            throw new InvalidOperationException("Hop dong va so du coc phai co chung tu nguon rieng.");
        if (!BusinessDataLimits.IsValidBusinessDate(hopDong.NgayBatDau)
            || (hopDong.NgayKetThuc.HasValue && !BusinessDataLimits.IsValidBusinessDate(hopDong.NgayKetThuc.Value))
            || hopDong.NgayBatDau.Date > ngayChot.Date
            || (hopDong.NgayKetThuc.HasValue && hopDong.NgayKetThuc.Value.Date < ngayChot.Date))
            throw new InvalidOperationException("Khoang hop dong mo so khong hop le tai ngay cutover.");
        if (hopDong.TienThueThoaThuan <= 0 || hopDong.TienCoc < 0 || request.SoDuCocThucTe < 0
            || hopDong.NgayThanhToanHangThang is < 1 or > 31)
            throw new InvalidOperationException("Tien thue/coc hoac ngay thanh toan khong hop le.");

        var cuTru = GetCuTru(request);
        if (cuTru.Count == 0)
            throw new InvalidOperationException("Hop dong mo so phai co lich su cu tru.");
        if (cuTru.Any(x => x.KhachThueId <= 0 || string.IsNullOrWhiteSpace(x.NguonThamChieu)
            || x.NgayBatDau.Date < hopDong.NgayBatDau.Date
            || (x.NgayKetThuc.HasValue && x.NgayKetThuc.Value.Date < x.NgayBatDau.Date)
            || (hopDong.NgayKetThuc.HasValue && (!x.NgayKetThuc.HasValue
                || x.NgayKetThuc.Value.Date > hopDong.NgayKetThuc.Value.Date))))
            throw new InvalidOperationException("Lich su cu tru thieu nguon hoac nam ngoai khoang hop dong.");
        var activeRepresentatives = cuTru.Count(x => x.LaDaiDien
            && x.NgayBatDau.Date <= ngayChot.Date
            && (!x.NgayKetThuc.HasValue || x.NgayKetThuc.Value.Date >= ngayChot.Date));
        if (activeRepresentatives != 1)
            throw new InvalidOperationException("Tai cutover phai co dung mot khach dai dien dang cu tru.");
        if (cuTru.GroupBy(x => new { x.KhachThueId, Ngay = x.NgayBatDau.Date }).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Lich su cu tru bi trung khach va ngay bat dau.");

        var serviceIds = GetPhongDichVuIds(request);
        if (serviceIds.Count == 0 || request.DichVu.Any(x => string.IsNullOrWhiteSpace(x.NguonThamChieu))
            || serviceIds.Count != request.DichVu.Count)
            throw new InvalidOperationException("Danh sach dich vu phai co ma PhongDichVu va chung tu rieng.");
        if (request.CongNo.Any(x => x.SoTien <= 0 || !BusinessDataLimits.IsValidPeriod(x.DenKyThang, x.DenKyNam)
            || string.IsNullOrWhiteSpace(x.MaChungTu) || string.IsNullOrWhiteSpace(x.NguonThamChieu)))
            throw new InvalidOperationException("Cong no mo so thieu so tien, ky hoac chung tu.");
        if (request.ChiSo.Any(x => x.DichVuId <= 0 || x.ChiSo < 0 || string.IsNullOrWhiteSpace(x.NguonThamChieu))
            || request.ChiSo.Select(x => x.DichVuId).Distinct().Count() != request.ChiSo.Count)
            throw new InvalidOperationException("Moc chi so mo so thieu, trung hoac khong hop le.");
    }

    private static List<CuTruMoSoInput> GetCuTru(MoSoHopDongRequest request)
        => request.CuTru.Count > 0
            ? request.CuTru
            : request.KhachThueIds.Distinct().Select(id => new CuTruMoSoInput
            {
                KhachThueId = id,
                NgayBatDau = request.HopDong.NgayBatDau,
                NgayKetThucDuKien = request.HopDong.NgayKetThuc,
                LaDaiDien = id == request.KhachDaiDienId,
                NguonThamChieu = $"LEGACY-CUTRU-{id}"
            }).ToList();

    private static HashSet<int> GetPhongDichVuIds(MoSoHopDongRequest request)
        => request.DichVu.Count > 0
            ? request.DichVu.Select(x => x.PhongDichVuId).ToHashSet()
            : request.PhongDichVuIds.ToHashSet();

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
