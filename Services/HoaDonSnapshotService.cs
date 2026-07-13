using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Services;

public class HoaDonSnapshotService
{
    public async Task<int> InsertHoaDonAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        HoaDon hoaDon)
    {
        await DienNhanDienAsync(conn, tx, hoaDon);
        const string sql = """
            INSERT INTO HoaDon
                (HopDongId,Thang,Nam,NgayLap,TienPhong,TongTienDichVu,TongTienPhatSinh,
                 TienNoKyTruoc,TongCong,SoTienDaThu,TrangThaiThanhToan,SoNgayO,
                 SoNgayTrongThang,HoaDonGhepId,GhiChu,NhaIdSnapshot,TenNhaSnapshot,
                 PhongIdSnapshot,TenPhongSnapshot,KhachDaiDienIdSnapshot,
                 TenKhachDaiDienSnapshot,CccdKhachDaiDienSnapshot)
            VALUES
                (@HopDongId,@Thang,@Nam,@NgayLap,@TienPhong,@TongTienDichVu,@TongTienPhatSinh,
                 @TienNoKyTruoc,@TongCong,@SoTienDaThu,@TrangThaiThanhToan,@SoNgayO,
                 @SoNgayTrongThang,@HoaDonGhepId,@GhiChu,@NhaIdSnapshot,@TenNhaSnapshot,
                 @PhongIdSnapshot,@TenPhongSnapshot,@KhachDaiDienIdSnapshot,
                 @TenKhachDaiDienSnapshot,@CccdKhachDaiDienSnapshot);
            SELECT LAST_INSERT_ID();
            """;
        return await conn.ExecuteScalarAsync<int>(sql, hoaDon, tx);
    }

    public async Task InsertChiTietAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hoaDonId,
        IEnumerable<ChiTietHoaDon> chiTiet)
    {
        const string serviceSql = """
            SELECT TenDichVu,DonViTinh FROM DichVu WHERE Id=@Id FOR UPDATE
            """;
        const string insertSql = """
            INSERT INTO ChiTietHoaDon
                (HoaDonId,DichVuId,ChiSoDienNuocId,SoLuong,DonGia,ThanhTien,
                 TenDichVuSnapshot,DonViTinhSnapshot)
            VALUES
                (@HoaDonId,@DichVuId,@ChiSoDienNuocId,@SoLuong,@DonGia,@ThanhTien,
                 @TenDichVuSnapshot,@DonViTinhSnapshot)
            """;

        foreach (var item in chiTiet)
        {
            var dichVu = await conn.QueryFirstOrDefaultAsync<ServiceIdentity>(
                serviceSql, new { Id = item.DichVuId }, tx)
                ?? throw new InvalidOperationException($"Không tìm thấy dịch vụ #{item.DichVuId} để chốt snapshot hóa đơn.");
            if (string.IsNullOrWhiteSpace(dichVu.TenDichVu) || string.IsNullOrWhiteSpace(dichVu.DonViTinh))
                throw new InvalidOperationException(
                    $"Dịch vụ #{item.DichVuId} phải có tên và đơn vị tính trước khi chốt hóa đơn.");
            item.HoaDonId = hoaDonId;
            item.TenDichVuSnapshot = dichVu.TenDichVu;
            item.DonViTinhSnapshot = dichVu.DonViTinh;
            await conn.ExecuteAsync(insertSql, item, tx);
        }
    }

    private static async Task DienNhanDienAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        HoaDon hoaDon)
    {
        var scope = await conn.QueryFirstOrDefaultAsync<InvoiceScope>(
            """
            SELECT n.Id NhaId,n.TenNha,p.Id PhongId,p.TenPhong
            FROM HopDong h
            INNER JOIN Phong p ON p.Id=h.PhongId
            INNER JOIN Nha n ON n.Id=p.NhaId
            WHERE h.Id=@HopDongId
            FOR UPDATE
            """,
            new { hoaDon.HopDongId }, tx)
            ?? throw new InvalidOperationException("Không tìm thấy hợp đồng/phòng/nhà để chốt snapshot hóa đơn.");

        var kyBatDau = new DateTime(hoaDon.Nam, hoaDon.Thang, 1);
        var kyKetThuc = kyBatDau.AddMonths(1).AddDays(-1);
        var daiDienTrongKy = (await conn.QueryAsync<RepresentativeIdentity>(
            """
            SELECT kt.Id KhachThueId,kt.HoTen,kt.CCCD,x.NgayBatDau,x.NgayKetThuc
            FROM HopDongKhachThue x
            INNER JOIN KhachThue kt ON kt.Id=x.KhachThueId
            WHERE x.HopDongId=@HopDongId AND x.LaDaiDien=1
              AND x.NgayBatDau<=@KyKetThuc
              AND (x.NgayKetThuc IS NULL OR x.NgayKetThuc>=@KyBatDau)
            ORDER BY x.NgayBatDau DESC,x.Id DESC
            FOR UPDATE
            """,
            new { hoaDon.HopDongId, KyBatDau = kyBatDau, KyKetThuc = kyKetThuc }, tx)).ToList();
        if (daiDienTrongKy.Count == 0)
            throw new InvalidOperationException(
                $"Hợp đồng #{hoaDon.HopDongId} không có khách đại diện trong kỳ {hoaDon.Thang}/{hoaDon.Nam} để chốt hóa đơn.");
        for (var i = 0; i < daiDienTrongKy.Count; i++)
        for (var j = i + 1; j < daiDienTrongKy.Count; j++)
        {
            var a = daiDienTrongKy[i];
            var b = daiDienTrongKy[j];
            if (a.NgayBatDau <= (b.NgayKetThuc ?? kyKetThuc)
                && b.NgayBatDau <= (a.NgayKetThuc ?? kyKetThuc))
                throw new InvalidOperationException(
                    $"Hợp đồng #{hoaDon.HopDongId} có nhiều khách đại diện đồng thời trong kỳ {hoaDon.Thang}/{hoaDon.Nam}.");
        }
        var daiDien = daiDienTrongKy[0];

        hoaDon.NhaIdSnapshot = scope.NhaId;
        hoaDon.TenNhaSnapshot = scope.TenNha;
        hoaDon.PhongIdSnapshot = scope.PhongId;
        hoaDon.TenPhongSnapshot = scope.TenPhong;
        hoaDon.KhachDaiDienIdSnapshot = daiDien.KhachThueId;
        hoaDon.TenKhachDaiDienSnapshot = daiDien.HoTen;
        hoaDon.CccdKhachDaiDienSnapshot = daiDien.CCCD;
    }

    private sealed class InvoiceScope
    {
        public int NhaId { get; init; }
        public string TenNha { get; init; } = string.Empty;
        public int PhongId { get; init; }
        public string TenPhong { get; init; } = string.Empty;
    }

    private sealed class RepresentativeIdentity
    {
        public int KhachThueId { get; init; }
        public string HoTen { get; init; } = string.Empty;
        public string? CCCD { get; init; }
        public DateTime NgayBatDau { get; init; }
        public DateTime? NgayKetThuc { get; init; }
    }

    private sealed class ServiceIdentity
    {
        public string TenDichVu { get; init; } = string.Empty;
        public string DonViTinh { get; init; } = string.Empty;
    }
}
