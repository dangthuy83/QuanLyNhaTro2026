-- Migration: contract-scoped meter readings
-- Purpose:
--   Allow multiple ChiSoDienNuoc rows for the same room/service/month when
--   the room has old tenant usage, off-contract usage, and new tenant usage
--   in the same billing period.
--
-- How to run:
--   1. Select the QuanLyNhaTro database in MySQL Workbench, or run:
--        USE QuanLyNhaTro;
--   2. Run this whole file once.
--   3. The script is intentionally guarded with INFORMATION_SCHEMA checks so
--      re-running it should be safe for normal cases.

DROP PROCEDURE IF EXISTS sp_migrate_contract_scoped_meter_readings;

DELIMITER $$

CREATE PROCEDURE sp_migrate_contract_scoped_meter_readings()
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'ChiSoDienNuoc'
          AND COLUMN_NAME = 'HopDongId'
    ) THEN
        ALTER TABLE ChiSoDienNuoc
            ADD COLUMN HopDongId INT NULL AFTER Id;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'ChiSoDienNuoc'
          AND COLUMN_NAME = 'ChiSoScopeKey'
    ) THEN
        ALTER TABLE ChiSoDienNuoc
            ADD COLUMN ChiSoScopeKey INT
                GENERATED ALWAYS AS (COALESCE(HopDongId, 0)) STORED;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'ChiSoDienNuoc'
          AND INDEX_NAME = 'IX_ChiSo_Phong_FK'
    ) THEN
        CREATE INDEX IX_ChiSo_Phong_FK
            ON ChiSoDienNuoc(PhongId);
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'ChiSoDienNuoc'
          AND INDEX_NAME = 'IX_ChiSo_DichVu_FK'
    ) THEN
        CREATE INDEX IX_ChiSo_DichVu_FK
            ON ChiSoDienNuoc(DichVuId);
    END IF;

    IF EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'ChiSoDienNuoc'
          AND INDEX_NAME = 'UQ_ChiSo'
    ) THEN
        ALTER TABLE ChiSoDienNuoc
            DROP INDEX UQ_ChiSo;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = 'ChiSoDienNuoc'
          AND CONSTRAINT_NAME = 'UQ_ChiSo'
          AND CONSTRAINT_TYPE = 'UNIQUE'
    ) THEN
        ALTER TABLE ChiSoDienNuoc
            ADD CONSTRAINT UQ_ChiSo
            UNIQUE (PhongId, DichVuId, Thang, Nam, ChiSoScopeKey);
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'ChiSoDienNuoc'
          AND INDEX_NAME = 'IX_ChiSo_HopDong_Ky'
    ) THEN
        CREATE INDEX IX_ChiSo_HopDong_Ky
            ON ChiSoDienNuoc(HopDongId, Nam, Thang);
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND CONSTRAINT_NAME = 'FK_ChiSo_HopDong'
    ) THEN
        ALTER TABLE ChiSoDienNuoc
            ADD CONSTRAINT FK_ChiSo_HopDong
            FOREIGN KEY (HopDongId) REFERENCES HopDong(Id);
    END IF;
END$$

DELIMITER ;

CALL sp_migrate_contract_scoped_meter_readings();

DROP PROCEDURE IF EXISTS sp_migrate_contract_scoped_meter_readings;
