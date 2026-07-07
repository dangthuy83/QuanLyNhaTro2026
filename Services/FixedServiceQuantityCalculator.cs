using System.Data;
using Dapper;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Services;

public static class FixedServiceQuantityCalculator
{
    public static async Task<decimal> ResolveQuantityAsync(
        IDbConnection conn,
        IDbTransaction? tx,
        int hopDongId,
        DichVu dichVu)
    {
        if (dichVu.LoaiTinhPhi != DichVu.LoaiCoDinh)
            throw new InvalidOperationException("Chi dung helper nay cho dich vu co dinh.");

        if (dichVu.CachTinhCoDinh != DichVu.CachTinhTheoNguoi)
            return 1;

        var soKhach = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM HopDongKhachThue WHERE HopDongId = @HopDongId",
            new { HopDongId = hopDongId },
            transaction: tx);

        if (soKhach <= 0)
            throw new InvalidOperationException($"Dich vu {dichVu.TenDichVu} tinh theo nguoi nhung hop dong #{hopDongId} chua gan khach thue.");

        return soKhach;
    }
}
