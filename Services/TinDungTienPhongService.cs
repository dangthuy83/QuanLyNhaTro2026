using System.Data;
using Dapper;
using MySqlConnector;

namespace QuanLyNhaTro.Services;

public class TinDungTienPhongService(IDbConnection db)
{
    public async Task<decimal> GetSoDuAsync(int hopDongId)
        => await db.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(SoTien),0) FROM GiaoDichTinDungTienPhong WHERE HopDongId=@HopDongId",
            new { HopDongId = hopDongId });

    public static async Task<decimal> GetSoDuForUpdateAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongId)
    {
        await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT Id FROM HopDong WHERE Id=@HopDongId FOR UPDATE",
            new { HopDongId = hopDongId }, tx);
        return await conn.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(SoTien),0) FROM GiaoDichTinDungTienPhong WHERE HopDongId=@HopDongId",
            new { HopDongId = hopDongId }, tx);
    }

    public static async Task<decimal> ApDungVaoHoaDonAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongId,
        int hoaDonId,
        decimal toiDa,
        DateTime ngay,
        string nguoiThucHien)
    {
        var idempotencyKey = $"AP_DUNG_HOA_DON:{hoaDonId}";
        var existing = await conn.QueryFirstOrDefaultAsync<decimal?>(
            "SELECT -SoTien FROM GiaoDichTinDungTienPhong WHERE IdempotencyKey=@Key",
            new { Key = idempotencyKey }, tx);
        if (existing.HasValue) return existing.Value;

        var soDu = await GetSoDuForUpdateAsync(conn, tx, hopDongId);
        var apDung = Math.Min(Math.Max(0, soDu), Math.Max(0, toiDa));
        if (apDung <= 0) return 0;

        await conn.ExecuteAsync(
            """
            INSERT INTO GiaoDichTinDungTienPhong
                (HopDongId,HoaDonId,LoaiGiaoDich,SoTien,SoDuSauGiaoDich,NgayGiaoDich,
                 IdempotencyKey,LyDo,NguoiThucHien)
            VALUES
                (@HopDongId,@HoaDonId,'ApDungHoaDon',-@SoTien,@SoDuSau,@Ngay,@Key,
                 'Tự động bù trừ vào kỳ thu','Hệ thống')
            """,
            new
            {
                HopDongId = hopDongId,
                HoaDonId = hoaDonId,
                SoTien = apDung,
                SoDuSau = soDu - apDung,
                Ngay = ngay.Date,
                Key = idempotencyKey,
                NguoiThucHien = nguoiThucHien
            }, tx);
        return apDung;
    }

    public static async Task<decimal> TaoKhiTraPhongAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongId,
        decimal soTien,
        DateTime ngay,
        string nguoiThucHien)
        => await GhiTangAsync(
            conn, tx, hopDongId, null, "TaoKhiTraPhong", soTien, ngay,
            $"TRA_PHONG:{hopDongId}:{ngay:yyyyMMdd}",
            "Tiền phòng trả trước của những ngày chưa ở",
            nguoiThucHien);

    public static async Task<decimal> ChuyenSangHopDongAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongCuId,
        int hopDongMoiId,
        decimal soTien,
        DateTime ngay,
        string nguoiThucHien)
        => await GhiTangAsync(
            conn, tx, hopDongMoiId, hopDongCuId, "ChuyenSangHopDong", soTien, ngay,
            $"CHUYEN_PHONG:{hopDongCuId}:{hopDongMoiId}:{ngay:yyyyMMdd}",
            $"Tín dụng chênh lệch tiền phòng từ hợp đồng #{hopDongCuId}",
            nguoiThucHien);

    public static async Task<decimal> HoanTienAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongId,
        DateTime ngay,
        string nguoiThucHien,
        string idempotencyKey)
    {
        var existing = await conn.QueryFirstOrDefaultAsync<decimal?>(
            "SELECT -SoTien FROM GiaoDichTinDungTienPhong WHERE IdempotencyKey=@Key",
            new { Key = idempotencyKey }, tx);
        if (existing.HasValue) return existing.Value;

        var soDu = await GetSoDuForUpdateAsync(conn, tx, hopDongId);
        if (soDu <= 0) return 0;

        await conn.ExecuteAsync(
            """
            INSERT INTO GiaoDichTinDungTienPhong
                (HopDongId,LoaiGiaoDich,SoTien,SoDuSauGiaoDich,NgayGiaoDich,
                 IdempotencyKey,LyDo,NguoiThucHien)
            VALUES
                (@HopDongId,'HoanTien',-@SoTien,0,@Ngay,@Key,
                 'Hoàn phần tín dụng tiền phòng còn lại',@NguoiThucHien)
            """,
            new
            {
                HopDongId = hopDongId,
                SoTien = soDu,
                Ngay = ngay.Date,
                Key = idempotencyKey,
                NguoiThucHien = nguoiThucHien
            }, tx);
        return soDu;
    }

    private static async Task<decimal> GhiTangAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongId,
        int? hopDongLienQuanId,
        string loaiGiaoDich,
        decimal soTien,
        DateTime ngay,
        string idempotencyKey,
        string lyDo,
        string nguoiThucHien)
    {
        if (soTien <= 0) return 0;
        var existing = await conn.QueryFirstOrDefaultAsync<decimal?>(
            "SELECT SoTien FROM GiaoDichTinDungTienPhong WHERE IdempotencyKey=@Key",
            new { Key = idempotencyKey }, tx);
        if (existing.HasValue) return existing.Value;

        var soDu = await GetSoDuForUpdateAsync(conn, tx, hopDongId);
        await conn.ExecuteAsync(
            """
            INSERT INTO GiaoDichTinDungTienPhong
                (HopDongId,HopDongLienQuanId,LoaiGiaoDich,SoTien,SoDuSauGiaoDich,
                 NgayGiaoDich,IdempotencyKey,LyDo,NguoiThucHien)
            VALUES
                (@HopDongId,@HopDongLienQuanId,@LoaiGiaoDich,@SoTien,@SoDuSau,
                 @Ngay,@Key,@LyDo,@NguoiThucHien)
            """,
            new
            {
                HopDongId = hopDongId,
                HopDongLienQuanId = hopDongLienQuanId,
                LoaiGiaoDich = loaiGiaoDich,
                SoTien = soTien,
                SoDuSau = soDu + soTien,
                Ngay = ngay.Date,
                Key = idempotencyKey,
                LyDo = lyDo,
                NguoiThucHien = nguoiThucHien
            }, tx);
        return soTien;
    }
}
