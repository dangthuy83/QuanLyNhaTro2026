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
    CONSTRAINT UQ_Phong_TenPhong UNIQUE (NhaId, TenPhong)
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
    SoDienThoai VARCHAR(20) NULL,
    NgaySinh DATE NULL,
    QueQuan VARCHAR(255) NULL,
    AnhCCCDMatTruoc VARCHAR(500) NULL,
    AnhCCCDMatSau VARCHAR(500) NULL,
    GhiChu TEXT NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 4. DICHVU — Danh mục loại dịch vụ (Điện, Nước, Internet, Vệ sinh...)
-- LoaiTinhPhi: CoDinh | TheoChiSo
-- Đây là "công tắc" quyết định cách tính tiền dịch vụ khi lập hóa đơn.
-- Muốn đổi Nước từ CoDinh sang TheoChiSo sau này: chỉ cần UPDATE dữ liệu
-- dòng này, KHÔNG cần sửa schema hay code, miễn Controller luôn rẽ nhánh
-- theo LoaiTinhPhi (không hard-code so sánh tên dịch vụ).
-- ============================================================
CREATE TABLE DichVu (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    TenDichVu VARCHAR(100) NOT NULL,
    LoaiTinhPhi VARCHAR(20) NOT NULL,
    DonGiaMacDinh DECIMAL(12,2) NOT NULL DEFAULT 0,
    DonViTinh VARCHAR(20) NULL
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
    CONSTRAINT UQ_Phong_DichVu UNIQUE (PhongId, DichVuId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 6. HOPDONG — Hợp đồng thuê
-- TrangThai: DangHieuLuc | DaKetThuc | DaHuy | DaChuyenPhong
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
    CONSTRAINT FK_HopDong_Truoc FOREIGN KEY (HopDongTruocId) REFERENCES HopDong(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 7. HOPDONGKHACHTHUE — Liên kết nhiều-nhiều Hợp đồng <-> Khách thuê
-- LaDaiDien: 1 = người đại diện ký hợp đồng, 0 = người ở thêm
-- ============================================================
CREATE TABLE HopDongKhachThue (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    HopDongId INT NOT NULL,
    KhachThueId INT NOT NULL,
    LaDaiDien BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_HDKT_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    CONSTRAINT FK_HDKT_KhachThue FOREIGN KEY (KhachThueId) REFERENCES KhachThue(Id),
    CONSTRAINT UQ_HopDong_Khach UNIQUE (HopDongId, KhachThueId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

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
    SanLuong DECIMAL(10,2) GENERATED ALWAYS AS (DenChiSo - TuChiSo) STORED,
    NgayGhiNhan DATE NOT NULL,
    LyDo VARCHAR(255) NOT NULL,
    GhiChu VARCHAR(255) NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_ChiSoNgoaiHopDong_Phong FOREIGN KEY (PhongId) REFERENCES Phong(Id),
    CONSTRAINT FK_ChiSoNgoaiHopDong_DichVu FOREIGN KEY (DichVuId) REFERENCES DichVu(Id),
    CONSTRAINT CK_ChiSoNgoaiHopDong_KhongAm CHECK (TuChiSo >= 0 AND DenChiSo >= 0),
    CONSTRAINT CK_ChiSoNgoaiHopDong_SanLuong CHECK (DenChiSo >= TuChiSo)
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
    NgayThuThucTe DATE NULL,
    GhiChu TEXT NULL,
    CONSTRAINT FK_HoaDon_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    CONSTRAINT FK_HoaDon_Ghep FOREIGN KEY (HoaDonGhepId) REFERENCES HoaDon(Id),
    CONSTRAINT UQ_HoaDon UNIQUE (HopDongId, Thang, Nam)
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
    CONSTRAINT FK_CTHD_HoaDon FOREIGN KEY (HoaDonId) REFERENCES HoaDon(Id),
    CONSTRAINT FK_CTHD_DichVu FOREIGN KEY (DichVuId) REFERENCES DichVu(Id),
    CONSTRAINT FK_CTHD_ChiSo FOREIGN KEY (ChiSoDienNuocId) REFERENCES ChiSoDienNuoc(Id)
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
    HinhThuc VARCHAR(20) NULL,
    GhiChu VARCHAR(255) NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_ThanhToan_HoaDon FOREIGN KEY (HoaDonId) REFERENCES HoaDon(Id)
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
    GhiChu VARCHAR(255) NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_GiaoDichCoc_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    CONSTRAINT FK_GiaoDichCoc_HoaDon FOREIGN KEY (HoaDonId) REFERENCES HoaDon(Id),
    CONSTRAINT CK_GiaoDichCoc_Loai CHECK (LoaiGiaoDich IN ('ThuCoc', 'ThuThemCoc', 'HoanCoc', 'TruNo', 'DieuChinh')),
    CONSTRAINT CK_GiaoDichCoc_SoDu CHECK (SoDuSauGiaoDich >= 0)
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
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_KhoanPhatSinh_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    CONSTRAINT FK_KhoanPhatSinh_HoaDon FOREIGN KEY (HoaDonId) REFERENCES HoaDon(Id),
    CONSTRAINT CK_KhoanPhatSinh_Tien CHECK (SoTien > 0 AND SoTienDaXuLy >= 0 AND SoTienDaXuLy <= SoTien),
    CONSTRAINT CK_KhoanPhatSinh_TrangThai CHECK (TrangThai IN ('ChuaXuLy', 'DaDuaVaoHoaDon', 'DaThu', 'DaTruCoc', 'DaHuy'))
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
    CONSTRAINT FK_ThuChi_Phong FOREIGN KEY (PhongId) REFERENCES Phong(Id)
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
    LoaiDoiTuong VARCHAR(30) NOT NULL,   -- HopDong | PhongDichVu
    DoiTuongId INT NOT NULL,             -- Id của HopDong hoặc PhongDichVu tương ứng
    GiaCu DECIMAL(12,2) NOT NULL,
    GiaMoi DECIMAL(12,2) NOT NULL,
    ThangApDung TINYINT NOT NULL,
    NamApDung SMALLINT NOT NULL,
    LyDo VARCHAR(255) NULL,
    NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- DỮ LIỆU MẪU BAN ĐẦU CHO DICHVU (tùy chỉnh lại đơn giá theo thực tế)
-- ============================================================
INSERT INTO DichVu (TenDichVu, LoaiTinhPhi, DonGiaMacDinh, DonViTinh) VALUES
    ('Điện', 'TheoChiSo', 0, 'kWh'),
    ('Nước', 'CoDinh', 0, 'người/tháng'),
    ('Internet', 'CoDinh', 0, 'tháng'),
    ('Vệ sinh', 'CoDinh', 0, 'tháng'),
    ('Gửi xe', 'CoDinh', 0, 'xe/tháng');

-- ============================================================
-- GHI CHÚ TỔNG QUAN QUAN HỆ
-- Nha 1-n Phong
-- Phong 1-n HopDong (theo thời gian)
-- HopDong n-n KhachThue (qua HopDongKhachThue)
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
