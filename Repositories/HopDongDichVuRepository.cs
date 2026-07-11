using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class HopDongDichVuRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<PhongDichVu>> GetPhongDichVuByHopDongKyAsync(
        int hopDongId,
        int thang,
        int nam)
        => await GetPhongDichVuByHopDongKyAsync(_db, null, hopDongId, thang, nam);

    public async Task<IEnumerable<PhongDichVu>> GetPhongDichVuByHopDongKyAsync(
        IDbConnection conn,
        IDbTransaction? tx,
        int hopDongId,
        int thang,
        int nam)
    {
        var ky = new DateTime(nam, thang, 1);
        const string sql = """
            SELECT pdv.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.CachTinhCoDinh,
                   dv.DonViTinh, dv.DonGiaMacDinh, dv.BatBuocKhiThue
            FROM HopDongDichVu hdv
            INNER JOIN PhongDichVu pdv ON pdv.Id = hdv.PhongDichVuId
            INNER JOIN DichVu dv ON dv.Id = pdv.DichVuId
            WHERE hdv.HopDongId = @HopDongId
              AND hdv.KyBatDau <= @Ky
              AND (hdv.KyKetThuc IS NULL OR @Ky < hdv.KyKetThuc)
            ORDER BY dv.TenDichVu
            """;

        var rows = (await conn.QueryAsync<PhongDichVu, DichVu, PhongDichVu>(
            sql,
            (pdv, dv) => { pdv.DichVu = dv; return pdv; },
            new { HopDongId = hopDongId, Ky = ky },
            transaction: tx,
            splitOn: "Id")).ToList();
        foreach (var row in rows.Where(x => x.DichVu != null))
        {
            var effective = await conn.QueryFirstOrDefaultAsync<(string LoaiTinhPhiMoi, string CachTinhCoDinhMoi)>("""
                SELECT LoaiTinhPhiMoi,CachTinhCoDinhMoi FROM LichSuHinhThucDichVu
                WHERE DichVuId=@DichVuId AND KyApDung<=@Ky ORDER BY KyApDung DESC LIMIT 1
                """, new { DichVuId=row.DichVuId, Ky=ky }, transaction:tx);
            if (!string.IsNullOrEmpty(effective.LoaiTinhPhiMoi)) { row.DichVu!.LoaiTinhPhi=effective.LoaiTinhPhiMoi; row.DichVu.CachTinhCoDinh=effective.CachTinhCoDinhMoi; }
            else
            {
                var first = await conn.QueryFirstOrDefaultAsync<(string LoaiTinhPhiCu,string CachTinhCoDinhCu)>("SELECT LoaiTinhPhiCu,CachTinhCoDinhCu FROM LichSuHinhThucDichVu WHERE DichVuId=@DichVuId ORDER BY KyApDung LIMIT 1", new { DichVuId=row.DichVuId }, transaction:tx);
                if (!string.IsNullOrEmpty(first.LoaiTinhPhiCu)) { row.DichVu!.LoaiTinhPhi=first.LoaiTinhPhiCu; row.DichVu.CachTinhCoDinh=first.CachTinhCoDinhCu; }
            }
        }
        return rows;
    }

    public async Task<HashSet<int>> GetPhongDichVuIdsByHopDongKyAsync(
        int hopDongId,
        int thang,
        int nam)
        => (await GetPhongDichVuByHopDongKyAsync(hopDongId, thang, nam))
            .Select(x => x.Id)
            .ToHashSet();

    public async Task InsertManyAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int hopDongId,
        IEnumerable<int> phongDichVuIds,
        DateTime kyBatDau)
    {
        const string sql = """
            INSERT INTO HopDongDichVu (HopDongId, PhongDichVuId, KyBatDau, KyKetThuc, NgayTao)
            VALUES (@HopDongId, @PhongDichVuId, @KyBatDau, NULL, NOW())
            """;
        var items = phongDichVuIds
            .Distinct()
            .Select(id => new
            {
                HopDongId = hopDongId,
                PhongDichVuId = id,
                KyBatDau = new DateTime(kyBatDau.Year, kyBatDau.Month, 1)
            })
            .ToList();

        if (items.Count > 0)
            await conn.ExecuteAsync(sql, items, transaction: tx);
    }

    public async Task ReplaceFromPeriodAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int hopDongId,
        IEnumerable<int> selectedPhongDichVuIds,
        DateTime kyApDung)
    {
        var ky = new DateTime(kyApDung.Year, kyApDung.Month, 1);
        var selected = selectedPhongDichVuIds.Distinct().ToHashSet();

        var crossing = (await conn.QueryAsync<HopDongDichVu>(
            """
            SELECT *
            FROM HopDongDichVu
            WHERE HopDongId = @HopDongId
              AND KyBatDau < @Ky
              AND (KyKetThuc IS NULL OR KyKetThuc > @Ky)
            """,
            new { HopDongId = hopDongId, Ky = ky },
            transaction: tx)).ToList();

        await conn.ExecuteAsync(
            "DELETE FROM HopDongDichVu WHERE HopDongId = @HopDongId AND KyBatDau >= @Ky",
            new { HopDongId = hopDongId, Ky = ky },
            transaction: tx);

        foreach (var item in crossing)
        {
            await conn.ExecuteAsync(
                "UPDATE HopDongDichVu SET KyKetThuc = @KyKetThuc WHERE Id = @Id",
                new { item.Id, KyKetThuc = selected.Contains(item.PhongDichVuId) ? (DateTime?)null : ky },
                transaction: tx);
        }

        var alreadyOpen = crossing
            .Where(x => selected.Contains(x.PhongDichVuId))
            .Select(x => x.PhongDichVuId)
            .ToHashSet();
        await InsertManyAsync(conn, tx, hopDongId, selected.Except(alreadyOpen), ky);
    }
}
