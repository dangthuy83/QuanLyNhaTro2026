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
