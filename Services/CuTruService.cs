using System.Data;
using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public class CuTruService(IDbConnection db, HopDongKhachThueRepository repository)
{
    public async Task ThemGiaiDoanAsync(
        int hopDongId,
        int khachThueId,
        DateTime ngayBatDau,
        DateTime? ngayKetThucDuKien,
        bool laDaiDien)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var hopDong = await LockAndValidateContractAsync(conn, tx, hopDongId);
            await ValidatePeriodAsync(
                conn, tx, hopDong, khachThueId, ngayBatDau, ngayKetThucDuKien, null, laDaiDien);
            await repository.InsertAsync(
                conn, tx, hopDongId, khachThueId, ngayBatDau, ngayKetThucDuKien, laDaiDien);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task KetThucGiaiDoanAsync(int id, DateTime ngayKetThuc)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var row = await conn.QueryFirstOrDefaultAsync<HopDongKhachThue>(
                "SELECT * FROM HopDongKhachThue WHERE Id=@Id FOR UPDATE", new { Id = id }, tx)
                ?? throw new InvalidOperationException("Không tìm thấy giai đoạn cư trú.");
            var hopDong = await LockAndValidateContractAsync(conn, tx, row.HopDongId);
            if (row.NgayKetThuc.HasValue)
                throw new InvalidOperationException("Giai đoạn cư trú đã kết thúc.");
            if (ngayKetThuc.Date < row.NgayBatDau.Date)
                throw new InvalidOperationException("Ngày kết thúc không được trước ngày bắt đầu thực tế.");
            if (hopDong.NgayKetThuc.HasValue && ngayKetThuc.Date > hopDong.NgayKetThuc.Value.Date)
                throw new InvalidOperationException("Ngày kết thúc cư trú không được sau ngày kết thúc hợp đồng.");
            if (row.LaDaiDien && (!hopDong.NgayKetThuc.HasValue || ngayKetThuc.Date < hopDong.NgayKetThuc.Value.Date))
                throw new InvalidOperationException(
                    "Không thể kết thúc giai đoạn của người đại diện khi hợp đồng còn tiếp tục. Hãy bố trí đại diện mới trước.");

            await conn.ExecuteAsync(
                "UPDATE HopDongKhachThue SET NgayKetThuc=@Ngay WHERE Id=@Id",
                new { Ngay = ngayKetThuc.Date, Id = id }, tx);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public static async Task DongTatCaDangMoAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int hopDongId,
        DateTime ngayKetThuc)
    {
        var invalid = await conn.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*) FROM HopDongKhachThue
            WHERE HopDongId=@HopDongId AND NgayKetThuc IS NULL AND NgayBatDau>@NgayKetThuc
            """, new { HopDongId = hopDongId, NgayKetThuc = ngayKetThuc.Date }, tx);
        if (invalid > 0)
            throw new InvalidOperationException("Có giai đoạn cư trú bắt đầu sau ngày đóng hợp đồng.");

        await conn.ExecuteAsync(
            """
            UPDATE HopDongKhachThue
            SET NgayKetThuc=@NgayKetThuc
            WHERE HopDongId=@HopDongId AND NgayKetThuc IS NULL
            """, new { HopDongId = hopDongId, NgayKetThuc = ngayKetThuc.Date }, tx);
    }

    public static async Task ChuyenSangHopDongMoiAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int hopDongCuId,
        int hopDongMoiId,
        DateTime ngayChuyenDi,
        DateTime ngayBatDauMoi)
    {
        await DongTatCaDangMoAsync(conn, tx, hopDongCuId, ngayChuyenDi);
        await conn.ExecuteAsync(
            """
            INSERT INTO HopDongKhachThue
                (HopDongId, KhachThueId, NgayBatDau, NgayKetThucDuKien, NgayKetThuc, LaDaiDien)
            SELECT @HopDongMoiId,
                   KhachThueId,
                   @NgayBatDauMoi,
                   CASE WHEN NgayKetThucDuKien>=@NgayBatDauMoi THEN NgayKetThucDuKien ELSE NULL END,
                   NULL,
                   LaDaiDien
            FROM HopDongKhachThue
            WHERE HopDongId=@HopDongCuId
              AND NgayBatDau<=@NgayChuyenDi
              AND NgayKetThuc>=@NgayChuyenDi
            """,
            new
            {
                HopDongCuId = hopDongCuId,
                HopDongMoiId = hopDongMoiId,
                NgayChuyenDi = ngayChuyenDi.Date,
                NgayBatDauMoi = ngayBatDauMoi.Date
            }, tx);
    }

    private static async Task<HopDong> LockAndValidateContractAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int hopDongId)
    {
        var hopDong = await conn.QueryFirstOrDefaultAsync<HopDong>(
            "SELECT * FROM HopDong WHERE Id=@Id FOR UPDATE", new { Id = hopDongId }, tx)
            ?? throw new InvalidOperationException("Không tìm thấy hợp đồng.");
        if (hopDong.TrangThai is not ("ChoHieuLuc" or "DangHieuLuc"))
            throw new InvalidOperationException("Chỉ được cập nhật cư trú cho hợp đồng chờ/đang hiệu lực.");
        return hopDong;
    }

    private static async Task ValidatePeriodAsync(
        IDbConnection conn,
        IDbTransaction tx,
        HopDong hopDong,
        int khachThueId,
        DateTime ngayBatDau,
        DateTime? ngayKetThucDuKien,
        DateTime? ngayKetThuc,
        bool laDaiDien)
    {
        ngayBatDau = ngayBatDau.Date;
        ngayKetThucDuKien = ngayKetThucDuKien?.Date;
        ngayKetThuc = ngayKetThuc?.Date;
        if (ngayBatDau < hopDong.NgayBatDau.Date)
            throw new InvalidOperationException("Ngày bắt đầu cư trú không được trước ngày bắt đầu hợp đồng.");
        if (hopDong.NgayKetThuc.HasValue && ngayBatDau > hopDong.NgayKetThuc.Value.Date)
            throw new InvalidOperationException("Ngày bắt đầu cư trú không được sau ngày kết thúc hợp đồng.");
        if (ngayKetThucDuKien.HasValue && ngayKetThucDuKien.Value < ngayBatDau)
            throw new InvalidOperationException("Ngày dự kiến rời không được trước ngày bắt đầu cư trú.");
        if (await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM KhachThue WHERE Id=@Id", new { Id = khachThueId }, tx) == 0)
            throw new InvalidOperationException("Không tìm thấy hồ sơ khách thuê.");

        var periodEnd = ngayKetThuc ?? new DateTime(9999, 12, 31);
        var overlapping = (await conn.QueryAsync<int>(
            """
            SELECT Id FROM HopDongKhachThue
            WHERE HopDongId=@HopDongId AND KhachThueId=@KhachThueId
              AND NgayBatDau<=@PeriodEnd
              AND COALESCE(NgayKetThuc, '9999-12-31')>=@NgayBatDau
            FOR UPDATE
            """,
            new { HopDongId = hopDong.Id, KhachThueId = khachThueId, PeriodEnd = periodEnd, NgayBatDau = ngayBatDau }, tx)).Any();
        if (overlapping)
            throw new InvalidOperationException("Khách đã có giai đoạn cư trú chồng nhau trong hợp đồng này.");

        if (laDaiDien)
        {
            var representativeOverlap = (await conn.QueryAsync<int>(
                """
                SELECT Id FROM HopDongKhachThue
                WHERE HopDongId=@HopDongId AND LaDaiDien=1
                  AND NgayBatDau<=@PeriodEnd
                  AND COALESCE(NgayKetThuc, '9999-12-31')>=@NgayBatDau
                FOR UPDATE
                """,
                new { HopDongId = hopDong.Id, PeriodEnd = periodEnd, NgayBatDau = ngayBatDau }, tx)).Any();
            if (representativeOverlap)
                throw new InvalidOperationException("Hợp đồng đã có người đại diện hiệu lực trong khoảng này.");
        }
    }
}
