using Dapper;
using MySqlConnector;
using System.Data;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public class GiaService(
    IDbConnection db,
    LichSuThayDoiGiaRepository lichSuRepo)
{
    public async Task LuuThayDoiAsync(ThayDoiGiaViewModel vm)
    {
        if (vm.LoaiDoiTuong is not ("Phong" or "DichVu"))
            throw new InvalidOperationException("Loai doi tuong gia khong hop le.");

        var ky = new DateTime(vm.NamApDung, vm.ThangApDung, 1);
        var kyTruoc = ky.AddMonths(-1);
        var giaTruocKy = await lichSuRepo.GetGiaTriApDungAsync(
            vm.LoaiDoiTuong, vm.DoiTuongId, kyTruoc.Month, kyTruoc.Year)
            ?? await GetGiaGocAsync(vm.LoaiDoiTuong, vm.DoiTuongId);

        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var duplicate = await conn.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*) FROM LichSuThayDoiGia
                WHERE LoaiDoiTuong = @LoaiDoiTuong AND DoiTuongId = @DoiTuongId
                  AND ThangApDung = @Thang AND NamApDung = @Nam
                """,
                new
                {
                    vm.LoaiDoiTuong,
                    vm.DoiTuongId,
                    Thang = vm.ThangApDung,
                    Nam = vm.NamApDung
                },
                transaction: tx);
            if (duplicate > 0)
                throw new InvalidOperationException("Da co mot thay doi gia cho doi tuong trong ky nay.");

            await conn.ExecuteAsync(
                """
                INSERT INTO LichSuThayDoiGia
                    (LoaiDoiTuong, DoiTuongId, GiaCu, GiaMoi, ThangApDung, NamApDung, LyDo, NgayTao)
                VALUES
                    (@LoaiDoiTuong, @DoiTuongId, @GiaCu, @GiaMoi, @ThangApDung, @NamApDung, @LyDo, NOW())
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
                },
                transaction: tx);

            var nextId = await conn.QueryFirstOrDefaultAsync<int?>(
                """
                SELECT Id FROM LichSuThayDoiGia
                WHERE LoaiDoiTuong = @LoaiDoiTuong AND DoiTuongId = @DoiTuongId
                  AND (NamApDung > @Nam OR (NamApDung = @Nam AND ThangApDung > @Thang))
                ORDER BY NamApDung, ThangApDung
                LIMIT 1
                """,
                new
                {
                    vm.LoaiDoiTuong,
                    vm.DoiTuongId,
                    Thang = vm.ThangApDung,
                    Nam = vm.NamApDung
                },
                transaction: tx);
            if (nextId.HasValue)
            {
                await conn.ExecuteAsync(
                    "UPDATE LichSuThayDoiGia SET GiaCu = @GiaCu WHERE Id = @Id",
                    new { GiaCu = vm.GiaMoi, Id = nextId.Value },
                    transaction: tx);
            }

            var giaMoiNhat = await conn.ExecuteScalarAsync<decimal>(
                """
                SELECT GiaMoi FROM LichSuThayDoiGia
                WHERE LoaiDoiTuong = @LoaiDoiTuong AND DoiTuongId = @DoiTuongId
                ORDER BY NamApDung DESC, ThangApDung DESC
                LIMIT 1
                """,
                new { vm.LoaiDoiTuong, vm.DoiTuongId },
                transaction: tx);
            if (vm.LoaiDoiTuong == "Phong")
            {
                await conn.ExecuteAsync(
                    "UPDATE Phong SET GiaThueMacDinh = @Gia WHERE Id = @Id",
                    new { Gia = giaMoiNhat, Id = vm.DoiTuongId },
                    transaction: tx);
            }
            else
            {
                await conn.ExecuteAsync(
                    "UPDATE PhongDichVu SET DonGia = @Gia WHERE Id = @Id",
                    new { Gia = giaMoiNhat, Id = vm.DoiTuongId },
                    transaction: tx);
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task XoaThayDoiAsync(int id)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var item = await conn.QueryFirstOrDefaultAsync<LichSuThayDoiGia>(
                "SELECT * FROM LichSuThayDoiGia WHERE Id = @Id",
                new { Id = id },
                transaction: tx)
                ?? throw new InvalidOperationException("Khong tim thay ban ghi thay doi gia.");

            var nextId = await conn.QueryFirstOrDefaultAsync<int?>(
                """
                SELECT Id FROM LichSuThayDoiGia
                WHERE LoaiDoiTuong = @LoaiDoiTuong AND DoiTuongId = @DoiTuongId
                  AND (NamApDung > @Nam OR (NamApDung = @Nam AND ThangApDung > @Thang))
                ORDER BY NamApDung, ThangApDung
                LIMIT 1
                """,
                new
                {
                    item.LoaiDoiTuong,
                    item.DoiTuongId,
                    Thang = item.ThangApDung,
                    Nam = item.NamApDung
                },
                transaction: tx);
            if (nextId.HasValue)
            {
                await conn.ExecuteAsync(
                    "UPDATE LichSuThayDoiGia SET GiaCu = @GiaCu WHERE Id = @Id",
                    new { item.GiaCu, Id = nextId.Value },
                    transaction: tx);
            }

            await conn.ExecuteAsync(
                "DELETE FROM LichSuThayDoiGia WHERE Id = @Id",
                new { Id = id },
                transaction: tx);

            var latest = await conn.QueryFirstOrDefaultAsync<decimal?>(
                """
                SELECT GiaMoi FROM LichSuThayDoiGia
                WHERE LoaiDoiTuong = @LoaiDoiTuong AND DoiTuongId = @DoiTuongId
                ORDER BY NamApDung DESC, ThangApDung DESC
                LIMIT 1
                """,
                new { item.LoaiDoiTuong, item.DoiTuongId },
                transaction: tx);
            var currentPrice = latest ?? item.GiaCu;
            var updateSql = item.LoaiDoiTuong == "Phong"
                ? "UPDATE Phong SET GiaThueMacDinh = @Gia WHERE Id = @Id"
                : "UPDATE PhongDichVu SET DonGia = @Gia WHERE Id = @Id";
            await conn.ExecuteAsync(
                updateSql,
                new { Gia = currentPrice, Id = item.DoiTuongId },
                transaction: tx);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<decimal> GetGiaGocAsync(string loaiDoiTuong, int doiTuongId)
        => loaiDoiTuong == "Phong"
            ? await db.ExecuteScalarAsync<decimal>(
                "SELECT GiaThueMacDinh FROM Phong WHERE Id = @Id", new { Id = doiTuongId })
            : await db.ExecuteScalarAsync<decimal>(
                "SELECT DonGia FROM PhongDichVu WHERE Id = @Id", new { Id = doiTuongId });
}
