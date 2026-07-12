using Dapper;
using MySqlConnector;
using System.Data;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public class GiaoDichCocService(
    IDbConnection db,
    HopDongRepository hopDongRepo,
    GiaoDichCocRepository giaoDichCocRepo,
    CongNoSettlementService congNoSettlementService)
{
    public async Task<decimal> GetSoDuHienTaiAsync(int hopDongId)
    {
        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId)
            ?? throw new InvalidOperationException("Khong tim thay hop dong.");

        var soDu = await giaoDichCocRepo.GetSoDuAsync(hopDongId);
        var giaoDich = await giaoDichCocRepo.GetByHopDongAsync(hopDongId);
        return giaoDich.Any() ? soDu : hopDong.TienCoc;
    }

    public async Task GhiNhanThuCongAsync(
        int hopDongId,
        string loaiGiaoDich,
        decimal soTien,
        DateTime ngayGiaoDich,
        int? hoaDonId,
        string? phuongThuc,
        string? ghiChu)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var hopDong = await LoadHopDongAsync(conn, tx, hopDongId)
                ?? throw new InvalidOperationException("Khong tim thay hop dong.");

            ValidateGiaoDichThuCong(
                hopDong, loaiGiaoDich, ngayGiaoDich, hoaDonId, phuongThuc);

            if (loaiGiaoDich == "TruNo")
            {
                var hoaDonHopDongId = await conn.ExecuteScalarAsync<int?>(
                    "SELECT HopDongId FROM HoaDon WHERE Id = @HoaDonId",
                    new { HoaDonId = hoaDonId!.Value },
                    transaction: tx);
                if (hoaDonHopDongId != hopDongId)
                    throw new InvalidOperationException("Hoa don tru no phai thuoc cung hop dong.");
            }

            await EnsureOpeningBalanceAsync(conn, tx, hopDong);

            decimal delta = NormalizeDelta(loaiGiaoDich, soTien);
            await InsertDeltaAsync(
                conn, tx, hopDongId, loaiGiaoDich, delta, ngayGiaoDich,
                hoaDonId, phuongThuc, ghiChu);

            if (loaiGiaoDich == "TruNo")
            {
                decimal soTienTruNo = Math.Abs(delta);
                decimal daTatToan = hoaDonId.HasValue
                    ? await congNoSettlementService.ThanhToanHoaDonAsync(
                        conn,
                        tx,
                        hoaDonId.Value,
                        soTienTruNo,
                        ngayGiaoDich,
                        "TruCoc",
                        $"Tru no vao coc. {ghiChu}".Trim())
                    : await congNoSettlementService.ThanhToanNoAsync(
                        conn,
                        tx,
                        hopDongId,
                        soTienTruNo,
                        ngayGiaoDich,
                        "TruCoc",
                        $"Tru no vao coc. {ghiChu}".Trim());

                if (daTatToan != soTienTruNo)
                    throw new InvalidOperationException("So tien tru no lon hon no hoa don con lai.");
            }

            await CapNhatDaXuLyChenhLechAsync(conn, tx, hopDongId);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<KetQuaXuLyChenhLechCoc> XuLyChenhLechChuyenPhongAsync(
        int hopDongId,
        DateTime ngayGiaoDich,
        string phuongThuc,
        string? ghiChu)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var hopDong = await LoadHopDongAsync(conn, tx, hopDongId)
                ?? throw new InvalidOperationException("Khong tim thay hop dong.");

            if (hopDong.HopDongTruocId == null)
                throw new InvalidOperationException("Chi xu ly chenh lech coc cho hop dong sinh tu chuyen phong.");

            if (ngayGiaoDich.Date < hopDong.NgayBatDau.Date || ngayGiaoDich.Date > DateTime.Today)
                throw new InvalidOperationException("Ngay giao dich phai tu ngay bat dau hop dong den ngay hien tai.");
            if (phuongThuc is not ("TienMat" or "ChuyenKhoan"))
                throw new InvalidOperationException("Xu ly chenh lech coc phai chon TienMat hoac ChuyenKhoan.");

            bool coLedger = await giaoDichCocRepo.HasAnyAsync(conn, tx, hopDongId);
            decimal soDuTruoc = coLedger
                ? await giaoDichCocRepo.GetSoDuAsync(conn, tx, hopDongId)
                : hopDong.TienCoc;
            decimal chenhLech = hopDong.TienCoc - soDuTruoc;
            string? loaiGiaoDich = null;

            if (chenhLech > 0)
            {
                loaiGiaoDich = "ThuThemCoc";
                await InsertDeltaAsync(
                    conn,
                    tx,
                    hopDongId,
                    loaiGiaoDich,
                    chenhLech,
                    ngayGiaoDich,
                    null,
                    phuongThuc,
                    $"Xu ly chenh lech coc chuyen phong. {ghiChu}".Trim());
            }
            else if (chenhLech < 0)
            {
                loaiGiaoDich = "HoanCoc";
                await InsertDeltaAsync(
                    conn,
                    tx,
                    hopDongId,
                    loaiGiaoDich,
                    chenhLech,
                    ngayGiaoDich,
                    null,
                    phuongThuc,
                    $"Xu ly chenh lech coc chuyen phong. {ghiChu}".Trim());
            }

            decimal soDuSau = coLedger || chenhLech != 0
                ? await giaoDichCocRepo.GetSoDuAsync(conn, tx, hopDongId)
                : soDuTruoc;

            bool daXuLy = soDuSau == hopDong.TienCoc;
            await conn.ExecuteAsync(
                "UPDATE HopDong SET DaXuLyChenhLechCoc = @DaXuLy WHERE Id = @Id",
                new { Id = hopDongId, DaXuLy = daXuLy },
                tx);

            await tx.CommitAsync();
            return new KetQuaXuLyChenhLechCoc(
                SoDuTruoc: soDuTruoc,
                SoDuSau: soDuSau,
                TienCocThoaThuan: hopDong.TienCoc,
                SoTienChenhLech: chenhLech,
                LoaiGiaoDich: loaiGiaoDich,
                DaXuLy: daXuLy);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task GhiNhanThuCocBanDauAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongId,
        decimal soTien,
        DateTime ngayGiaoDich,
        string? ghiChu)
    {
        if (soTien <= 0) return;

        await InsertDeltaAsync(conn, tx, hopDongId, "ThuCoc", soTien, ngayGiaoDich, null, null, ghiChu);
    }

    public async Task ChuyenCocSangHopDongMoiAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        HopDong hopDongCu,
        int hopDongMoiId,
        decimal tienCocMoi,
        DateTime ngayGiaoDich)
    {
        await EnsureOpeningBalanceAsync(conn, tx, hopDongCu);

        decimal soDuCu = await giaoDichCocRepo.GetSoDuAsync(conn, tx, hopDongCu.Id);
        if (soDuCu > 0)
        {
            await InsertDeltaAsync(
                conn,
                tx,
                hopDongCu.Id,
                "DieuChinh",
                -soDuCu,
                ngayGiaoDich,
                null,
                null,
                $"Chuyen coc sang hop dong #{hopDongMoiId}");

            await InsertDeltaAsync(
                conn,
                tx,
                hopDongMoiId,
                "DieuChinh",
                soDuCu,
                ngayGiaoDich,
                null,
                null,
                $"Nhan coc tu hop dong #{hopDongCu.Id}");
        }

        await conn.ExecuteAsync(
            "UPDATE HopDong SET DaXuLyChenhLechCoc = @DaXuLy WHERE Id = @Id",
            new { Id = hopDongMoiId, DaXuLy = soDuCu == tienCocMoi },
            tx);
    }

    public async Task<KetQuaTatToanCoc> TatToanCocKhiTraPhongAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        HopDong hopDong,
        int? hoaDonId,
        decimal tongNoTruocXuLyCoc,
        DateTime ngayGiaoDich,
        string? ghiChu)
    {
        await EnsureOpeningBalanceAsync(conn, tx, hopDong);

        decimal soDu = await giaoDichCocRepo.GetSoDuAsync(conn, tx, hopDong.Id);
        decimal soTienTruNo = Math.Min(soDu, Math.Max(0, tongNoTruocXuLyCoc));

        if (soTienTruNo > 0)
        {
            await InsertDeltaAsync(
                conn,
                tx,
                hopDong.Id,
                "TruNo",
                -soTienTruNo,
                ngayGiaoDich,
                hoaDonId,
                null,
                $"Tru no vao coc khi tra phong. {ghiChu}".Trim());
        }

        decimal soDuSauTruNo = soDu - soTienTruNo;
        decimal soTienHoanCoc = Math.Max(0, soDuSauTruNo);

        if (soTienHoanCoc > 0)
        {
            await InsertDeltaAsync(
                conn,
                tx,
                hopDong.Id,
                "HoanCoc",
                -soTienHoanCoc,
                ngayGiaoDich,
                null,
                null,
                $"Hoan coc khi tra phong. {ghiChu}".Trim());
        }

        return new KetQuaTatToanCoc(
            SoDuCocTruocXuLy: soDu,
            SoTienTruNo: soTienTruNo,
            SoTienHoanCoc: soTienHoanCoc,
            KhachConNoThem: Math.Max(0, tongNoTruocXuLyCoc - soTienTruNo));
    }

    private async Task EnsureOpeningBalanceAsync(MySqlConnection conn, MySqlTransaction tx, HopDong hopDong)
    {
        if (await giaoDichCocRepo.HasAnyAsync(conn, tx, hopDong.Id)) return;
        if (hopDong.TienCoc <= 0) return;

        await InsertDeltaAsync(
            conn,
            tx,
            hopDong.Id,
            "ThuCoc",
            hopDong.TienCoc,
            hopDong.NgayBatDau,
            null,
            null,
            "Khoi tao ledger tu HopDong.TienCoc");
    }

    private async Task InsertDeltaAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongId,
        string loaiGiaoDich,
        decimal delta,
        DateTime ngayGiaoDich,
        int? hoaDonId,
        string? phuongThuc,
        string? ghiChu)
    {
        if (delta == 0) return;

        var hopDongTonTai = await conn.ExecuteScalarAsync<int?>(
            "SELECT Id FROM HopDong WHERE Id = @HopDongId FOR UPDATE",
            new { HopDongId = hopDongId },
            transaction: tx);
        if (!hopDongTonTai.HasValue)
            throw new InvalidOperationException("Khong tim thay hop dong.");

        decimal soDuHienTai = await giaoDichCocRepo.GetSoDuAsync(conn, tx, hopDongId);
        decimal soDuMoi = soDuHienTai + delta;
        if (soDuMoi < 0)
            throw new InvalidOperationException("So du coc khong du de ghi nhan giao dich nay.");

        await giaoDichCocRepo.InsertAsync(conn, tx, new GiaoDichCoc
        {
            HopDongId = hopDongId,
            LoaiGiaoDich = loaiGiaoDich,
            SoTien = delta,
            SoDuSauGiaoDich = soDuMoi,
            NgayGiaoDich = ngayGiaoDich,
            HoaDonId = hoaDonId,
            PhuongThuc = phuongThuc,
            GhiChu = ghiChu
        });
    }

    private async Task CapNhatDaXuLyChenhLechAsync(MySqlConnection conn, MySqlTransaction tx, int hopDongId)
    {
        var hopDong = await LoadHopDongAsync(conn, tx, hopDongId);
        if (hopDong?.HopDongTruocId == null) return;

        decimal soDu = await giaoDichCocRepo.GetSoDuAsync(conn, tx, hopDongId);
        await conn.ExecuteAsync(
            "UPDATE HopDong SET DaXuLyChenhLechCoc = @DaXuLy WHERE Id = @Id",
            new { Id = hopDongId, DaXuLy = soDu == hopDong.TienCoc },
            tx);
    }

    private static decimal NormalizeDelta(string loaiGiaoDich, decimal soTien)
    {
        if (soTien == 0)
            throw new InvalidOperationException("So tien giao dich coc phai khac 0.");

        var amount = Math.Abs(soTien);
        return loaiGiaoDich switch
        {
            "ThuCoc" or "ThuThemCoc" => amount,
            "HoanCoc" or "TruNo" => -amount,
            "DieuChinh" => soTien,
            _ => throw new InvalidOperationException("Loai giao dich coc khong hop le.")
        };
    }

    private static void ValidateGiaoDichThuCong(
        HopDong hopDong,
        string loaiGiaoDich,
        DateTime ngayGiaoDich,
        int? hoaDonId,
        string? phuongThuc)
    {
        if (loaiGiaoDich is not ("ThuThemCoc" or "HoanCoc" or "TruNo"))
            throw new InvalidOperationException("Loai giao dich nay chi duoc tao boi flow noi bo.");

        if (ngayGiaoDich.Date < hopDong.NgayBatDau.Date || ngayGiaoDich.Date > DateTime.Today)
            throw new InvalidOperationException("Ngay giao dich phai tu ngay bat dau hop dong den ngay hien tai.");

        if (loaiGiaoDich == "TruNo")
        {
            if (!hoaDonId.HasValue)
                throw new InvalidOperationException("Tru no bat buoc chon hoa don cung hop dong.");
            if (phuongThuc != null)
                throw new InvalidOperationException("Tru no tu coc khong dung phuong thuc thu/hoan tien.");
        }
        else
        {
            if (hoaDonId.HasValue)
                throw new InvalidOperationException("Chi giao dich TruNo moi duoc lien ket hoa don.");
            if (phuongThuc is not ("TienMat" or "ChuyenKhoan"))
                throw new InvalidOperationException("Thu/hoan coc phai chon TienMat hoac ChuyenKhoan.");
        }
    }

    private static async Task<HopDong?> LoadHopDongAsync(MySqlConnection conn, MySqlTransaction tx, int hopDongId)
        => await conn.QueryFirstOrDefaultAsync<HopDong>(
            "SELECT * FROM HopDong WHERE Id = @Id FOR UPDATE",
            new { Id = hopDongId },
            tx);
}

public sealed record KetQuaTatToanCoc(
    decimal SoDuCocTruocXuLy,
    decimal SoTienTruNo,
    decimal SoTienHoanCoc,
    decimal KhachConNoThem);

public sealed record KetQuaXuLyChenhLechCoc(
    decimal SoDuTruoc,
    decimal SoDuSau,
    decimal TienCocThoaThuan,
    decimal SoTienChenhLech,
    string? LoaiGiaoDich,
    bool DaXuLy);
