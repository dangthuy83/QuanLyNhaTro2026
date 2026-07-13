-- REVIEW-008: lich su cu tru theo ngay cho HopDongKhachThue.
-- Chay khoi DRY-RUN truoc, luu ket qua va dung lai neu co dong bat thuong can xu ly thu cong.
-- Script khong merge KhachThue, khong xoa HopDongKhachThue va khong tao UNIQUE cung cho CCCD.

-- DRY-RUN BAT BUOC
SELECT COUNT(*) AS TotalHopDongKhachThue FROM HopDongKhachThue;

SELECT hd.Id AS HopDongId, COUNT(hdkt.Id) AS SoDong, SUM(CASE WHEN hdkt.LaDaiDien=1 THEN 1 ELSE 0 END) AS SoDaiDien
FROM HopDong hd LEFT JOIN HopDongKhachThue hdkt ON hdkt.HopDongId=hd.Id
GROUP BY hd.Id HAVING SoDaiDien=0;

SELECT hd.Id AS HopDongId, COUNT(hdkt.Id) AS SoDong, SUM(CASE WHEN hdkt.LaDaiDien=1 THEN 1 ELSE 0 END) AS SoDaiDien
FROM HopDong hd INNER JOIN HopDongKhachThue hdkt ON hdkt.HopDongId=hd.Id
GROUP BY hd.Id HAVING SoDaiDien>1;

SELECT HopDongId, KhachThueId, COUNT(*) AS SoDongTrung
FROM HopDongKhachThue GROUP BY HopDongId, KhachThueId HAVING COUNT(*)>1;

SELECT TRIM(CCCD) AS CCCD, COUNT(*) AS SoHoSo, GROUP_CONCAT(Id ORDER BY Id) AS KhachThueIds
FROM KhachThue WHERE NULLIF(TRIM(CCCD), '') IS NOT NULL
GROUP BY TRIM(CCCD) HAVING COUNT(*)>1;

SELECT Id AS HopDongId, NgayBatDau, NgayKetThuc, TrangThai
FROM HopDong WHERE NgayBatDau IS NULL OR (NgayKetThuc IS NOT NULL AND NgayKetThuc<NgayBatDau);

DELIMITER $$
DROP PROCEDURE IF EXISTS ApplyReview008$$
CREATE PROCEDURE ApplyReview008()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='HopDongKhachThue' AND COLUMN_NAME='NgayBatDau'
    ) THEN
        ALTER TABLE HopDongKhachThue
            ADD COLUMN NgayBatDau DATE NULL AFTER KhachThueId,
            ADD COLUMN NgayKetThucDuKien DATE NULL AFTER NgayBatDau,
            ADD COLUMN NgayKetThuc DATE NULL AFTER NgayKetThucDuKien;

        UPDATE HopDongKhachThue hdkt
        INNER JOIN HopDong hd ON hd.Id=hdkt.HopDongId
        SET hdkt.NgayBatDau=hd.NgayBatDau,
            hdkt.NgayKetThucDuKien=CASE
                WHEN hd.NgayKetThuc IS NOT NULL AND hd.NgayKetThuc>=hd.NgayBatDau THEN hd.NgayKetThuc ELSE NULL END,
            hdkt.NgayKetThuc=CASE
                WHEN hd.TrangThai IN ('DaKetThuc','DaChuyenPhong') THEN hd.NgayKetThuc ELSE NULL END;

        ALTER TABLE HopDongKhachThue MODIFY COLUMN NgayBatDau DATE NOT NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='HopDongKhachThue' AND INDEX_NAME='IX_HDKT_HopDong_FK'
    ) THEN
        CREATE INDEX IX_HDKT_HopDong_FK ON HopDongKhachThue(HopDongId);
    END IF;

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='HopDongKhachThue' AND INDEX_NAME='UQ_HopDong_Khach'
    ) THEN
        ALTER TABLE HopDongKhachThue DROP INDEX UQ_HopDong_Khach;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='HopDongKhachThue' AND CONSTRAINT_NAME='CK_HDKT_KhoangNgay'
    ) THEN
        ALTER TABLE HopDongKhachThue ADD CONSTRAINT CK_HDKT_KhoangNgay CHECK (
            (NgayKetThucDuKien IS NULL OR NgayKetThucDuKien>=NgayBatDau)
            AND (NgayKetThuc IS NULL OR NgayKetThuc>=NgayBatDau));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='HopDongKhachThue' AND INDEX_NAME='UQ_HDKT_GiaiDoan'
    ) THEN
        ALTER TABLE HopDongKhachThue
            ADD CONSTRAINT UQ_HDKT_GiaiDoan UNIQUE (HopDongId, KhachThueId, NgayBatDau);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='HopDongKhachThue' AND INDEX_NAME='IX_HDKT_HopDong_HieuLuc'
    ) THEN
        CREATE INDEX IX_HDKT_HopDong_HieuLuc ON HopDongKhachThue(HopDongId, NgayBatDau, NgayKetThuc, KhachThueId);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='HopDongKhachThue' AND INDEX_NAME='IX_HDKT_Khach_HieuLuc'
    ) THEN
        CREATE INDEX IX_HDKT_Khach_HieuLuc ON HopDongKhachThue(KhachThueId, NgayBatDau, NgayKetThuc, HopDongId);
    END IF;
END$$
CALL ApplyReview008()$$
DROP PROCEDURE ApplyReview008$$
DELIMITER ;

SELECT COUNT(*) AS TotalAfter, SUM(NgayBatDau IS NULL) AS MissingNgayBatDau FROM HopDongKhachThue;
SELECT INDEX_NAME, NON_UNIQUE
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='HopDongKhachThue'
  AND INDEX_NAME IN ('UQ_HDKT_GiaiDoan','IX_HDKT_HopDong_HieuLuc','IX_HDKT_Khach_HieuLuc')
GROUP BY INDEX_NAME, NON_UNIQUE ORDER BY INDEX_NAME;
