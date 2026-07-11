using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class PhongRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<Phong>> GetAllAsync(int? nhaId = null)
    {
        var sql = nhaId.HasValue
            ? """
              SELECT p.*, n.Id AS NhaSplitId, n.Id, n.TenNha, n.DiaChi, n.GhiChu, n.NgayTao
              FROM Phong p
              INNER JOIN Nha n ON n.Id = p.NhaId
              WHERE p.NhaId = @NhaId
              ORDER BY n.TenNha, p.TenPhong
              """
            : """
              SELECT p.*, n.Id AS NhaSplitId, n.Id, n.TenNha, n.DiaChi, n.GhiChu, n.NgayTao
              FROM Phong p
              INNER JOIN Nha n ON n.Id = p.NhaId
              ORDER BY n.TenNha, p.TenPhong
              """;
        return await _db.QueryAsync<Phong, Nha, Phong>(
            sql,
            (phong, nha) =>
            {
                phong.Nha = nha;
                return phong;
            },
            new { NhaId = nhaId },
            splitOn: "NhaSplitId");
    }

    public async Task<Phong?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT p.*, n.Id AS NhaSplitId, n.Id, n.TenNha, n.DiaChi, n.GhiChu, n.NgayTao
            FROM Phong p
            INNER JOIN Nha n ON n.Id = p.NhaId
            WHERE p.Id = @Id
            """;

        var phong = await _db.QueryAsync<Phong, Nha, Phong>(
            sql,
            (p, n) =>
            {
                p.Nha = n;
                return p;
            },
            new { Id = id },
            splitOn: "NhaSplitId");

        return phong.FirstOrDefault();
    }

    public async Task<int> InsertAsync(Phong phong)
    {
        const string sql = """
            INSERT INTO Phong (NhaId, TenPhong, DienTich, GiaThueMacDinh, TrangThai, GhiChu, NgayTao)
            VALUES (@NhaId, @TenPhong, @DienTich, @GiaThueMacDinh, @TrangThai, @GhiChu, NOW());
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, phong);
    }

    public async Task<int> InsertAsync(IDbConnection conn, IDbTransaction tx, Phong phong)
    {
        const string sql = """
            INSERT INTO Phong (NhaId, TenPhong, DienTich, GiaThueMacDinh, TrangThai, GhiChu, NgayTao)
            VALUES (@NhaId, @TenPhong, @DienTich, @GiaThueMacDinh, @TrangThai, @GhiChu, NOW());
            SELECT LAST_INSERT_ID();
            """;
        return await conn.ExecuteScalarAsync<int>(sql, phong, transaction: tx);
    }

    public async Task UpdateAsync(Phong phong)
    {
        const string sql = """
            UPDATE Phong SET NhaId = @NhaId, TenPhong = @TenPhong, DienTich = @DienTich,
                GiaThueMacDinh = @GiaThueMacDinh, TrangThai = @TrangThai, GhiChu = @GhiChu
            WHERE Id = @Id
            """;
        await _db.ExecuteAsync(sql, phong);
    }

    public async Task UpdateAsync(IDbConnection conn, IDbTransaction tx, Phong phong)
    {
        const string sql = """
            UPDATE Phong SET NhaId = @NhaId, TenPhong = @TenPhong, DienTich = @DienTich,
                GiaThueMacDinh = @GiaThueMacDinh, TrangThai = @TrangThai, GhiChu = @GhiChu
            WHERE Id = @Id
            """;
        await conn.ExecuteAsync(sql, phong, transaction: tx);
    }

    public async Task UpdateTrangThaiAsync(int id, string trangThai)
        => await _db.ExecuteAsync(
            "UPDATE Phong SET TrangThai = @TrangThai WHERE Id = @Id",
            new { Id = id, TrangThai = trangThai });

    public async Task UpdateTrangThaiAsync(IDbConnection conn, IDbTransaction tx, int id, string trangThai)
        => await conn.ExecuteAsync(
            "UPDATE Phong SET TrangThai = @TrangThai WHERE Id = @Id",
            new { Id = id, TrangThai = trangThai },
            transaction: tx);

    public async Task DeleteAsync(int id)
        => await _db.ExecuteAsync("DELETE FROM Phong WHERE Id = @Id", new { Id = id });

    // ── Bổ sung Phase 3 ──────────────────────────────────────────────────────

    public async Task<IEnumerable<Phong>> GetPhongTrongAsync()
        => await _db.QueryAsync<Phong>(
            "SELECT * FROM Phong WHERE TrangThai='Trong' ORDER BY TenPhong");

    public async Task CapNhatGiaThueMacDinhAsync(int phongId, decimal giaMoi)
        => await _db.ExecuteAsync(
            "UPDATE Phong SET GiaThueMacDinh=@Gia WHERE Id=@Id",
            new { Gia = giaMoi, Id = phongId });
}
