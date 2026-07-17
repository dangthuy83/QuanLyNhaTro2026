namespace QuanLyNhaTro.Models;

public sealed class DotMoSo
{
    public int Id { get; set; }
    public DateTime NgayChot { get; set; }
    public string TenNguon { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string NguoiDuyet { get; set; } = "";
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }
}

public sealed class MoSoHopDongRequest
{
    public int DotMoSoId { get; set; }
    public string NguonThamChieu { get; set; } = "";
    public HopDong HopDong { get; set; } = new();
    public int[] KhachThueIds { get; set; } = [];
    public int KhachDaiDienId { get; set; }
    public int[] PhongDichVuIds { get; set; } = [];
    public List<DichVuMoSoInput> DichVu { get; set; } = [];
    public decimal SoDuCocThucTe { get; set; }
    public List<CongNoMoSoInput> CongNo { get; set; } = [];
    public List<ChiSoMoSoInput> ChiSo { get; set; } = [];
    public List<CuTruMoSoInput> CuTru { get; set; } = [];
    public string SoDuCocNguonThamChieu { get; set; } = "";
}

public sealed class DichVuMoSoInput
{
    public int PhongDichVuId { get; set; }
    public string NguonThamChieu { get; set; } = "";
}

public sealed class MoSoImportBatch
{
    public DotMoSo DotMoSo { get; set; } = new();
    public List<MoSoHopDongRequest> HopDong { get; set; } = [];
}

public sealed class CuTruMoSoInput
{
    public int KhachThueId { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime? NgayKetThucDuKien { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public bool LaDaiDien { get; set; }
    public string NguonThamChieu { get; set; } = "";
}

public sealed class CongNoMoSoInput
{
    public decimal SoTien { get; set; }
    public int DenKyThang { get; set; }
    public int DenKyNam { get; set; }
    public string MaChungTu { get; set; } = "";
    public string NguonThamChieu { get; set; } = "";
}

public sealed class ChiSoMoSoInput
{
    public int DichVuId { get; set; }
    public decimal ChiSo { get; set; }
    public string NguonThamChieu { get; set; } = "";
}
