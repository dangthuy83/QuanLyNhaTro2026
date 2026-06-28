using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

/// <summary>Nghiệp vụ liên quan đến Phong: kiểm tra trạng thái, cập nhật khi ký/kết thúc hợp đồng.</summary>
public class PhongService(
    PhongRepository phongRepo,
    HopDongRepository hopDongRepo)
{
    /// <summary>Sau khi ký hợp đồng mới → cập nhật phòng sang DangThue.</summary>
    public async Task XuLyKyHopDongAsync(int phongId)
        => await phongRepo.UpdateTrangThaiAsync(phongId, "DangThue");

    /// <summary>
    /// Sau khi kết thúc / huỷ hợp đồng:
    /// kiểm tra không còn hợp đồng DangHieuLuc nào thì chuyển phòng về Trong.
    /// </summary>
    public async Task XuLyKetThucHopDongAsync(int phongId)
    {
        var con = await hopDongRepo.GetDangHieuLucByPhongAsync(phongId);
        if (con == null)
            await phongRepo.UpdateTrangThaiAsync(phongId, "Trong");
    }

    /// <summary>Tính tổng nợ còn lại của hợp đồng (dùng khi trả phòng, hoàn cọc).</summary>
    public static decimal TinhTienHoanCoc(HopDong hopDong, decimal tongNoCuoiKy)
        => hopDong.TienCoc - tongNoCuoiKy;
}
