using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Services;

public class PhongLifecycleService
{
    public async Task<Phong> KhoaPhongAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int phongId)
        => await conn.QueryFirstOrDefaultAsync<Phong>(
            "SELECT * FROM Phong WHERE Id = @PhongId FOR UPDATE",
            new { PhongId = phongId },
            tx)
           ?? throw new InvalidOperationException("Không tìm thấy phòng.");

    public static async Task<bool> CoHopDongHieuLucTheoNgayAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int phongId,
        DateTime ngay,
        int? excludeId = null)
        => await conn.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM HopDong
                WHERE PhongId = @PhongId
                  AND TrangThai IN ('ChoHieuLuc', 'DangHieuLuc')
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
                  AND NgayBatDau <= @Ngay
                  AND (NgayKetThuc IS NULL OR NgayKetThuc >= @Ngay))
            """,
            new { PhongId = phongId, Ngay = ngay.Date, ExcludeId = excludeId },
            tx);

    public static async Task<bool> CoHopDongTuongLaiAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int phongId,
        DateTime ngay,
        int? excludeId = null)
        => await conn.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM HopDong
                WHERE PhongId = @PhongId
                  AND TrangThai IN ('ChoHieuLuc', 'DangHieuLuc')
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
                  AND NgayBatDau > @Ngay)
            """,
            new { PhongId = phongId, Ngay = ngay.Date, ExcludeId = excludeId },
            tx);

    public static void DamBaoKhongDangSua(Phong phong)
    {
        if (phong.TrangThai == "DangSuaChua")
            throw new InvalidOperationException(
                "Phòng đang sửa chữa, không thể tạo, chuyển hoặc kích hoạt hợp đồng.");
    }

    public static async Task DongBoTrangThaiTheoNgayAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int phongId,
        DateTime ngay,
        bool giuTrangThaiDangSua = true)
    {
        var phong = await conn.QueryFirstOrDefaultAsync<Phong>(
            "SELECT * FROM Phong WHERE Id = @PhongId FOR UPDATE",
            new { PhongId = phongId },
            tx) ?? throw new InvalidOperationException("Không tìm thấy phòng.");

        var coHopDong = await CoHopDongHieuLucTheoNgayAsync(conn, tx, phongId, ngay);
        var trangThai = coHopDong
            ? "DangThue"
            : giuTrangThaiDangSua && phong.TrangThai == "DangSuaChua"
                ? "DangSuaChua"
                : "Trong";

        await conn.ExecuteAsync(
            "UPDATE Phong SET TrangThai = @TrangThai WHERE Id = @PhongId",
            new { PhongId = phongId, TrangThai = trangThai },
            tx);
    }
}
