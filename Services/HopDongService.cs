using System.Data;
using MySqlConnector;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public class HopDongService(
    IDbConnection db,
    HopDongRepository hopDongRepo,
    HopDongKhachThueRepository hdKhachRepo,
    PhongRepository phongRepo)
{
    public async Task<int> TaoHopDongAsync(HopDong hopDong, int[] khachThueIds, int? khachChinhId)
    {
        hopDong.TrangThai = "DangHieuLuc";

        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var hopDongDangHieuLuc = await hopDongRepo.GetDangHieuLucByPhongAsync(conn, tx, hopDong.PhongId);
            if (hopDongDangHieuLuc != null)
                throw new InvalidOperationException($"Phong #{hopDong.PhongId} da co hop dong dang hieu luc.");

            var hopDongId = await hopDongRepo.InsertAsync(conn, tx, hopDong);
            await CapNhatKhachThueAsync(conn, tx, hopDongId, khachThueIds, khachChinhId);
            await phongRepo.UpdateTrangThaiAsync(conn, tx, hopDong.PhongId, "DangThue");

            await tx.CommitAsync();
            return hopDongId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task SuaHopDongAsync(HopDong hopDong, int[] khachThueIds, int? khachChinhId)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await hopDongRepo.UpdateAsync(conn, tx, hopDong);
            await hdKhachRepo.DeleteByHopDongAsync(conn, tx, hopDong.Id);
            await CapNhatKhachThueAsync(conn, tx, hopDong.Id, khachThueIds, khachChinhId);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task CapNhatKhachThueAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int hopDongId,
        int[] khachThueIds,
        int? khachChinhId)
    {
        foreach (var khachId in khachThueIds.Distinct())
        {
            await hdKhachRepo.InsertAsync(conn, tx, hopDongId, khachId, khachId == khachChinhId);
        }
    }
}
