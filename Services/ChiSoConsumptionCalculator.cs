using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Services;

public static class ChiSoConsumptionCalculator
{
    public static decimal Calculate(ChiSoDienNuoc chiSo)
    {
        if (chiSo.LoaiGhiNhan == ChiSoDienNuoc.LoaiBinhThuong)
        {
            if (chiSo.ChiSoCuoi < chiSo.ChiSoDau)
            {
                throw BuildInvalidChiSoException(
                    chiSo,
                    $"chi so cuoi {chiSo.ChiSoCuoi:N2} nho hon chi so dau {chiSo.ChiSoDau:N2}");
            }

            return chiSo.ChiSoCuoi - chiSo.ChiSoDau;
        }

        if (chiSo.LoaiGhiNhan == ChiSoDienNuoc.LoaiReset)
        {
            if (!chiSo.ChiSoTruocReset.HasValue)
            {
                throw BuildInvalidChiSoException(chiSo, "thieu chi so truoc reset");
            }

            if (chiSo.ChiSoTruocReset.Value < chiSo.ChiSoDau)
            {
                throw BuildInvalidChiSoException(
                    chiSo,
                    $"chi so truoc reset {chiSo.ChiSoTruocReset.Value:N2} nho hon chi so dau {chiSo.ChiSoDau:N2}");
            }

            var chiSoSauReset = chiSo.ChiSoSauReset ?? 0;
            if (chiSoSauReset < 0)
            {
                throw BuildInvalidChiSoException(chiSo, "chi so sau reset khong duoc am");
            }

            if (chiSo.ChiSoCuoi < chiSoSauReset)
            {
                throw BuildInvalidChiSoException(
                    chiSo,
                    $"chi so cuoi {chiSo.ChiSoCuoi:N2} nho hon chi so sau reset {chiSoSauReset:N2}");
            }

            return (chiSo.ChiSoTruocReset.Value - chiSo.ChiSoDau) + (chiSo.ChiSoCuoi - chiSoSauReset);
        }

        throw BuildInvalidChiSoException(chiSo, $"loai ghi nhan khong hop le '{chiSo.LoaiGhiNhan}'");
    }

    private static InvalidOperationException BuildInvalidChiSoException(ChiSoDienNuoc chiSo, string message)
    {
        var tenDichVu = chiSo.DichVu?.TenDichVu ?? $"Dich vu #{chiSo.DichVuId}";
        return new InvalidOperationException(
            $"{tenDichVu} phong #{chiSo.PhongId} ky {chiSo.Thang}/{chiSo.Nam}: {message}.");
    }
}
