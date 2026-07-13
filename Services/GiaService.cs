using Dapper;
using MySqlConnector;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Services;

public class GiaService(IDbConnection db)
{
    public async Task LuuThayDoiAsync(ThayDoiGiaViewModel vm)
    {
        Validate(vm.LoaiDoiTuong, vm.DoiTuongId, vm.ThangApDung, vm.NamApDung);
        if (vm.GiaMoi <= 0)
            throw new InvalidOperationException("Giá mới phải lớn hơn 0.");

        var ky = new DateTime(vm.NamApDung, vm.ThangApDung, 1);
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var giaGoc = await LockAndGetBasePriceAsync(conn, tx, vm.LoaiDoiTuong, vm.DoiTuongId);
            await LockHistoryAsync(conn, tx, vm.LoaiDoiTuong, vm.DoiTuongId);
            await EnsureNoInvoiceFromPeriodAsync(conn, tx, vm.LoaiDoiTuong, vm.DoiTuongId, ky);

            var duplicate = await conn.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*) FROM LichSuThayDoiGia
                WHERE LoaiDoiTuong=@LoaiDoiTuong AND DoiTuongId=@DoiTuongId
                  AND ThangApDung=@Thang AND NamApDung=@Nam
                """,
                new
                {
                    vm.LoaiDoiTuong,
                    vm.DoiTuongId,
                    Thang = vm.ThangApDung,
                    Nam = vm.NamApDung
                }, tx);
            if (duplicate > 0)
                throw new InvalidOperationException("Đã có một thay đổi giá cho đối tượng trong kỳ này.");

            var kyTruoc = ky.AddMonths(-1);
            var giaTruocKy = await ResolvePriceAsync(
                conn, tx, vm.LoaiDoiTuong, vm.DoiTuongId, kyTruoc, giaGoc);
            if (giaTruocKy == vm.GiaMoi)
                throw new InvalidOperationException("Giá mới không thay đổi so với giá đang áp dụng trước kỳ đã chọn.");

            await conn.ExecuteAsync(
                """
                INSERT INTO LichSuThayDoiGia
                    (LoaiDoiTuong,DoiTuongId,GiaCu,GiaMoi,ThangApDung,NamApDung,LyDo,NgayTao)
                VALUES
                    (@LoaiDoiTuong,@DoiTuongId,@GiaCu,@GiaMoi,@ThangApDung,@NamApDung,@LyDo,NOW())
                """,
                new
                {
                    vm.LoaiDoiTuong,
                    vm.DoiTuongId,
                    GiaCu = giaTruocKy,
                    vm.GiaMoi,
                    vm.ThangApDung,
                    vm.NamApDung,
                    LyDo = vm.GhiChu
                }, tx);

            await UpdateNextOldPriceAsync(
                conn, tx, vm.LoaiDoiTuong, vm.DoiTuongId, ky, vm.GiaMoi);
            await SyncCurrentServicePriceAsync(
                conn, tx, vm.LoaiDoiTuong, vm.DoiTuongId, giaGoc);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<LichSuThayDoiGia> XoaThayDoiAsync(int id)
    {
        if (id <= 0) throw new InvalidOperationException("Bản ghi thay đổi giá không hợp lệ.");

        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var scope = await conn.QueryFirstOrDefaultAsync<LichSuThayDoiGia>(
                "SELECT * FROM LichSuThayDoiGia WHERE Id=@Id",
                new { Id = id }, tx)
                ?? throw new InvalidOperationException("Không tìm thấy bản ghi thay đổi giá.");

            Validate(scope.LoaiDoiTuong, scope.DoiTuongId, scope.ThangApDung, scope.NamApDung);
            await LockAndGetBasePriceAsync(conn, tx, scope.LoaiDoiTuong, scope.DoiTuongId);
            await LockHistoryAsync(conn, tx, scope.LoaiDoiTuong, scope.DoiTuongId);

            var item = await conn.QueryFirstOrDefaultAsync<LichSuThayDoiGia>(
                "SELECT * FROM LichSuThayDoiGia WHERE Id=@Id FOR UPDATE",
                new { Id = id }, tx)
                ?? throw new InvalidOperationException("Bản ghi thay đổi giá đã được xử lý bởi thao tác khác.");
            var ky = new DateTime(item.NamApDung, item.ThangApDung, 1);
            await EnsureNoInvoiceFromPeriodAsync(
                conn, tx, item.LoaiDoiTuong, item.DoiTuongId, ky);

            await UpdateNextOldPriceAsync(
                conn, tx, item.LoaiDoiTuong, item.DoiTuongId, ky, item.GiaCu);
            await conn.ExecuteAsync(
                "DELETE FROM LichSuThayDoiGia WHERE Id=@Id",
                new { Id = id }, tx);
            await SyncCurrentServicePriceAsync(
                conn, tx, item.LoaiDoiTuong, item.DoiTuongId, item.GiaCu);

            await tx.CommitAsync();
            return item;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static void Validate(string loaiDoiTuong, int doiTuongId, int thang, int nam)
    {
        if (loaiDoiTuong is not ("HopDong" or "DichVu"))
            throw new InvalidOperationException("Loại đối tượng giá không hợp lệ.");
        if (doiTuongId <= 0 || thang is < 1 or > 12 || nam is < 2024 or > 2099)
            throw new InvalidOperationException("Đối tượng hoặc kỳ áp dụng không hợp lệ.");
    }

    private static async Task<decimal> LockAndGetBasePriceAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        string loaiDoiTuong,
        int doiTuongId)
    {
        decimal? price = loaiDoiTuong == "HopDong"
            ? await conn.QueryFirstOrDefaultAsync<decimal?>(
                "SELECT TienThueThoaThuan FROM HopDong WHERE Id=@Id FOR UPDATE",
                new { Id = doiTuongId }, tx)
            : await conn.QueryFirstOrDefaultAsync<decimal?>(
                "SELECT DonGia FROM PhongDichVu WHERE Id=@Id FOR UPDATE",
                new { Id = doiTuongId }, tx);
        return price ?? throw new InvalidOperationException("Không tìm thấy đối tượng cần thay đổi giá.");
    }

    private static async Task LockHistoryAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        string loaiDoiTuong,
        int doiTuongId)
        => await conn.QueryAsync<int>(
            """
            SELECT Id FROM LichSuThayDoiGia
            WHERE LoaiDoiTuong=@LoaiDoiTuong AND DoiTuongId=@DoiTuongId
            ORDER BY NamApDung,ThangApDung
            FOR UPDATE
            """,
            new { LoaiDoiTuong = loaiDoiTuong, DoiTuongId = doiTuongId }, tx);

    private static async Task EnsureNoInvoiceFromPeriodAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        string loaiDoiTuong,
        int doiTuongId,
        DateTime ky)
    {
        const string hopDongSql = """
            SELECT Id,Thang,Nam FROM HoaDon
            WHERE HopDongId=@DoiTuongId
              AND (Nam>@Nam OR (Nam=@Nam AND Thang>=@Thang))
            ORDER BY Nam,Thang LIMIT 1 FOR UPDATE
            """;
        const string dichVuSql = """
            SELECT hd.Id,hd.Thang,hd.Nam
            FROM HoaDon hd
            INNER JOIN HopDongDichVu hdv
                ON hdv.HopDongId=hd.HopDongId
               AND hdv.PhongDichVuId=@DoiTuongId
               AND hdv.KyBatDau<=STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d')
               AND (hdv.KyKetThuc IS NULL OR STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d')<hdv.KyKetThuc)
            WHERE hd.Nam>@Nam OR (hd.Nam=@Nam AND hd.Thang>=@Thang)
            ORDER BY hd.Nam,hd.Thang LIMIT 1 FOR UPDATE
            """;

        var invoice = await conn.QueryFirstOrDefaultAsync<LockedInvoice>(
            loaiDoiTuong == "HopDong" ? hopDongSql : dichVuSql,
            new { DoiTuongId = doiTuongId, Thang = ky.Month, Nam = ky.Year }, tx);
        if (invoice != null)
            throw new InvalidOperationException(
                $"Không thể thay đổi lịch sử giá từ kỳ T{ky.Month}/{ky.Year} vì hóa đơn #{invoice.Id} kỳ T{invoice.Thang}/{invoice.Nam} đã được chốt.");
    }

    private static async Task<decimal> ResolvePriceAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        string loaiDoiTuong,
        int doiTuongId,
        DateTime ky,
        decimal fallback)
    {
        var applied = await conn.QueryFirstOrDefaultAsync<decimal?>(
            """
            SELECT GiaMoi FROM LichSuThayDoiGia
            WHERE LoaiDoiTuong=@LoaiDoiTuong AND DoiTuongId=@DoiTuongId
              AND (NamApDung<@Nam OR (NamApDung=@Nam AND ThangApDung<=@Thang))
            ORDER BY NamApDung DESC,ThangApDung DESC LIMIT 1
            """,
            new
            {
                LoaiDoiTuong = loaiDoiTuong,
                DoiTuongId = doiTuongId,
                Thang = ky.Month,
                Nam = ky.Year
            }, tx);
        if (applied.HasValue) return applied.Value;

        return await conn.QueryFirstOrDefaultAsync<decimal?>(
            """
            SELECT GiaCu FROM LichSuThayDoiGia
            WHERE LoaiDoiTuong=@LoaiDoiTuong AND DoiTuongId=@DoiTuongId
            ORDER BY NamApDung,ThangApDung LIMIT 1
            """,
            new { LoaiDoiTuong = loaiDoiTuong, DoiTuongId = doiTuongId }, tx) ?? fallback;
    }

    private static async Task UpdateNextOldPriceAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        string loaiDoiTuong,
        int doiTuongId,
        DateTime ky,
        decimal giaCu)
    {
        var nextId = await conn.QueryFirstOrDefaultAsync<int?>(
            """
            SELECT Id FROM LichSuThayDoiGia
            WHERE LoaiDoiTuong=@LoaiDoiTuong AND DoiTuongId=@DoiTuongId
              AND (NamApDung>@Nam OR (NamApDung=@Nam AND ThangApDung>@Thang))
            ORDER BY NamApDung,ThangApDung LIMIT 1
            """,
            new
            {
                LoaiDoiTuong = loaiDoiTuong,
                DoiTuongId = doiTuongId,
                Thang = ky.Month,
                Nam = ky.Year
            }, tx);
        if (nextId.HasValue)
            await conn.ExecuteAsync(
                "UPDATE LichSuThayDoiGia SET GiaCu=@GiaCu WHERE Id=@Id",
                new { GiaCu = giaCu, Id = nextId.Value }, tx);
    }

    private static async Task SyncCurrentServicePriceAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        string loaiDoiTuong,
        int doiTuongId,
        decimal fallback)
    {
        if (loaiDoiTuong != "DichVu") return;
        var today = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var currentPrice = await ResolvePriceAsync(
            conn, tx, loaiDoiTuong, doiTuongId, today, fallback);
        await conn.ExecuteAsync(
            "UPDATE PhongDichVu SET DonGia=@Gia WHERE Id=@Id",
            new { Gia = currentPrice, Id = doiTuongId }, tx);
    }

    private sealed class LockedInvoice
    {
        public int Id { get; init; }
        public int Thang { get; init; }
        public int Nam { get; init; }
    }
}
