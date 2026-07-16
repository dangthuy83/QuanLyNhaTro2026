-- REVIEW-027: mo so hop dong dang van hanh, coc thuc te, cong no cu va moc chi so.
-- Apply-once, blocker-first, rerun-safe. Khong tu suy dien/backfill va khong tao thanh toan.

-- DRY-RUN BAT BUOC (chi doc)
SELECT COUNT(*) AS ExistingOpeningTables
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA=DATABASE()
  AND TABLE_NAME IN ('DotMoSo','HopDongMoSo','CongNoMoSo','ChiSoMoSo');

SELECT COUNT(*) AS InvalidExistingDepositRows
FROM GiaoDichCoc
WHERE SoDuSauGiaoDich < 0 OR SoTien = 0;

DELIMITER $$
DROP PROCEDURE IF EXISTS ApplyReview027$$
CREATE PROCEDURE ApplyReview027()
BEGIN
    DECLARE AnomalyCount INT DEFAULT 0;

    SELECT COUNT(*) INTO AnomalyCount
    FROM GiaoDichCoc
    WHERE SoDuSauGiaoDich < 0 OR SoTien = 0;
    IF AnomalyCount > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT='REVIEW-027 BLOCKED: ledger coc hien huu co so du am hoac delta bang 0.';
    END IF;

    CREATE TABLE IF NOT EXISTS DotMoSo (
        Id INT AUTO_INCREMENT PRIMARY KEY,
        NgayChot DATE NOT NULL,
        TenNguon VARCHAR(255) NOT NULL,
        Sha256 CHAR(64) NOT NULL,
        NguoiDuyet VARCHAR(100) NOT NULL,
        GhiChu VARCHAR(500) NULL,
        NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        CONSTRAINT UQ_DotMoSo_Sha256 UNIQUE (Sha256),
        CONSTRAINT CK_DotMoSo_Ngay CHECK (YEAR(NgayChot) BETWEEN 2000 AND 2100),
        CONSTRAINT CK_DotMoSo_Nguon CHECK (
            CHAR_LENGTH(TRIM(TenNguon)) > 0
            AND Sha256 REGEXP '^[0-9A-F]{64}$'
            AND CHAR_LENGTH(TRIM(NguoiDuyet)) > 0)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

    CREATE TABLE IF NOT EXISTS HopDongMoSo (
        Id INT AUTO_INCREMENT PRIMARY KEY,
        DotMoSoId INT NOT NULL,
        HopDongId INT NOT NULL,
        NguonThamChieu VARCHAR(100) NOT NULL,
        NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        CONSTRAINT UQ_HopDongMoSo_HopDong UNIQUE (HopDongId),
        CONSTRAINT UQ_HopDongMoSo_Nguon UNIQUE (DotMoSoId,NguonThamChieu),
        CONSTRAINT FK_HopDongMoSo_Dot FOREIGN KEY (DotMoSoId) REFERENCES DotMoSo(Id),
        CONSTRAINT FK_HopDongMoSo_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
        CONSTRAINT CK_HopDongMoSo_Nguon CHECK (CHAR_LENGTH(TRIM(NguonThamChieu)) > 0)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

    CREATE TABLE IF NOT EXISTS CongNoMoSo (
        Id INT AUTO_INCREMENT PRIMARY KEY,
        DotMoSoId INT NOT NULL,
        HopDongId INT NOT NULL,
        SoTien DECIMAL(12,0) NOT NULL,
        DenKyThang TINYINT NOT NULL,
        DenKyNam SMALLINT NOT NULL,
        MaChungTu VARCHAR(100) NOT NULL,
        NguonThamChieu VARCHAR(100) NOT NULL,
        HoaDonTiepNhanId INT NULL,
        NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        INDEX IX_CongNoMoSo_HopDong (HopDongId,HoaDonTiepNhanId),
        CONSTRAINT UQ_CongNoMoSo_Nguon UNIQUE (DotMoSoId,NguonThamChieu),
        CONSTRAINT FK_CongNoMoSo_Dot FOREIGN KEY (DotMoSoId) REFERENCES DotMoSo(Id),
        CONSTRAINT FK_CongNoMoSo_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
        CONSTRAINT FK_CongNoMoSo_HoaDon FOREIGN KEY (HoaDonTiepNhanId) REFERENCES HoaDon(Id),
        CONSTRAINT CK_CongNoMoSo_Tien CHECK (SoTien > 0),
        CONSTRAINT CK_CongNoMoSo_Ky CHECK (DenKyThang BETWEEN 1 AND 12 AND DenKyNam BETWEEN 2000 AND 2100),
        CONSTRAINT CK_CongNoMoSo_Nguon CHECK (
            CHAR_LENGTH(TRIM(MaChungTu)) > 0 AND CHAR_LENGTH(TRIM(NguonThamChieu)) > 0)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

    CREATE TABLE IF NOT EXISTS ChiSoMoSo (
        Id INT AUTO_INCREMENT PRIMARY KEY,
        DotMoSoId INT NOT NULL,
        HopDongId INT NOT NULL,
        PhongId INT NOT NULL,
        DichVuId INT NOT NULL,
        NgayChot DATE NOT NULL,
        ChiSo DECIMAL(10,2) NOT NULL,
        NguonThamChieu VARCHAR(100) NOT NULL,
        NgayTao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        CONSTRAINT UQ_ChiSoMoSo_HopDongDichVu UNIQUE (HopDongId,DichVuId),
        CONSTRAINT UQ_ChiSoMoSo_Nguon UNIQUE (DotMoSoId,NguonThamChieu),
        CONSTRAINT FK_ChiSoMoSo_Dot FOREIGN KEY (DotMoSoId) REFERENCES DotMoSo(Id),
        CONSTRAINT FK_ChiSoMoSo_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
        CONSTRAINT FK_ChiSoMoSo_Phong FOREIGN KEY (PhongId) REFERENCES Phong(Id),
        CONSTRAINT FK_ChiSoMoSo_DichVu FOREIGN KEY (DichVuId) REFERENCES DichVu(Id),
        CONSTRAINT CK_ChiSoMoSo_Ngay CHECK (YEAR(NgayChot) BETWEEN 2000 AND 2100),
        CONSTRAINT CK_ChiSoMoSo_GiaTri CHECK (ChiSo >= 0),
        CONSTRAINT CK_ChiSoMoSo_Nguon CHECK (CHAR_LENGTH(TRIM(NguonThamChieu)) > 0)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='GiaoDichCoc' AND COLUMN_NAME='DotMoSoId'
    ) THEN
        ALTER TABLE GiaoDichCoc ADD COLUMN DotMoSoId INT NULL AFTER PhuongThuc;
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='GiaoDichCoc' AND COLUMN_NAME='NguonThamChieu'
    ) THEN
        ALTER TABLE GiaoDichCoc ADD COLUMN NguonThamChieu VARCHAR(100) NULL AFTER DotMoSoId;
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='GiaoDichCoc' AND INDEX_NAME='IX_GiaoDichCoc_DotMoSo'
    ) THEN
        CREATE INDEX IX_GiaoDichCoc_DotMoSo ON GiaoDichCoc(DotMoSoId);
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='GiaoDichCoc' AND CONSTRAINT_NAME='FK_GiaoDichCoc_DotMoSo'
    ) THEN
        ALTER TABLE GiaoDichCoc ADD CONSTRAINT FK_GiaoDichCoc_DotMoSo
            FOREIGN KEY (DotMoSoId) REFERENCES DotMoSo(Id);
    END IF;

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='GiaoDichCoc' AND CONSTRAINT_NAME='CK_GiaoDichCoc_Loai'
    ) THEN
        ALTER TABLE GiaoDichCoc DROP CHECK CK_GiaoDichCoc_Loai;
    END IF;
    ALTER TABLE GiaoDichCoc ADD CONSTRAINT CK_GiaoDichCoc_Loai
        CHECK (LoaiGiaoDich IN ('ThuCoc','ThuThemCoc','HoanCoc','TruNo','DieuChinh','SoDuMoSo'));

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='GiaoDichCoc' AND CONSTRAINT_NAME='CK_GiaoDichCoc_SoTien'
    ) THEN
        ALTER TABLE GiaoDichCoc DROP CHECK CK_GiaoDichCoc_SoTien;
    END IF;
    ALTER TABLE GiaoDichCoc ADD CONSTRAINT CK_GiaoDichCoc_SoTien CHECK (
        (LoaiGiaoDich IN ('ThuCoc','ThuThemCoc','SoDuMoSo') AND SoTien > 0)
        OR (LoaiGiaoDich IN ('HoanCoc','TruNo') AND SoTien < 0)
        OR (LoaiGiaoDich='DieuChinh' AND SoTien <> 0));

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='GiaoDichCoc' AND CONSTRAINT_NAME='CK_GiaoDichCoc_MoSoNguon'
    ) THEN
        ALTER TABLE GiaoDichCoc ADD CONSTRAINT CK_GiaoDichCoc_MoSoNguon CHECK (
            (LoaiGiaoDich='SoDuMoSo' AND DotMoSoId IS NOT NULL
             AND CHAR_LENGTH(TRIM(NguonThamChieu)) > 0
             AND PhuongThuc IS NULL AND HoaDonId IS NULL)
            OR (LoaiGiaoDich<>'SoDuMoSo' AND DotMoSoId IS NULL AND NguonThamChieu IS NULL));
    END IF;
END$$

CALL ApplyReview027()$$
DROP PROCEDURE ApplyReview027$$
DELIMITER ;

-- POST-CHECK
SELECT COUNT(*) AS OpeningTableCount
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA=DATABASE()
  AND TABLE_NAME IN ('DotMoSo','HopDongMoSo','CongNoMoSo','ChiSoMoSo');
SELECT COUNT(*) AS DepositOpeningColumns
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='GiaoDichCoc'
  AND COLUMN_NAME IN ('DotMoSoId','NguonThamChieu');
