-- Migration: default service prices and contract one-off charges
-- Purpose:
--   1. Add DichVu.DonGiaMacDinh as a default suggestion when assigning
--      services to rooms. Room-specific pricing remains in PhongDichVu.DonGia.
--   2. Add KhoanPhatSinhHopDong for one-off tenant charges such as damaged
--      property compensation, lost keys, penalties, or ad hoc surcharges.
--   3. Add HoaDon.TongTienPhatSinh so invoice totals can keep those one-off
--      charges separate from recurring service lines.
--
-- How to run:
--   1. Backup the database.
--   2. Select the QuanLyNhaTro database in MySQL Workbench, or run:
--        USE QuanLyNhaTro;
--   3. Run this whole file once. It is guarded by INFORMATION_SCHEMA checks.

DROP PROCEDURE IF EXISTS sp_migrate_default_service_price_and_contract_charges;

DELIMITER $$

CREATE PROCEDURE sp_migrate_default_service_price_and_contract_charges()
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'DichVu'
          AND COLUMN_NAME = 'DonGiaMacDinh'
    ) THEN
        ALTER TABLE DichVu
            ADD COLUMN DonGiaMacDinh DECIMAL(12,2) NOT NULL DEFAULT 0 AFTER LoaiTinhPhi;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'HoaDon'
          AND COLUMN_NAME = 'TongTienPhatSinh'
    ) THEN
        ALTER TABLE HoaDon
            ADD COLUMN TongTienPhatSinh DECIMAL(12,0) NOT NULL DEFAULT 0 AFTER TongTienDichVu;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'KhoanPhatSinhHopDong'
    ) THEN
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
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'KhoanPhatSinhHopDong'
          AND INDEX_NAME = 'IX_KhoanPhatSinh_HopDong_TrangThai'
    ) THEN
        CREATE INDEX IX_KhoanPhatSinh_HopDong_TrangThai
            ON KhoanPhatSinhHopDong(HopDongId, TrangThai, NgayPhatSinh, Id);
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'KhoanPhatSinhHopDong'
          AND INDEX_NAME = 'IX_KhoanPhatSinh_HoaDon'
    ) THEN
        CREATE INDEX IX_KhoanPhatSinh_HoaDon
            ON KhoanPhatSinhHopDong(HoaDonId);
    END IF;
END$$

DELIMITER ;

CALL sp_migrate_default_service_price_and_contract_charges();

DROP PROCEDURE IF EXISTS sp_migrate_default_service_price_and_contract_charges;
