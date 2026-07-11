-- Migration: fixed service quantity method
-- Purpose:
--   Add DichVu.CachTinhCoDinh so fixed services can charge either once per
--   room/contract or once per tenant attached to the contract.
--
-- How to run:
--   1. Backup the database.
--   2. Select the QuanLyNhaTro database in MySQL Workbench, or run:
--        USE QuanLyNhaTro;
--   3. Run this whole file once. It is guarded by INFORMATION_SCHEMA checks.
--
-- Notes:
--   Existing services default to TheoPhong. Review current fixed services
--   such as water, cleaning, laundry, or maintenance and set TheoNguoi where
--   that is the real business rule.

DROP PROCEDURE IF EXISTS sp_migrate_fixed_service_quantity_method;

DELIMITER $$

CREATE PROCEDURE sp_migrate_fixed_service_quantity_method()
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'DichVu'
          AND COLUMN_NAME = 'CachTinhCoDinh'
    ) THEN
        ALTER TABLE DichVu
            ADD COLUMN CachTinhCoDinh VARCHAR(20) NOT NULL DEFAULT 'TheoPhong' AFTER LoaiTinhPhi;
    END IF;

    UPDATE DichVu
    SET CachTinhCoDinh = 'TheoPhong'
    WHERE Id >= 0
      AND (CachTinhCoDinh IS NULL OR CachTinhCoDinh = '');

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = 'DichVu'
          AND CONSTRAINT_NAME = 'CK_DichVu_LoaiTinhPhi'
    ) THEN
        ALTER TABLE DichVu
            ADD CONSTRAINT CK_DichVu_LoaiTinhPhi
            CHECK (LoaiTinhPhi IN ('CoDinh', 'TheoChiSo'));
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = 'DichVu'
          AND CONSTRAINT_NAME = 'CK_DichVu_CachTinhCoDinh'
    ) THEN
        ALTER TABLE DichVu
            ADD CONSTRAINT CK_DichVu_CachTinhCoDinh
            CHECK (CachTinhCoDinh IN ('TheoPhong', 'TheoNguoi'));
    END IF;
END$$

DELIMITER ;

CALL sp_migrate_fixed_service_quantity_method();

DROP PROCEDURE IF EXISTS sp_migrate_fixed_service_quantity_method;
