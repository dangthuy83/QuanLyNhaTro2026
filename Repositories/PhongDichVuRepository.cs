using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class PhongDichVuRepository(IDbConnection db) : BaseRepository(db)
{
    /// <summary>Lấy toàn bộ dịch vụ của 1 phòng (kèm thông tin DichVu)</summary>
    public async Task<IEnumerable<PhongDichVu>> GetByPhongAsync(int phongId)
    {
        const string sql = """
            SELECT pdv.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.CachTinhCoDinh, dv.DonViTinh, dv.DonGiaMacDinh, dv.BatBuocKhiThue
            FROM PhongDichVu pdv
            INNER JOIN DichVu dv ON dv.Id = pdv.DichVuId
            WHERE pdv.PhongId = @PhongId AND pdv.DangApDung = 1
            ORDER BY dv.TenDichVu
            """;
        return await _db.QueryAsync<PhongDichVu, DichVu, PhongDichVu>(
            sql,
            (pdv, dv) => { pdv.DichVu = dv; return pdv; },
            new { PhongId = phongId },
            splitOn: "Id");
    }

    public async Task<IEnumerable<PhongDichVu>> GetByPhongKyAsync(int phongId, int thang, int nam)
    {
        var rows=(await GetByPhongAsync(phongId)).ToList();
        var ky=new DateTime(nam,thang,1);
        foreach(var row in rows.Where(x=>x.DichVu!=null))
        {
            var effective=await _db.QueryFirstOrDefaultAsync<(string LoaiTinhPhiMoi,string CachTinhCoDinhMoi)>("SELECT LoaiTinhPhiMoi,CachTinhCoDinhMoi FROM LichSuHinhThucDichVu WHERE DichVuId=@DichVuId AND KyApDung<=@Ky ORDER BY KyApDung DESC LIMIT 1",new{DichVuId=row.DichVuId,Ky=ky});
            if(!string.IsNullOrEmpty(effective.LoaiTinhPhiMoi)){row.DichVu!.LoaiTinhPhi=effective.LoaiTinhPhiMoi;row.DichVu.CachTinhCoDinh=effective.CachTinhCoDinhMoi;}
            else { var first=await _db.QueryFirstOrDefaultAsync<(string LoaiTinhPhiCu,string CachTinhCoDinhCu)>("SELECT LoaiTinhPhiCu,CachTinhCoDinhCu FROM LichSuHinhThucDichVu WHERE DichVuId=@DichVuId ORDER BY KyApDung LIMIT 1",new{DichVuId=row.DichVuId}); if(!string.IsNullOrEmpty(first.LoaiTinhPhiCu)){row.DichVu!.LoaiTinhPhi=first.LoaiTinhPhiCu;row.DichVu.CachTinhCoDinh=first.CachTinhCoDinhCu;} }
        }
        return rows;
    }

    public async Task<IEnumerable<PhongDichVu>> GetAllByPhongAsync(int phongId)
    {
        const string sql = """
            SELECT pdv.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.CachTinhCoDinh,
                   dv.DonViTinh, dv.DonGiaMacDinh, dv.BatBuocKhiThue
            FROM PhongDichVu pdv
            INNER JOIN DichVu dv ON dv.Id = pdv.DichVuId
            WHERE pdv.PhongId = @PhongId
            ORDER BY dv.TenDichVu
            """;
        return await _db.QueryAsync<PhongDichVu, DichVu, PhongDichVu>(
            sql,
            (pdv, dv) => { pdv.DichVu = dv; return pdv; },
            new { PhongId = phongId },
            splitOn: "Id");
    }

    public async Task<List<PhongDichVu>> GetSelectedForPhongAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int phongId,
        IEnumerable<int> phongDichVuIds,
        bool requireActive = true)
    {
        var ids = phongDichVuIds.Distinct().ToArray();
        if (ids.Length == 0) return [];

        const string sql = """
            SELECT pdv.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.CachTinhCoDinh,
                   dv.DonViTinh, dv.DonGiaMacDinh, dv.BatBuocKhiThue
            FROM PhongDichVu pdv
            INNER JOIN DichVu dv ON dv.Id = pdv.DichVuId
            WHERE pdv.PhongId = @PhongId
              AND (@RequireActive = 0 OR pdv.DangApDung = 1)
              AND pdv.Id IN @Ids
            ORDER BY dv.TenDichVu
            """;
        var rows = await conn.QueryAsync<PhongDichVu, DichVu, PhongDichVu>(
            sql,
            (pdv, dv) => { pdv.DichVu = dv; return pdv; },
            new { PhongId = phongId, Ids = ids, RequireActive = requireActive },
            transaction: tx,
            splitOn: "Id");
        return rows.ToList();
    }

    public async Task<HashSet<int>> GetRequiredIdsForPhongAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int phongId)
    {
        var ids = await conn.QueryAsync<int>(
            """
            SELECT pdv.Id
            FROM PhongDichVu pdv
            INNER JOIN DichVu dv ON dv.Id = pdv.DichVuId
            WHERE pdv.PhongId = @PhongId
              AND pdv.DangApDung = 1
              AND dv.BatBuocKhiThue = 1
            """,
            new { PhongId = phongId },
            transaction: tx);
        return ids.ToHashSet();
    }

    public async Task SyncForPhongAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int phongId,
        IReadOnlyDictionary<int, decimal> selectedDichVuPrices)
    {
        await conn.ExecuteAsync(
            "UPDATE PhongDichVu SET DangApDung = 0 WHERE PhongId = @PhongId",
            new { PhongId = phongId },
            transaction: tx);

        const string sql = """
            INSERT INTO PhongDichVu (PhongId, DichVuId, DonGia, DangApDung)
            VALUES (@PhongId, @DichVuId, @DonGia, 1)
            ON DUPLICATE KEY UPDATE DonGia = @DonGia, DangApDung = 1
            """;
        var items = selectedDichVuPrices.Select(x => new
        {
            PhongId = phongId,
            DichVuId = x.Key,
            DonGia = x.Value
        });
        await conn.ExecuteAsync(sql, items, transaction: tx);
    }

    public async Task<int> InsertAsync(PhongDichVu pdv)
    {
        const string sql = """
            INSERT INTO PhongDichVu (PhongId, DichVuId, DonGia, DangApDung)
            VALUES (@PhongId, @DichVuId, @DonGia, @DangApDung);
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, pdv);
    }

    public async Task<IEnumerable<GanDichVuHangLoatRow>> GetBulkAssignmentRowsAsync(
        int dichVuId,
        int? nhaId,
        string trangThai)
    {
        const string sql = """
            SELECT
                p.Id AS PhongId,
                n.TenNha,
                p.TenPhong,
                p.TrangThai,
                hd.Id AS HopDongId,
                COUNT(hdkt.Id) AS SoKhach,
                pdv.Id AS PhongDichVuId,
                pdv.DonGia AS DonGiaHienTai,
                COALESCE(pdv.DangApDung, 0) AS DangApDung
            FROM Phong p
            INNER JOIN Nha n ON n.Id = p.NhaId
            LEFT JOIN HopDong hd
                ON hd.PhongId = p.Id
               AND hd.TrangThai = 'DangHieuLuc'
            LEFT JOIN HopDongKhachThue hdkt
                ON hdkt.HopDongId = hd.Id
               AND hdkt.NgayBatDau <= CURDATE()
               AND (hdkt.NgayKetThuc IS NULL OR hdkt.NgayKetThuc >= CURDATE())
            LEFT JOIN PhongDichVu pdv
                ON pdv.PhongId = p.Id
               AND pdv.DichVuId = @DichVuId
            WHERE (@NhaId IS NULL OR p.NhaId = @NhaId)
              AND (@TrangThai = 'TatCa' OR p.TrangThai = @TrangThai)
            GROUP BY
                p.Id, n.TenNha, p.TenPhong, p.TrangThai,
                hd.Id, pdv.Id, pdv.DonGia, pdv.DangApDung
            ORDER BY n.TenNha, p.TenPhong
            """;

        return await _db.QueryAsync<GanDichVuHangLoatRow>(
            sql,
            new { DichVuId = dichVuId, NhaId = nhaId, TrangThai = trangThai });
    }

    public async Task<int> UpsertBulkAsync(IEnumerable<int> phongIds, int dichVuId, decimal donGia)
    {
        const string sql = """
            INSERT INTO PhongDichVu (PhongId, DichVuId, DonGia, DangApDung)
            VALUES (@PhongId, @DichVuId, @DonGia, 1)
            ON DUPLICATE KEY UPDATE
                DangApDung = 1
            """;

        var items = phongIds
            .Distinct()
            .Select(phongId => new { PhongId = phongId, DichVuId = dichVuId, DonGia = donGia })
            .ToList();

        if (items.Count == 0) return 0;

        return await _db.ExecuteAsync(sql, items);
    }

    public async Task UpdateDonGiaAsync(int id, decimal donGiaMoi)
        => await _db.ExecuteAsync(
            "UPDATE PhongDichVu SET DonGia = @DonGia WHERE Id = @Id",
            new { Id = id, DonGia = donGiaMoi });

    public async Task SetApDungAsync(int id, bool apDung)
        => await _db.ExecuteAsync(
            "UPDATE PhongDichVu SET DangApDung = @DangApDung WHERE Id = @Id",
            new { Id = id, DangApDung = apDung });

    // ── Bổ sung Phase 3 ──────────────────────────────────────────────────────

    public async Task<PhongDichVu?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT pdv.*, dv.Id, dv.TenDichVu, dv.LoaiTinhPhi, dv.CachTinhCoDinh, dv.DonViTinh, dv.DonGiaMacDinh, dv.BatBuocKhiThue
            FROM PhongDichVu pdv
            JOIN DichVu dv ON pdv.DichVuId = dv.Id
            WHERE pdv.Id = @Id
            """;
        var rows = await _db.QueryAsync<PhongDichVu, DichVu, PhongDichVu>(
            sql,
            (pdv, dv) => { pdv.DichVu = dv; return pdv; },
            new { Id = id }, splitOn: "Id");
        return rows.FirstOrDefault();
    }
}
