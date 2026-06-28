using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class LichSuThayDoiGiaRepository(IDbConnection db) : BaseRepository(db)
{
    /// <summary>
    /// Lấy giá áp dụng cho kỳ (thang, nam) — dùng trong LayGiaApDung của Service.
    /// Trả về bản ghi có ThangApDung/NamApDung gần nhất mà không vượt quá kỳ đang lập.
    /// </summary>
    public async Task<LichSuThayDoiGia?> GetGiaApDungAsync(string loaiDoiTuong, int doiTuongId, int thang, int nam)
    {
        const string sql = """
            SELECT * FROM LichSuThayDoiGia
            WHERE LoaiDoiTuong = @LoaiDoiTuong AND DoiTuongId = @DoiTuongId
              AND (NamApDung < @Nam OR (NamApDung = @Nam AND ThangApDung <= @Thang))
            ORDER BY NamApDung DESC, ThangApDung DESC
            LIMIT 1
            """;
        return await _db.QueryFirstOrDefaultAsync<LichSuThayDoiGia>(sql,
            new { LoaiDoiTuong = loaiDoiTuong, DoiTuongId = doiTuongId, Thang = thang, Nam = nam });
    }

    public async Task<IEnumerable<LichSuThayDoiGia>> GetByDoiTuongAsync(string loaiDoiTuong, int doiTuongId)
        => await _db.QueryAsync<LichSuThayDoiGia>(
            """
            SELECT * FROM LichSuThayDoiGia
            WHERE LoaiDoiTuong = @LoaiDoiTuong AND DoiTuongId = @DoiTuongId
            ORDER BY NamApDung DESC, ThangApDung DESC
            """,
            new { LoaiDoiTuong = loaiDoiTuong, DoiTuongId = doiTuongId });

    public async Task<int> InsertAsync(LichSuThayDoiGia ls)
    {
        const string sql = """
            INSERT INTO LichSuThayDoiGia
                (LoaiDoiTuong, DoiTuongId, GiaCu, GiaMoi, ThangApDung, NamApDung, LyDo, NgayTao)
            VALUES
                (@LoaiDoiTuong, @DoiTuongId, @GiaCu, @GiaMoi, @ThangApDung, @NamApDung, @LyDo, NOW());
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, ls);
    }

    public async Task DeleteAsync(int id)
        => await _db.ExecuteAsync(
            "DELETE FROM LichSuThayDoiGia WHERE Id=@Id", new { Id = id });
}
