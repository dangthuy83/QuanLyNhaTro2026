using System.Data;
using Dapper;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Services;

public static class HoaDonDeletionPolicy
{
    public static async Task<HoaDonDeletionAssessment> EvaluateAsync(
        IDbConnection conn,
        IDbTransaction? tx,
        HoaDon hoaDon,
        bool lockRelatedRows)
    {
        string lockClause = lockRelatedRows ? " FOR UPDATE" : string.Empty;
        var coThanhToan = (await conn.QueryAsync<int>(
            $"SELECT Id FROM ThanhToan WHERE HoaDonId = @HoaDonId{lockClause}",
            new { HoaDonId = hoaDon.Id },
            transaction: tx)).Any();
        var coGiaoDichCoc = (await conn.QueryAsync<int>(
            $"SELECT Id FROM GiaoDichCoc WHERE HoaDonId = @HoaDonId{lockClause}",
            new { HoaDonId = hoaDon.Id },
            transaction: tx)).Any();

        if (hoaDon.SoTienDaThu > 0 || coThanhToan)
            return new(false, "hóa đơn đã có thanh toán hoặc bút toán settlement");
        if (hoaDon.TienNoKyTruoc > 0)
            return new(false, "hóa đơn đang mang nợ kỳ trước đã kết chuyển");
        if (coGiaoDichCoc)
            return new(false, "hóa đơn đã có giao dịch cọc liên quan");

        return new(true, null);
    }
}

public sealed record HoaDonDeletionAssessment(bool CanDelete, string? BlockReason);
