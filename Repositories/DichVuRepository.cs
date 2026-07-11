using Dapper;
using System.Data;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Repositories;

public class DichVuRepository(IDbConnection db) : BaseRepository(db)
{
    public async Task<IEnumerable<DichVu>> GetAllAsync(bool? hienThi = null)
    {
        var rows=(await _db.QueryAsync<DichVu>("SELECT * FROM DichVu ORDER BY TenDichVu")).ToList();
        foreach(var row in rows) await ResolveCurrentAsync(row);
        return rows;
    }

    public async Task<DichVu?> GetByIdAsync(int id)
    {
        var row=await _db.QueryFirstOrDefaultAsync<DichVu>("SELECT * FROM DichVu WHERE Id = @Id",new{Id=id});
        if(row!=null) await ResolveCurrentAsync(row);
        return row;
    }

    public async Task<int> InsertAsync(DichVu dichVu)
    {
        const string sql = """
            INSERT INTO DichVu (TenDichVu, LoaiTinhPhi, CachTinhCoDinh, DonViTinh, DonGiaMacDinh, BatBuocKhiThue)
            VALUES (@TenDichVu, @LoaiTinhPhi, @CachTinhCoDinh, @DonViTinh, @DonGiaMacDinh, @BatBuocKhiThue);
            SELECT LAST_INSERT_ID();
            """;
        return await _db.ExecuteScalarAsync<int>(sql, dichVu);
    }

    public async Task UpdateAsync(DichVu dichVu)
    {
        const string sql = """
            UPDATE DichVu SET TenDichVu = @TenDichVu, LoaiTinhPhi = @LoaiTinhPhi,
                CachTinhCoDinh = @CachTinhCoDinh,
                DonViTinh = @DonViTinh,
                DonGiaMacDinh = @DonGiaMacDinh,
                BatBuocKhiThue = @BatBuocKhiThue
            WHERE Id = @Id
            """;
        await _db.ExecuteAsync(sql, dichVu);
    }

    public async Task<bool> DaTungGanPhongAsync(int dichVuId)
        => await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PhongDichVu WHERE DichVuId=@DichVuId", new { DichVuId = dichVuId }) > 0;

    public async Task UpdateThongTinAsync(DichVu dichVu)
        => await _db.ExecuteAsync("""
            UPDATE DichVu SET TenDichVu=@TenDichVu, DonViTinh=@DonViTinh,
                DonGiaMacDinh=@DonGiaMacDinh, BatBuocKhiThue=@BatBuocKhiThue
            WHERE Id=@Id
            """, dichVu);

    private async Task ResolveCurrentAsync(DichVu row)
    {
        var effective=await _db.QueryFirstOrDefaultAsync<(string LoaiTinhPhiMoi,string CachTinhCoDinhMoi)>("SELECT LoaiTinhPhiMoi,CachTinhCoDinhMoi FROM LichSuHinhThucDichVu WHERE DichVuId=@Id AND KyApDung<=@Today ORDER BY KyApDung DESC LIMIT 1",new{row.Id,Today=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1)});
        if(!string.IsNullOrEmpty(effective.LoaiTinhPhiMoi)){row.LoaiTinhPhi=effective.LoaiTinhPhiMoi;row.CachTinhCoDinh=effective.CachTinhCoDinhMoi;}
        else {var first=await _db.QueryFirstOrDefaultAsync<(string LoaiTinhPhiCu,string CachTinhCoDinhCu)>("SELECT LoaiTinhPhiCu,CachTinhCoDinhCu FROM LichSuHinhThucDichVu WHERE DichVuId=@Id ORDER BY KyApDung LIMIT 1",new{row.Id});if(!string.IsNullOrEmpty(first.LoaiTinhPhiCu)){row.LoaiTinhPhi=first.LoaiTinhPhiCu;row.CachTinhCoDinh=first.CachTinhCoDinhCu;}}
    }
}
