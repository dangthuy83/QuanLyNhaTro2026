using Dapper;
using MySqlConnector;

namespace QuanLyNhaTro.Services;

public class CongNoSettlementService
{
    public async Task<decimal> ThanhToanNoAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongId,
        decimal soTien,
        DateTime ngayThanhToan,
        string hinhThuc,
        string ghiChu,
        IReadOnlyCollection<int>? excludeHoaDonIds = null)
    {
        if (soTien <= 0) return 0;

        const string sql = """
            SELECT Id, TongCong, SoTienDaThu
            FROM HoaDon
            WHERE HopDongId = @HopDongId
              AND TongCong > SoTienDaThu
            ORDER BY Nam, Thang, Id
            FOR UPDATE
            """;

        var hoaDons = (await conn.QueryAsync<HoaDonNoTam>(
            sql,
            new { HopDongId = hopDongId },
            tx)).ToList();

        decimal conLaiCanThu = soTien;
        decimal daApDung = 0;
        var exclude = excludeHoaDonIds?.ToHashSet() ?? [];

        foreach (var hoaDon in hoaDons)
        {
            if (exclude.Contains(hoaDon.Id)) continue;
            if (conLaiCanThu <= 0) break;

            decimal soTienApDung = await ThanhToanHoaDonNoAsync(
                conn,
                tx,
                hoaDon,
                conLaiCanThu,
                ngayThanhToan,
                hinhThuc,
                ghiChu);

            conLaiCanThu -= soTienApDung;
            daApDung += soTienApDung;
        }

        return daApDung;
    }

    public async Task<decimal> ThanhToanHoaDonAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hoaDonId,
        decimal soTien,
        DateTime ngayThanhToan,
        string hinhThuc,
        string ghiChu)
    {
        if (soTien <= 0) return 0;

        var hoaDon = await conn.QueryFirstOrDefaultAsync<HoaDonNoTam>(
            """
            SELECT Id, TongCong, SoTienDaThu
            FROM HoaDon
            WHERE Id = @HoaDonId
            LIMIT 1
            FOR UPDATE
            """,
            new { HoaDonId = hoaDonId },
            tx);

        if (hoaDon == null) return 0;

        return await ThanhToanHoaDonNoAsync(
            conn,
            tx,
            hoaDon,
            soTien,
            ngayThanhToan,
            hinhThuc,
            ghiChu);
    }

    private static async Task<decimal> ThanhToanHoaDonNoAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        HoaDonNoTam hoaDon,
        decimal soTien,
        DateTime ngayThanhToan,
        string hinhThuc,
        string ghiChu)
    {
        decimal noHoaDon = hoaDon.TongCong - hoaDon.SoTienDaThu;
        if (noHoaDon <= 0) return 0;

        decimal soTienApDung = Math.Min(noHoaDon, soTien);
        decimal soTienDaThuMoi = hoaDon.SoTienDaThu + soTienApDung;
        string trangThai = TinhTrangThai(soTienDaThuMoi, hoaDon.TongCong);

        await conn.ExecuteAsync(
            """
            INSERT INTO ThanhToan (HoaDonId, SoTien, NgayThu, HinhThuc, GhiChu, NgayTao)
            VALUES (@HoaDonId, @SoTien, @NgayThu, @HinhThuc, @GhiChu, NOW())
            """,
            new
            {
                HoaDonId = hoaDon.Id,
                SoTien = soTienApDung,
                NgayThu = ngayThanhToan,
                HinhThuc = hinhThuc,
                GhiChu = ghiChu
            },
            tx);

        await conn.ExecuteAsync(
            """
            UPDATE HoaDon
            SET SoTienDaThu = @SoTienDaThu,
                TrangThaiThanhToan = @TrangThai,
                NgayThuThucTe = CASE WHEN @TrangThai = 'DaThu' THEN @NgayThu ELSE NgayThuThucTe END
            WHERE Id = @Id
            """,
            new
            {
                Id = hoaDon.Id,
                SoTienDaThu = soTienDaThuMoi,
                TrangThai = trangThai,
                NgayThu = ngayThanhToan
            },
            tx);

        return soTienApDung;
    }

    private static string TinhTrangThai(decimal soTienDaThu, decimal tongCong)
    {
        if (soTienDaThu <= 0) return "ChuaThu";
        return soTienDaThu >= tongCong ? "DaThu" : "ThuMotPhan";
    }

    private sealed record HoaDonNoTam(int Id, decimal TongCong, decimal SoTienDaThu);
}
