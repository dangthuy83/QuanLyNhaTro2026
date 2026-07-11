using Dapper;
using MySqlConnector;
using System.Data;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public class HinhThucDichVuService(
    IDbConnection db,
    DichVuRepository dichVuRepo,
    LichSuHinhThucDichVuRepository lichSuRepo)
{
    public async Task ApplyForPeriodAsync(ThayDoiHinhThucDichVuViewModel vm)
    {
        var dichVu = await dichVuRepo.GetByIdAsync(vm.DichVuId)
            ?? throw new InvalidOperationException("Không tìm thấy dịch vụ.");
        if (!await dichVuRepo.DaTungGanPhongAsync(vm.DichVuId))
            throw new InvalidOperationException("Dịch vụ chưa gắn phòng; hãy sửa trực tiếp trên danh mục dịch vụ.");
        ValidateValues(vm);
        var ky = new DateTime(vm.NamApDung, vm.ThangApDung, 1);
        var kyTruoc = ky.AddMonths(-1);
        var (loaiCu, cachCu) = await lichSuRepo.ResolveAsync(vm.DichVuId, kyTruoc.Month, kyTruoc.Year, dichVu);
        if (loaiCu == vm.LoaiTinhPhiMoi && cachCu == vm.CachTinhCoDinhMoi)
            throw new InvalidOperationException("Hình thức mới không khác hình thức đang áp dụng ở kỳ trước.");

        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var latestPeriod = await conn.ExecuteScalarAsync<DateTime?>("SELECT MAX(KyApDung) FROM LichSuHinhThucDichVu WHERE DichVuId=@DichVuId", new { vm.DichVuId }, tx);
            if (latestPeriod.HasValue && ky <= latestPeriod.Value)
                throw new InvalidOperationException($"Kỳ áp dụng phải sau kỳ thay đổi gần nhất {latestPeriod:MM/yyyy}.");

            var hasFutureInvoice = await conn.ExecuteScalarAsync<int>("""
                SELECT COUNT(*) FROM HoaDon hd
                JOIN HopDong h ON h.Id=hd.HopDongId
                JOIN PhongDichVu pdv ON pdv.PhongId=h.PhongId AND pdv.DichVuId=@DichVuId
                WHERE STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d') >= @Ky
                """, new { vm.DichVuId, Ky = ky }, tx);
            if (hasFutureInvoice > 0)
                throw new InvalidOperationException("Đã tồn tại hóa đơn từ kỳ áp dụng trở đi.");

            var missingPreviousInvoices = await conn.QueryAsync<string>("""
                SELECT CONCAT(p.TenPhong, ' (#', h.Id, ')')
                FROM HopDong h
                JOIN Phong p ON p.Id=h.PhongId
                JOIN HopDongDichVu hdv ON hdv.HopDongId=h.Id
                JOIN PhongDichVu pdv ON pdv.Id=hdv.PhongDichVuId AND pdv.DichVuId=@DichVuId
                LEFT JOIN HoaDon hd ON hd.HopDongId=h.Id AND hd.Thang=@Thang AND hd.Nam=@Nam
                WHERE hdv.KyBatDau<=@KyTruoc AND (hdv.KyKetThuc IS NULL OR @KyTruoc<hdv.KyKetThuc)
                  AND h.NgayBatDau < DATE_ADD(@KyTruoc, INTERVAL 1 MONTH)
                  AND (h.NgayKetThuc IS NULL OR h.NgayKetThuc>=@KyTruoc)
                  AND hd.Id IS NULL
                """, new { vm.DichVuId, Thang = kyTruoc.Month, Nam = kyTruoc.Year, KyTruoc = kyTruoc }, tx);
            var missing = missingPreviousInvoices.ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException("Kỳ trước chưa chốt hóa đơn cho: " + string.Join(", ", missing));

            if (loaiCu == DichVu.LoaiTheoChiSo && vm.LoaiTinhPhiMoi == DichVu.LoaiCoDinh)
            {
                var missingReadings = (await conn.QueryAsync<string>("""
                    SELECT CONCAT(p.TenPhong, ' (#', h.Id, ')')
                    FROM HopDong h JOIN Phong p ON p.Id=h.PhongId
                    JOIN HopDongDichVu hdv ON hdv.HopDongId=h.Id
                    JOIN PhongDichVu pdv ON pdv.Id=hdv.PhongDichVuId AND pdv.DichVuId=@DichVuId
                    JOIN HoaDon hd ON hd.HopDongId=h.Id AND hd.Thang=@Thang AND hd.Nam=@Nam
                    LEFT JOIN ChiTietHoaDon ct ON ct.HoaDonId=hd.Id AND ct.DichVuId=@DichVuId AND ct.ChiSoDienNuocId IS NOT NULL
                    WHERE hdv.KyBatDau<=@KyTruoc AND (hdv.KyKetThuc IS NULL OR @KyTruoc<hdv.KyKetThuc) AND ct.Id IS NULL
                    """, new { vm.DichVuId, Thang = kyTruoc.Month, Nam = kyTruoc.Year, KyTruoc = kyTruoc }, tx)).ToList();
                if (missingReadings.Count > 0)
                    throw new InvalidOperationException("Kỳ cuối theo chỉ số chưa có chỉ số đã chốt cho: " + string.Join(", ", missingReadings));
            }

            var relatedRooms = (await conn.QueryAsync<(int PhongId, string TenPhong)>("""
                SELECT DISTINCT p.Id AS PhongId, p.TenPhong FROM PhongDichVu pdv JOIN Phong p ON p.Id=pdv.PhongId
                WHERE pdv.DichVuId=@DichVuId ORDER BY p.TenPhong
                """, new { vm.DichVuId }, tx)).ToList();
            if (loaiCu == DichVu.LoaiCoDinh && vm.LoaiTinhPhiMoi == DichVu.LoaiTheoChiSo)
            {
                var supplied = vm.PhongLienQuan.Where(x => x.ChiSoDau.HasValue && x.ChiSoDau >= 0).ToDictionary(x => x.PhongId, x => x.ChiSoDau!.Value);
                var absent = relatedRooms.Where(x => !supplied.ContainsKey(x.PhongId)).Select(x => x.TenPhong).ToList();
                if (absent.Count > 0) throw new InvalidOperationException("Phải khai báo chỉ số đầu cho: " + string.Join(", ", absent));
            }

            var historyId = await conn.ExecuteScalarAsync<int>("""
                INSERT INTO LichSuHinhThucDichVu
                    (DichVuId,LoaiTinhPhiCu,CachTinhCoDinhCu,LoaiTinhPhiMoi,CachTinhCoDinhMoi,KyApDung,LyDo)
                VALUES (@DichVuId,@LoaiCu,@CachCu,@LoaiMoi,@CachMoi,@Ky,@LyDo);
                SELECT LAST_INSERT_ID();
                """, new { vm.DichVuId, LoaiCu = loaiCu, CachCu = cachCu, LoaiMoi = vm.LoaiTinhPhiMoi, CachMoi = vm.CachTinhCoDinhMoi, Ky = ky, LyDo = vm.LyDo.Trim() }, tx);

            if (loaiCu == DichVu.LoaiCoDinh && vm.LoaiTinhPhiMoi == DichVu.LoaiTheoChiSo)
                await conn.ExecuteAsync("INSERT INTO ChiSoDauChuyenDoiDichVu (LichSuHinhThucDichVuId,PhongId,ChiSoDau) VALUES (@HistoryId,@PhongId,@ChiSoDau)",
                    vm.PhongLienQuan.Select(x => new { HistoryId = historyId, x.PhongId, ChiSoDau = x.ChiSoDau!.Value }), tx);

            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    private static void ValidateValues(ThayDoiHinhThucDichVuViewModel vm)
    {
        if (vm.ThangApDung is < 1 or > 12 || vm.NamApDung < 2000) throw new ArgumentException("Kỳ áp dụng không hợp lệ.");
        if (string.IsNullOrWhiteSpace(vm.LyDo)) throw new ArgumentException("Lý do là bắt buộc.");
        if (vm.LoaiTinhPhiMoi is not (DichVu.LoaiCoDinh or DichVu.LoaiTheoChiSo)) throw new ArgumentException("Loại tính phí không hợp lệ.");
        if (vm.CachTinhCoDinhMoi is not (DichVu.CachTinhTheoPhong or DichVu.CachTinhTheoNguoi)) throw new ArgumentException("Cách tính cố định không hợp lệ.");
        if (vm.LoaiTinhPhiMoi == DichVu.LoaiTheoChiSo) vm.CachTinhCoDinhMoi = DichVu.CachTinhTheoPhong;
    }
}
