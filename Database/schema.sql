-- ============================================================
-- SCHEMA DATABASE: QUẢN LÝ NHÀ TRỌ
-- MySQL 8.x / InnoDB / utf8mb4
-- ============================================================

CREATE DATABASE IF NOT EXISTS QuanLyNhaTro
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE QuanLyNhaTro;

-- ============================================================
-- 1. NHA — Nhà trọ (bảng gốc, dù chỉ quản lý 1 nhà vẫn nên giữ
--    để dễ mở rộng sau này mà không phải sửa cấu trúc)
-- ============================================================
CREATE TABLE Nha (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    TenNha VARCHAR(255) NOT NULL,
    DiaChi VARCHAR(500) NULL,
    GhiChu TEXT NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 2. PHONG — Phòng trọ
-- TrangThai: Trong | DangThue | DangSua
-- (Dùng VARCHAR thay ENUM để thêm trạng thái mới không cần ALTER TABLE)
-- ============================================================
CREATE TABLE Phong (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    NhaId INT NOT NULL,
    TenPhong VARCHAR(50) NOT NULL,
    DienTich DECIMAL(6,2) NULL,
    GiaThueMacDinh DECIMAL(12,0) NOT NULL DEFAULT 0,
    TrangThai VARCHAR(20) NOT NULL DEFAULT 'Trong',
    GhiChu TEXT NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Phong_Nha FOREIGN KEY (NhaId) REFERENCES Nha(Id),
    CONSTRAINT UQ_Phong_TenPhong UNIQUE (NhaId, TenPhong),
    CONSTRAINT CK_Phong_GiaThue CHECK (GiaThueMacDinh >= 0),
    CONSTRAINT CK_Phong_TrangThai CHECK (TrangThai IN ('Trong', 'DangThue', 'DangSuaChua'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 3. KHACHTHUE — Hồ sơ khách thuê
-- CCCD KHÔNG đặt UNIQUE: khách có thể thuê - trả - thuê lại,
-- kiểm tra trùng nên xử lý ở Controller (cảnh báo) thay vì chặn cứng ở DB.
-- ============================================================
CREATE TABLE KhachThue (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HoTen VARCHAR(255) NOT NULL,
    CCCD VARCHAR(20) NULL,
    CCCDNormalized VARCHAR(20) GENERATED ALWAYS AS (NULLIF(TRIM(CCCD), '')) STORED,
    SoDienThoai VARCHAR(20) NULL,
    NgaySinh DATE NULL,
    NgayCapCCCD DATE NULL,
    NgheNghiep VARCHAR(150) NULL,
    LoaiXe VARCHAR(50) NULL,
    BienSoXe VARCHAR(30) NULL,
    QueQuan VARCHAR(255) NULL,
    AnhCCCDMatTruoc VARCHAR(500) NULL,
    AnhCCCDMatSau VARCHAR(500) NULL,
    GhiChu TEXT NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_KhachThue_CCCD_Normalized UNIQUE (CCCDNormalized)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 4. DICHVU — Danh mục loại dịch vụ (Điện, Nước, Internet, Vệ sinh...)
-- LoaiTinhPhi: CoDinh | TheoChiSo
-- CachTinhCoDinh: TheoPhong | TheoNguoi, only applies when LoaiTinhPhi = CoDinh.
-- Đây là "công tắc" quyết định cách tính tiền dịch vụ khi lập hóa đơn.
-- Muốn đổi Nước từ CoDinh sang TheoChiSo sau này: chỉ cần UPDATE dữ liệu
-- dòng này, KHÔNG cần sửa schema hay code, miễn Controller luôn rẽ nhánh
-- theo LoaiTinhPhi (không hard-code so sánh tên dịch vụ).
-- ============================================================
CREATE TABLE DichVu (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    TenDichVu VARCHAR(100) NOT NULL,
    LoaiTinhPhi VARCHAR(20) NOT NULL,
    CachTinhCoDinh VARCHAR(20) NOT NULL DEFAULT 'TheoPhong',
    DonGiaMacDinh DECIMAL(12,2) NOT NULL DEFAULT 0,
    BatBuocKhiThue BIT NOT NULL DEFAULT 0,
    DonViTinh VARCHAR(20) NULL,
    CONSTRAINT CK_DichVu_DonGia CHECK (DonGiaMacDinh >= 0),
    CONSTRAINT CK_DichVu_LoaiTinhPhi CHECK (LoaiTinhPhi IN ('CoDinh', 'TheoChiSo')),
    CONSTRAINT CK_DichVu_CachTinhCoDinh CHECK (CachTinhCoDinh IN ('TheoPhong', 'TheoNguoi'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Lich su hinh thuc tinh cua dich vu theo ky su dung. KyApDung luon la ngay dau thang.
-- Gia tri tren DichVu la baseline khi chua co lich su; moi ky resolve tu lich su gan nhat.
CREATE TABLE LichSuHinhThucDichVu (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    DichVuId INT NOT NULL,
    LoaiTinhPhiCu VARCHAR(20) NOT NULL,
    CachTinhCoDinhCu VARCHAR(20) NOT NULL,
    LoaiTinhPhiMoi VARCHAR(20) NOT NULL,
    CachTinhCoDinhMoi VARCHAR(20) NOT NULL,
    KyApDung DATE NOT NULL,
    LyDo VARCHAR(500) NOT NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_LichSuHinhThuc_DichVu FOREIGN KEY (DichVuId) REFERENCES DichVu(Id),
    CONSTRAINT UQ_LichSuHinhThuc_DichVuKy UNIQUE (DichVuId, KyApDung),
    CONSTRAINT CK_LichSuHinhThuc_Ky CHECK (DAY(KyApDung) = 1),
    CONSTRAINT CK_LichSuHinhThuc_Nam CHECK (YEAR(KyApDung) BETWEEN 2000 AND 2100),
    CONSTRAINT CK_LichSuHinhThuc_LoaiCu CHECK (LoaiTinhPhiCu IN ('CoDinh', 'TheoChiSo')),
    CONSTRAINT CK_LichSuHinhThuc_LoaiMoi CHECK (LoaiTinhPhiMoi IN ('CoDinh', 'TheoChiSo')),
    CONSTRAINT CK_LichSuHinhThuc_CachCu CHECK (CachTinhCoDinhCu IN ('TheoPhong', 'TheoNguoi')),
    CONSTRAINT CK_LichSuHinhThuc_CachMoi CHECK (CachTinhCoDinhMoi IN ('TheoPhong', 'TheoNguoi'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_LichSuHinhThuc_DichVuKy
    ON LichSuHinhThucDichVu(DichVuId, KyApDung);

-- Chi so dau bat buoc khi dich vu chuyen tu CoDinh sang TheoChiSo.
CREATE TABLE ChiSoDauChuyenDoiDichVu (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    LichSuHinhThucDichVuId INT NOT NULL,
    PhongId INT NOT NULL,
    ChiSoDau DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_ChiSoDauChuyenDoi_LichSu FOREIGN KEY (LichSuHinhThucDichVuId) REFERENCES LichSuHinhThucDichVu(Id),
    CONSTRAINT FK_ChiSoDauChuyenDoi_Phong FOREIGN KEY (PhongId) REFERENCES Phong(Id),
    CONSTRAINT UQ_ChiSoDauChuyenDoi_LichSuPhong UNIQUE (LichSuHinhThucDichVuId, PhongId),
    CONSTRAINT CK_ChiSoDauChuyenDoi_KhongAm CHECK (ChiSoDau >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 5. PHONGDICHVU — Giá dịch vụ áp dụng riêng cho từng phòng
-- ============================================================
CREATE TABLE PhongDichVu (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PhongId INT NOT NULL,
    DichVuId INT NOT NULL,
    DonGia DECIMAL(12,2) NOT NULL,
    DangApDung BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_PhongDichVu_Phong FOREIGN KEY (PhongId) REFERENCES Phong(Id),
    CONSTRAINT FK_PhongDichVu_DichVu FOREIGN KEY (DichVuId) REFERENCES DichVu(Id),
    CONSTRAINT UQ_Phong_DichVu UNIQUE (PhongId, DichVuId),
    CONSTRAINT CK_PhongDichVu_DonGia CHECK (DonGia >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 6. HOPDONG — Hợp đồng thuê
-- TrangThai: ChoHieuLuc | DangHieuLuc | DaKetThuc | DaHuy | DaChuyenPhong
-- HopDongTruocId: dùng khi khách CHUYỂN PHÒNG (không phải trả phòng thật) —
--   liên kết hợp đồng mới với hợp đồng cũ để xử lý bù trừ tiền cọc và
--   tính nợ kỳ trước xuyên hợp đồng.
-- DaXuLyChenhLechCoc: cờ đánh dấu đã thu/hoàn phần chênh lệch cọc khi
--   chuyển phòng hay chưa (tránh quên xử lý).
-- ============================================================
CREATE TABLE HopDong (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PhongId INT NOT NULL,
    HopDongTruocId INT NULL,
    NgayBatDau DATE NOT NULL,
    NgayKetThuc DATE NULL,
    TienThueThoaThuan DECIMAL(12,0) NOT NULL,
    TienCoc DECIMAL(12,0) NOT NULL DEFAULT 0,
    NgayThanhToanHangThang TINYINT NOT NULL DEFAULT 5,
    TrangThai VARCHAR(20) NOT NULL DEFAULT 'DangHieuLuc',
    NgayTraPhongThucTe DATE NULL,
    TienCocHoanLai DECIMAL(12,0) NULL,
    DaXuLyChenhLechCoc BIT NOT NULL DEFAULT 0,
    GhiChu TEXT NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_HopDong_Phong FOREIGN KEY (PhongId) REFERENCES Phong(Id),
    CONSTRAINT FK_HopDong_Truoc FOREIGN KEY (HopDongTruocId) REFERENCES HopDong(Id),
    CONSTRAINT CK_HopDong_KhoangNgay CHECK (NgayKetThuc IS NULL OR NgayKetThuc >= NgayBatDau),
    CONSTRAINT CK_HopDong_TrangThai CHECK (TrangThai IN ('ChoHieuLuc', 'DangHieuLuc', 'DaKetThuc', 'DaHuy', 'DaChuyenPhong')),
    CONSTRAINT CK_HopDong_NamNghiepVu CHECK (
        YEAR(NgayBatDau) BETWEEN 2000 AND 2100
        AND (NgayKetThuc IS NULL OR YEAR(NgayKetThuc) BETWEEN 2000 AND 2100)
        AND (NgayTraPhongThucTe IS NULL OR (
            YEAR(NgayTraPhongThucTe) BETWEEN 2000 AND 2100
            AND NgayTraPhongThucTe >= NgayBatDau
        ))
    ),
    CONSTRAINT CK_HopDong_Tien CHECK (
        TienThueThoaThuan > 0
        AND TienCoc >= 0
        AND (TienCocHoanLai IS NULL OR TienCocHoanLai >= 0)
    ),
    CONSTRAINT CK_HopDong_NgayThanhToan CHECK (NgayThanhToanHangThang BETWEEN 1 AND 31)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_HopDong_Phong_KhoangNgay
    ON HopDong(PhongId, NgayBatDau, NgayKetThuc, TrangThai);

-- ============================================================
-- 7. HOPDONGKHACHTHUE — Liên kết nhiều-nhiều Hợp đồng <-> Khách thuê
-- LaDaiDien: 1 = người đại diện ký hợp đồng, 0 = người ở thêm
-- ============================================================
CREATE TABLE HopDongKhachThue (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HopDongId INT NOT NULL,
    KhachThueId INT NOT NULL,
    NgayBatDau DATE NOT NULL,
    NgayKetThucDuKien DATE NULL,
    NgayKetThuc DATE NULL,
    LaDaiDien BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_HDKT_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    CONSTRAINT FK_HDKT_KhachThue FOREIGN KEY (KhachThueId) REFERENCES KhachThue(Id),
    CONSTRAINT CK_HDKT_KhoangNgay CHECK (
        (NgayKetThucDuKien IS NULL OR NgayKetThucDuKien >= NgayBatDau)
        AND (NgayKetThuc IS NULL OR NgayKetThuc >= NgayBatDau)
    ),
    CONSTRAINT CK_HDKT_NamNghiepVu CHECK (
        YEAR(NgayBatDau) BETWEEN 2000 AND 2100
        AND (NgayKetThucDuKien IS NULL OR YEAR(NgayKetThucDuKien) BETWEEN 2000 AND 2100)
        AND (NgayKetThuc IS NULL OR YEAR(NgayKetThuc) BETWEEN 2000 AND 2100)
    ),
    CONSTRAINT UQ_HDKT_GiaiDoan UNIQUE (HopDongId, KhachThueId, NgayBatDau)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_HDKT_HopDong_HieuLuc
    ON HopDongKhachThue(HopDongId, NgayBatDau, NgayKetThuc, KhachThueId);
CREATE INDEX IX_HDKT_Khach_HieuLuc
    ON HopDongKhachThue(KhachThueId, NgayBatDau, NgayKetThuc, HopDongId);

-- ============================================================
-- 8. HOPDONGDICHVU — Dịch vụ đăng ký theo hợp đồng và kỳ sử dụng
-- KyKetThuc là mốc loại trừ: dòng có hiệu lực khi
-- KyBatDau <= Ky và (KyKetThuc IS NULL hoặc Ky < KyKetThuc).
-- Đơn giá vẫn nằm ở PhongDichVu và LichSuThayDoiGia.
-- ============================================================
CREATE TABLE HopDongDichVu (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HopDongId INT NOT NULL,
    PhongDichVuId INT NOT NULL,
    KyBatDau DATE NOT NULL,
    KyKetThuc DATE NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_HopDongDichVu_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    CONSTRAINT FK_HopDongDichVu_PhongDichVu FOREIGN KEY (PhongDichVuId) REFERENCES PhongDichVu(Id),
    CONSTRAINT CK_HopDongDichVu_Ky CHECK (
        DAY(KyBatDau) = 1
        AND (KyKetThuc IS NULL OR (DAY(KyKetThuc) = 1 AND KyKetThuc > KyBatDau))
    ),
    CONSTRAINT CK_HopDongDichVu_Nam CHECK (
        YEAR(KyBatDau) BETWEEN 2000 AND 2100
        AND (KyKetThuc IS NULL OR YEAR(KyKetThuc) BETWEEN 2000 AND 2100)
    ),
    CONSTRAINT UQ_HopDongDichVu_Ky UNIQUE (HopDongId, PhongDichVuId, KyBatDau)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_HopDongDichVu_Ky
    ON HopDongDichVu(HopDongId, KyBatDau, KyKetThuc);

-- ============================================================
-- 8. CHISODIENNUOC — Chỉ số đọc theo tháng cho dịch vụ tính theo chỉ số
-- Bảng này tổng quát: tham chiếu DichVuId nên dùng được cho BẤT KỲ dịch vụ
-- nào có LoaiTinhPhi = TheoChiSo, không chỉ riêng Điện/Nước.
-- UNIQUE (PhongId, DichVuId, Thang, Nam) chặn nhập trùng chỉ số 2 lần
-- cho cùng phòng/dịch vụ/tháng — lỗi rất hay gặp khi làm bằng Excel.
-- ============================================================
CREATE TABLE ChiSoDienNuoc (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HopDongId INT NULL,
    PhongId INT NOT NULL,
    DichVuId INT NOT NULL,
    Thang TINYINT NOT NULL,
    Nam SMALLINT NOT NULL,
    ChiSoDau DECIMAL(10,2) NOT NULL,
    ChiSoCuoi DECIMAL(10,2) NOT NULL,
    LoaiGhiNhan VARCHAR(20) NOT NULL DEFAULT 'BinhThuong',
    ChiSoTruocReset DECIMAL(10,2) NULL,
    ChiSoSauReset DECIMAL(10,2) NULL,
    LyDoDieuChinh VARCHAR(255) NULL,
    NgayDoc DATE NULL,
    GhiChu VARCHAR(255) NULL,
    ChiSoScopeKey INT GENERATED ALWAYS AS (COALESCE(HopDongId, 0)) STORED,
    CONSTRAINT FK_ChiSo_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    CONSTRAINT FK_ChiSo_Phong FOREIGN KEY (PhongId) REFERENCES Phong(Id),
    CONSTRAINT FK_ChiSo_DichVu FOREIGN KEY (DichVuId) REFERENCES DichVu(Id),
    CONSTRAINT CK_ChiSo_LoaiGhiNhan CHECK (LoaiGhiNhan IN ('BinhThuong', 'Reset')),
    CONSTRAINT CK_ChiSo_Ky CHECK (Thang BETWEEN 1 AND 12 AND Nam BETWEEN 2000 AND 2100),
    CONSTRAINT CK_ChiSo_NgayDoc CHECK (
        NgayDoc IS NOT NULL
        AND YEAR(NgayDoc) BETWEEN 2000 AND 2100
        AND MONTH(NgayDoc) = Thang
        AND YEAR(NgayDoc) = Nam
    ),
    CONSTRAINT CK_ChiSo_KhongAmTrenDongHo CHECK (
        ChiSoDau >= 0
        AND ChiSoCuoi >= 0
        AND (ChiSoTruocReset IS NULL OR ChiSoTruocReset >= 0)
        AND (ChiSoSauReset IS NULL OR ChiSoSauReset >= 0)
    ),
    CONSTRAINT CK_ChiSo_SanLuongHopLe CHECK (
        (
            LoaiGhiNhan = 'BinhThuong'
            AND ChiSoCuoi >= ChiSoDau
            AND ChiSoTruocReset IS NULL
            AND ChiSoSauReset IS NULL
        )
        OR
        (
            LoaiGhiNhan = 'Reset'
            AND ChiSoTruocReset IS NOT NULL
            AND ChiSoTruocReset >= ChiSoDau
            AND ChiSoCuoi >= COALESCE(ChiSoSauReset, 0)
        )
    ),
    CONSTRAINT UQ_ChiSo UNIQUE (PhongId, DichVuId, Thang, Nam, ChiSoScopeKey)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_ChiSo_HopDong_Ky
    ON ChiSoDienNuoc(HopDongId, Nam, Thang);

CREATE INDEX IX_ChiSo_Phong_FK
    ON ChiSoDienNuoc(PhongId);

CREATE INDEX IX_ChiSo_DichVu_FK
    ON ChiSoDienNuoc(DichVuId);

-- ============================================================
-- 8.1. CHISONGOAIHOPDONG - chỉ số phát sinh khi phòng không gắn hợp đồng
-- Dùng cho các đoạn điện/nước chủ nhà dùng khi phòng trống, sửa phòng,
-- bàn giao lại phòng... Không tính vào hóa đơn khách thuê.
-- DenChiSo mới nhất có thể làm mốc ChiSoDau cho hợp đồng/kỳ sau.
-- ============================================================
CREATE TABLE ChiSoNgoaiHopDong (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PhongId INT NOT NULL,
    DichVuId INT NOT NULL,
    TuChiSo DECIMAL(10,2) NOT NULL,
    DenChiSo DECIMAL(10,2) NOT NULL,
    LoaiGhiNhan VARCHAR(20) NOT NULL DEFAULT 'BinhThuong',
    ChiSoTruocReset DECIMAL(10,2) NULL,
    ChiSoSauReset DECIMAL(10,2) NULL,
    LyDoDieuChinh VARCHAR(255) NULL,
    SanLuong DECIMAL(10,2) GENERATED ALWAYS AS (
        CASE
            WHEN LoaiGhiNhan = 'Reset' THEN
                (ChiSoTruocReset - TuChiSo) + (DenChiSo - COALESCE(ChiSoSauReset, 0))
            ELSE DenChiSo - TuChiSo
        END
    ) STORED,
    NgayGhiNhan DATE NOT NULL,
    LyDo VARCHAR(255) NOT NULL,
    GhiChu VARCHAR(255) NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_ChiSoNgoaiHopDong_Phong FOREIGN KEY (PhongId) REFERENCES Phong(Id),
    CONSTRAINT FK_ChiSoNgoaiHopDong_DichVu FOREIGN KEY (DichVuId) REFERENCES DichVu(Id),
    CONSTRAINT CK_ChiSoNgoaiHopDong_Nam CHECK (YEAR(NgayGhiNhan) BETWEEN 2000 AND 2100),
    CONSTRAINT CK_ChiSoNgoaiHopDong_Loai CHECK (LoaiGhiNhan IN ('BinhThuong', 'Reset')),
    CONSTRAINT CK_ChiSoNgoaiHopDong_KhongAm CHECK (
        TuChiSo >= 0
        AND DenChiSo >= 0
        AND (ChiSoTruocReset IS NULL OR ChiSoTruocReset >= 0)
        AND (ChiSoSauReset IS NULL OR ChiSoSauReset >= 0)
    ),
    CONSTRAINT CK_ChiSoNgoaiHopDong_SanLuongHopLe CHECK (
        (
            LoaiGhiNhan = 'BinhThuong'
            AND DenChiSo >= TuChiSo
            AND ChiSoTruocReset IS NULL
            AND ChiSoSauReset IS NULL
            AND LyDoDieuChinh IS NULL
        )
        OR
        (
            LoaiGhiNhan = 'Reset'
            AND ChiSoTruocReset IS NOT NULL
            AND ChiSoTruocReset >= TuChiSo
            AND DenChiSo >= COALESCE(ChiSoSauReset, 0)
            AND LyDoDieuChinh IS NOT NULL
            AND TRIM(LyDoDieuChinh) <> ''
        )
    )
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_ChiSoNgoaiHopDong_Moc
    ON ChiSoNgoaiHopDong(PhongId, DichVuId, NgayGhiNhan, Id);

-- ============================================================
-- 9. HOADON — Hóa đơn hàng tháng theo hợp đồng
--
-- HoaDonGhepId: dùng khi tháng đó khách CHUYỂN PHÒNG, sinh 2 hóa đơn riêng
--   (1 cho phòng cũ, 1 cho phòng mới) nhưng muốn gộp lại làm 1 phiếu thu
--   duy nhất cho khách. Query gộp: WHERE Id = X OR HoaDonGhepId = X.
--
-- SoNgayO / SoNgayTrongThang: căn cứ tính tiền phòng khi hợp đồng KHÔNG
--   trọn tháng (dọn vào/trả phòng/chuyển phòng giữa tháng). NULL = tính
--   trọn tháng (trường hợp bình thường, chiếm đa số).
--   Công thức: TienPhong = ROUND(TienThueThoaThuan / SoNgayTrongThang * SoNgayO)
--
-- TienNoKyTruoc: SNAPSHOT số nợ còn lại của hóa đơn tháng trước (tính lúc
--   lập hóa đơn, KHÔNG tính động) để tránh sai lệch nếu sau này sửa hóa đơn cũ.
--   Nếu hợp đồng này có HopDongTruocId (vừa chuyển phòng tới) và đây là hóa
--   đơn đầu tiên, lấy nợ từ hóa đơn cuối cùng của HopDongTruocId.
--
-- TongCong = TienPhong + TongTienDichVu + TienNoKyTruoc
-- (Nếu khách trả dư, TienNoKyTruoc có thể âm = tiền ứng trước, công thức tự xử lý)
--
-- TrangThaiThanhToan: ChuaThu (SoTienDaThu=0) | ThuMotPhan (0<SoTienDaThu<TongCong)
--   | DaThu (SoTienDaThu>=TongCong)
-- SoTienDaThu là cột tổng hợp (denormalized) từ bảng ThanhToan, PHẢI cập nhật
--   cùng transaction mỗi khi insert vào ThanhToan.
-- NgayDenHan: snapshot ngày thanh toán của hợp đồng lúc chốt hóa đơn. Hóa đơn
--   kỳ N đến hạn trong tháng N+1; ngày 29-31 được chặn về ngày cuối tháng.
-- ============================================================
CREATE TABLE HoaDon (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HopDongId INT NOT NULL,
    HoaDonGhepId INT NULL,
    Thang TINYINT NOT NULL,
    Nam SMALLINT NOT NULL,
    TienPhong DECIMAL(12,0) NOT NULL,
    SoNgayO TINYINT NULL,
    SoNgayTrongThang TINYINT NULL,
    TongTienDichVu DECIMAL(12,0) NOT NULL DEFAULT 0,
    TongTienPhatSinh DECIMAL(12,0) NOT NULL DEFAULT 0,
    TienNoKyTruoc DECIMAL(12,0) NOT NULL DEFAULT 0,
    TongCong DECIMAL(12,0) NOT NULL DEFAULT 0,
    SoTienDaThu DECIMAL(12,0) NOT NULL DEFAULT 0,
    TrangThaiThanhToan VARCHAR(20) NOT NULL DEFAULT 'ChuaThu',
    NgayLap DATE NOT NULL,
    NgayDenHan DATE NOT NULL,
    NgayThuThucTe DATE NULL,
    GhiChu TEXT NULL,
    NhaIdSnapshot INT NOT NULL,
    TenNhaSnapshot VARCHAR(100) NOT NULL,
    PhongIdSnapshot INT NOT NULL,
    TenPhongSnapshot VARCHAR(50) NOT NULL,
    KhachDaiDienIdSnapshot INT NOT NULL,
    TenKhachDaiDienSnapshot VARCHAR(100) NOT NULL,
    CccdKhachDaiDienSnapshot VARCHAR(20) NULL,
    CONSTRAINT FK_HoaDon_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    CONSTRAINT FK_HoaDon_Ghep FOREIGN KEY (HoaDonGhepId) REFERENCES HoaDon(Id),
    CONSTRAINT UQ_HoaDon UNIQUE (HopDongId, Thang, Nam),
    CONSTRAINT CK_HoaDon_Ky CHECK (Thang BETWEEN 1 AND 12 AND Nam BETWEEN 2000 AND 2100),
    CONSTRAINT CK_HoaDon_NgayNghiepVu CHECK (
        YEAR(NgayLap) BETWEEN 2000 AND 2100
        AND (NgayThuThucTe IS NULL OR YEAR(NgayThuThucTe) BETWEEN 2000 AND 2100)
    ),
    CONSTRAINT CK_HoaDon_NgayDenHan CHECK (
        YEAR(NgayDenHan) BETWEEN 2000 AND 2101
        AND NgayDenHan BETWEEN
            DATE_ADD(STR_TO_DATE(CONCAT(Nam, '-', LPAD(Thang, 2, '0'), '-01'), '%Y-%m-%d'), INTERVAL 1 MONTH)
            AND LAST_DAY(DATE_ADD(STR_TO_DATE(CONCAT(Nam, '-', LPAD(Thang, 2, '0'), '-01'), '%Y-%m-%d'), INTERVAL 1 MONTH))
    ),
    CONSTRAINT CK_HoaDon_Tien CHECK (
        TienPhong >= 0
        AND TongTienDichVu >= 0
        AND TongTienPhatSinh >= 0
        AND TienNoKyTruoc >= 0
        AND TongCong >= 0
        AND SoTienDaThu >= 0
        AND SoTienDaThu <= TongCong
        AND TongCong = TienPhong + TongTienDichVu + TongTienPhatSinh + TienNoKyTruoc
    ),
    CONSTRAINT CK_HoaDon_TrangThai CHECK (
        (SoTienDaThu = 0 AND TrangThaiThanhToan = 'ChuaThu')
        OR (SoTienDaThu > 0 AND SoTienDaThu < TongCong AND TrangThaiThanhToan = 'ThuMotPhan')
        OR (SoTienDaThu = TongCong AND SoTienDaThu > 0 AND TrangThaiThanhToan = 'DaThu')
    ),
    CONSTRAINT CK_HoaDon_SoNgay CHECK (
        (SoNgayO IS NULL AND SoNgayTrongThang IS NULL)
        OR (
            SoNgayO > 0
            AND SoNgayTrongThang BETWEEN 28 AND 31
            AND SoNgayO <= SoNgayTrongThang
            AND SoNgayTrongThang = DAY(LAST_DAY(STR_TO_DATE(
                CONCAT(Nam, '-', LPAD(Thang, 2, '0'), '-01'), '%Y-%m-%d'
            )))
        )
    )
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 10. CHITIETHOADON — Chi tiết từng dòng dịch vụ/tiền phòng trong hóa đơn
-- ChiSoDienNuocId chỉ có giá trị nếu dòng này thuộc dịch vụ TheoChiSo,
-- để biết hóa đơn dùng số liệu đọc nào.
-- ============================================================
CREATE TABLE ChiTietHoaDon (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HoaDonId INT NOT NULL,
    DichVuId INT NOT NULL,
    ChiSoDienNuocId INT NULL,
    SoLuong DECIMAL(10,2) NOT NULL DEFAULT 1,
    DonGia DECIMAL(12,2) NOT NULL,
    ThanhTien DECIMAL(12,0) NOT NULL,
    TenDichVuSnapshot VARCHAR(100) NOT NULL,
    DonViTinhSnapshot VARCHAR(30) NOT NULL,
    CONSTRAINT FK_CTHD_HoaDon FOREIGN KEY (HoaDonId) REFERENCES HoaDon(Id),
    CONSTRAINT FK_CTHD_DichVu FOREIGN KEY (DichVuId) REFERENCES DichVu(Id),
    CONSTRAINT FK_CTHD_ChiSo FOREIGN KEY (ChiSoDienNuocId) REFERENCES ChiSoDienNuoc(Id),
    CONSTRAINT CK_CTHD_Tien CHECK (
        SoLuong >= 0
        AND DonGia >= 0
        AND ThanhTien >= 0
        AND ThanhTien = ROUND(SoLuong * DonGia, 0)
    )
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 11. THANHTOAN — Lịch sử từng lần thu tiền (1 hóa đơn có thể thu nhiều lần)
-- HinhThuc: TienMat | ChuyenKhoan
-- ============================================================
CREATE TABLE ThanhToan (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HoaDonId INT NOT NULL,
    SoTien DECIMAL(12,0) NOT NULL,
    NgayThu DATE NOT NULL,
    HinhThuc VARCHAR(20) NOT NULL,
    GhiChu VARCHAR(255) NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_ThanhToan_HoaDon FOREIGN KEY (HoaDonId) REFERENCES HoaDon(Id),
    CONSTRAINT CK_ThanhToan_Tien CHECK (SoTien > 0),
    CONSTRAINT CK_ThanhToan_HinhThuc CHECK (HinhThuc IN ('TienMat', 'ChuyenKhoan', 'KetChuyenNo', 'TruCoc')),
    CONSTRAINT CK_ThanhToan_Ngay CHECK (YEAR(NgayThu) BETWEEN 2000 AND 2100)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 12. GIAODICHCOC - ledger tien coc theo hop dong
-- SoTien la delta co dau:
--   ThuCoc/ThuThemCoc: duong
--   HoanCoc/TruNo: am
--   DieuChinh: duong hoac am, dung cho chuyen coc/chinh lech
-- SoDuSauGiaoDich giu snapshot de audit nhanh.
-- ============================================================
CREATE TABLE GiaoDichCoc (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HopDongId INT NOT NULL,
    LoaiGiaoDich VARCHAR(30) NOT NULL,
    SoTien DECIMAL(12,0) NOT NULL,
    SoDuSauGiaoDich DECIMAL(12,0) NOT NULL DEFAULT 0,
    NgayGiaoDich DATE NOT NULL,
    HoaDonId INT NULL,
    PhuongThuc VARCHAR(20) NULL,
    GhiChu VARCHAR(255) NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_GiaoDichCoc_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    CONSTRAINT FK_GiaoDichCoc_HoaDon FOREIGN KEY (HoaDonId) REFERENCES HoaDon(Id),
    CONSTRAINT CK_GiaoDichCoc_Loai CHECK (LoaiGiaoDich IN ('ThuCoc', 'ThuThemCoc', 'HoanCoc', 'TruNo', 'DieuChinh')),
    CONSTRAINT CK_GiaoDichCoc_PhuongThuc CHECK (PhuongThuc IS NULL OR PhuongThuc IN ('TienMat', 'ChuyenKhoan')),
    CONSTRAINT CK_GiaoDichCoc_SoDu CHECK (SoDuSauGiaoDich >= 0),
    CONSTRAINT CK_GiaoDichCoc_SoTien CHECK (
        (LoaiGiaoDich IN ('ThuCoc', 'ThuThemCoc') AND SoTien > 0)
        OR (LoaiGiaoDich IN ('HoanCoc', 'TruNo') AND SoTien < 0)
        OR (LoaiGiaoDich = 'DieuChinh' AND SoTien <> 0)
    ),
    CONSTRAINT CK_GiaoDichCoc_Ngay CHECK (YEAR(NgayGiaoDich) BETWEEN 2000 AND 2100),
    CONSTRAINT CK_GiaoDichCoc_LienKet CHECK (HoaDonId IS NULL OR LoaiGiaoDich = 'TruNo')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_GiaoDichCoc_HopDong ON GiaoDichCoc(HopDongId, NgayGiaoDich, Id);
CREATE INDEX IX_GiaoDichCoc_HoaDon ON GiaoDichCoc(HoaDonId);

-- ============================================================
-- 12.1. KHOANPHATSINHHOPDONG - khoan mot lan gan voi hop dong
-- Dung cho den bu hu hong, mat chia khoa, phu thu/phat mot lan.
-- Neu da dua vao hoa don, cong no di theo HoaDon. Neu tra phong chua co
-- hoa don moi, khoan nay co the duoc tru truc tiep vao coc.
-- ============================================================
CREATE TABLE KhoanPhatSinhHopDong (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HopDongId INT NOT NULL,
    HoaDonId INT NULL,
    NgayPhatSinh DATE NOT NULL,
    LoaiKhoan VARCHAR(50) NOT NULL,
    MoTa VARCHAR(500) NOT NULL,
    SoTien DECIMAL(12,0) NOT NULL,
    SoTienDaXuLy DECIMAL(12,0) NOT NULL DEFAULT 0,
    TrangThai VARCHAR(30) NOT NULL DEFAULT 'ChuaXuLy',
    GhiChu VARCHAR(255) NULL,
    MoTaHoaDonSnapshot VARCHAR(500) NULL,
    SoTienHoaDonSnapshot DECIMAL(12,0) NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_KhoanPhatSinh_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    CONSTRAINT FK_KhoanPhatSinh_HoaDon FOREIGN KEY (HoaDonId) REFERENCES HoaDon(Id),
    CONSTRAINT CK_KhoanPhatSinh_Nam CHECK (YEAR(NgayPhatSinh) BETWEEN 2000 AND 2100),
    CONSTRAINT CK_KhoanPhatSinh_Tien CHECK (SoTien > 0 AND SoTienDaXuLy >= 0 AND SoTienDaXuLy <= SoTien),
    CONSTRAINT CK_KhoanPhatSinh_TrangThai CHECK (TrangThai IN ('ChuaXuLy', 'DaDuaVaoHoaDon', 'DaThu', 'DaTruCoc', 'DaHuy')),
    CONSTRAINT CK_KhoanPhatSinh_HoaDonSnapshot CHECK (
        HoaDonId IS NULL OR (MoTaHoaDonSnapshot IS NOT NULL AND SoTienHoaDonSnapshot > 0)
    )
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_KhoanPhatSinh_HopDong_TrangThai
    ON KhoanPhatSinhHopDong(HopDongId, TrangThai, NgayPhatSinh, Id);
CREATE INDEX IX_KhoanPhatSinh_HoaDon
    ON KhoanPhatSinhHopDong(HoaDonId);

-- ============================================================
-- 12. THUCHI — Sổ thu chi ngoài tiền phòng (sửa chữa, mua sắm, lương...)
-- LoaiThuChi: Thu | Chi
-- PhongId NULL nếu là thu/chi chung, không gắn phòng cụ thể
-- ============================================================
CREATE TABLE ThuChi (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    LoaiGiaoDich VARCHAR(10) NOT NULL,
    DanhMuc VARCHAR(100) NOT NULL,
    SoTien DECIMAL(12,0) NOT NULL,
    NgayPhatSinh DATE NOT NULL,
    NoiDung VARCHAR(500) NULL,
    PhongId INT NULL,
    GhiChu TEXT NULL,
    CONSTRAINT FK_ThuChi_Phong FOREIGN KEY (PhongId) REFERENCES Phong(Id),
    CONSTRAINT CK_ThuChi_Loai CHECK (LoaiGiaoDich IN ('Thu', 'Chi')),
    CONSTRAINT CK_ThuChi_Tien CHECK (SoTien > 0),
    CONSTRAINT CK_ThuChi_Ngay CHECK (YEAR(NgayPhatSinh) BETWEEN 2000 AND 2100)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 13. LICHSUTHAYDOIGIA — Lịch sử thay đổi đơn giá (phòng / dịch vụ)
--
-- LƯU Ý QUAN TRỌNG (do mô hình thu tiền TRẢ SAU - postpaid):
-- ThangApDung/NamApDung là KỲ SỬ DỤNG đầu tiên tính giá mới, khớp với
-- cột Thang/Nam của HoaDon — KHÔNG phải ngày thu tiền hay ngày admin
-- nhập liệu (2 mốc này luôn lệch nhau ít nhất 1 tháng vì thu trả sau).
--
-- Khi lập hóa đơn cho kỳ (Thang, Nam) của 1 HopDong hoặc PhongDichVu,
-- PHẢI tra bảng này để lấy đúng giá hiệu lực của kỳ đó (lấy bản ghi có
-- ThangApDung/NamApDung <= kỳ đang lập, gần nhất), KHÔNG được lấy thẳng
-- giá hiện tại trong HopDong.TienThueThoaThuan / PhongDichVu.DonGia —
-- vì nếu nhập giá mới sớm/trễ so với kỳ áp dụng thực tế sẽ tính sai.
--
-- Trường hợp tăng giá khi KẾT THÚC hợp đồng (tạo hợp đồng mới, phổ biến
-- nhất theo thực tế quản lý) thì KHÔNG cần dùng bảng này, vì hợp đồng
-- mới đã có TienThueThoaThuan mới ngay từ đầu. Bảng này chỉ dùng cho
-- trường hợp hiếm: đổi giá khi hợp đồng vẫn còn hiệu lực.
-- ============================================================
CREATE TABLE LichSuThayDoiGia (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    LoaiDoiTuong VARCHAR(30) NOT NULL,   -- HopDong | DichVu (DichVu dùng Id của PhongDichVu)
    DoiTuongId INT NOT NULL,             -- HopDongId hoặc PhongDichVuId tương ứng
    GiaCu DECIMAL(12,2) NOT NULL,
    GiaMoi DECIMAL(12,2) NOT NULL,
    ThangApDung TINYINT NOT NULL,
    NamApDung SMALLINT NOT NULL,
    LyDo VARCHAR(255) NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_LichSuGia_DoiTuongKy UNIQUE (LoaiDoiTuong, DoiTuongId, ThangApDung, NamApDung),
    CONSTRAINT CK_LichSuGia_Loai CHECK (LoaiDoiTuong IN ('HopDong', 'DichVu', 'PhongLegacy')),
    CONSTRAINT CK_LichSuGia_Ky CHECK (ThangApDung BETWEEN 1 AND 12 AND NamApDung BETWEEN 2000 AND 2100),
    CONSTRAINT CK_LichSuGia_Tien CHECK (GiaCu >= 0 AND GiaMoi > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- MySQL khong co exclusion constraint. Hai trigger duoi day khoa dong cha
-- truoc khi kiem tra khoang de serialize ca ghi truc tiep/import va service.
CREATE TRIGGER TR_HopDong_NoOverlap_Insert
BEFORE INSERT ON HopDong
FOR EACH ROW
BEGIN
    DECLARE LockedRoomId INT;
    DECLARE OverlapCount INT DEFAULT 0;

    SELECT Id INTO LockedRoomId FROM Phong WHERE Id = NEW.PhongId FOR UPDATE;
    IF NEW.TrangThai <> 'DaHuy' THEN
        SELECT COUNT(*) INTO OverlapCount
        FROM HopDong
        WHERE PhongId = NEW.PhongId
          AND TrangThai <> 'DaHuy'
          AND NgayBatDau <= COALESCE(NEW.NgayKetThuc, '9999-12-31')
          AND COALESCE(NgayKetThuc, '9999-12-31') >= NEW.NgayBatDau;
        IF OverlapCount > 0 THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'REVIEW-016: hop dong chong khoang thoi gian cua phong.';
        END IF;
    END IF;
END;

CREATE TRIGGER TR_HopDong_NoOverlap_Update
BEFORE UPDATE ON HopDong
FOR EACH ROW
BEGIN
    DECLARE LockedRoomId INT;
    DECLARE OverlapCount INT DEFAULT 0;

    IF OLD.PhongId <= NEW.PhongId THEN
        SELECT Id INTO LockedRoomId FROM Phong WHERE Id = OLD.PhongId FOR UPDATE;
        IF NEW.PhongId <> OLD.PhongId THEN
            SELECT Id INTO LockedRoomId FROM Phong WHERE Id = NEW.PhongId FOR UPDATE;
        END IF;
    ELSE
        SELECT Id INTO LockedRoomId FROM Phong WHERE Id = NEW.PhongId FOR UPDATE;
        SELECT Id INTO LockedRoomId FROM Phong WHERE Id = OLD.PhongId FOR UPDATE;
    END IF;

    IF NEW.TrangThai <> 'DaHuy' THEN
        SELECT COUNT(*) INTO OverlapCount
        FROM HopDong
        WHERE PhongId = NEW.PhongId
          AND Id <> OLD.Id
          AND TrangThai <> 'DaHuy'
          AND NgayBatDau <= COALESCE(NEW.NgayKetThuc, '9999-12-31')
          AND COALESCE(NgayKetThuc, '9999-12-31') >= NEW.NgayBatDau;
        IF OverlapCount > 0 THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'REVIEW-016: hop dong chong khoang thoi gian cua phong.';
        END IF;
    END IF;
END;

CREATE TRIGGER TR_HDKT_RepresentativeOverlap_Insert
BEFORE INSERT ON HopDongKhachThue
FOR EACH ROW
BEGIN
    DECLARE LockedContractId INT;
    DECLARE OverlapCount INT DEFAULT 0;

    SELECT Id INTO LockedContractId FROM HopDong WHERE Id = NEW.HopDongId FOR UPDATE;
    IF NEW.LaDaiDien = 1 THEN
        SELECT COUNT(*) INTO OverlapCount
        FROM HopDongKhachThue
        WHERE HopDongId = NEW.HopDongId
          AND LaDaiDien = 1
          AND NgayBatDau <= COALESCE(NEW.NgayKetThuc, '9999-12-31')
          AND COALESCE(NgayKetThuc, '9999-12-31') >= NEW.NgayBatDau;
        IF OverlapCount > 0 THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'REVIEW-016: hai dai dien chong thoi gian trong hop dong.';
        END IF;
    END IF;
END;

CREATE TRIGGER TR_HDKT_RepresentativeOverlap_Update
BEFORE UPDATE ON HopDongKhachThue
FOR EACH ROW
BEGIN
    DECLARE LockedContractId INT;
    DECLARE OverlapCount INT DEFAULT 0;

    IF OLD.HopDongId <= NEW.HopDongId THEN
        SELECT Id INTO LockedContractId FROM HopDong WHERE Id = OLD.HopDongId FOR UPDATE;
        IF NEW.HopDongId <> OLD.HopDongId THEN
            SELECT Id INTO LockedContractId FROM HopDong WHERE Id = NEW.HopDongId FOR UPDATE;
        END IF;
    ELSE
        SELECT Id INTO LockedContractId FROM HopDong WHERE Id = NEW.HopDongId FOR UPDATE;
        SELECT Id INTO LockedContractId FROM HopDong WHERE Id = OLD.HopDongId FOR UPDATE;
    END IF;

    IF NEW.LaDaiDien = 1 THEN
        SELECT COUNT(*) INTO OverlapCount
        FROM HopDongKhachThue
        WHERE HopDongId = NEW.HopDongId
          AND Id <> OLD.Id
          AND LaDaiDien = 1
          AND NgayBatDau <= COALESCE(NEW.NgayKetThuc, '9999-12-31')
          AND COALESCE(NgayKetThuc, '9999-12-31') >= NEW.NgayBatDau;
        IF OverlapCount > 0 THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'REVIEW-016: hai dai dien chong thoi gian trong hop dong.';
        END IF;
    END IF;
END;

-- ============================================================
-- DỮ LIỆU MẪU BAN ĐẦU CHO DICHVU (tùy chỉnh lại đơn giá theo thực tế)
-- ============================================================
INSERT INTO DichVu (TenDichVu, LoaiTinhPhi, CachTinhCoDinh, DonGiaMacDinh, BatBuocKhiThue, DonViTinh) VALUES
    ('Điện', 'TheoChiSo', 'TheoPhong', 4000, 1, 'kWh'),
    ('Nước', 'CoDinh', 'TheoNguoi', 120000, 0, 'Người/Tháng'),
    ('Internet', 'CoDinh', 'TheoPhong', 100000, 0, 'Tháng'),
    ('Vệ sinh', 'CoDinh', 'TheoNguoi', 40000, 0, 'Người/Tháng'),
    ('Bảo trì thang máy', 'CoDinh', 'TheoNguoi', 50000, 0, 'Người/Tháng'),
    ('Máy giặt', 'CoDinh', 'TheoNguoi', 80000, 0, 'Người/Tháng'),
    ('Gửi xe', 'CoDinh', 'TheoNguoi', 0, 0, 'Xe/Tháng');

-- ============================================================
-- GHI CHÚ TỔNG QUAN QUAN HỆ
-- Nha 1-n Phong
-- Phong 1-n HopDong (theo thời gian)
-- HopDong n-n KhachThue (qua HopDongKhachThue)
-- HopDong n-n PhongDichVu theo kỳ (qua HopDongDichVu)
-- HopDong 1-n HopDong (tự tham chiếu qua HopDongTruocId, ca chuyển phòng)
-- HopDong 1-n HoaDon (theo tháng)
-- HoaDon 1-n HoaDon (tự tham chiếu qua HoaDonGhepId, ca chuyển phòng)
-- HoaDon 1-n ChiTietHoaDon
-- HoaDon 1-n ThanhToan
-- Phong n-n DichVu (qua PhongDichVu, giá riêng từng phòng)
-- ChiSoDienNuoc 1-n ChiTietHoaDon (qua ChiSoDienNuocId)
-- KhoanPhatSinhHopDong gan voi HopDong; neu dua vao hoa don thi lien ket HoaDonId
-- ChiSoNgoaiHopDong audit chỉ số phát sinh ngoài hợp đồng, không liên kết HoaDon
-- LichSuThayDoiGia tham chiếu LOGIC tới HopDong hoặc PhongDichVu qua
--   (LoaiDoiTuong, DoiTuongId) — không đặt FK cứng vì DoiTuongId có thể
--   thuộc 1 trong 2 bảng khác nhau tùy LoaiDoiTuong.
-- ============================================================
