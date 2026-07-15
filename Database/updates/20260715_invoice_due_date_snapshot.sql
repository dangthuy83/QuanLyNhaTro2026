-- REVIEW-017: snapshot ngay den han cua hoa don.
-- Apply-once, rerun-safe. Bat buoc luu ket qua DRY-RUN truoc khi CALL.
-- Backfill duy nhat HoaDon.NgayDenHan con NULL tu ngay thanh toan hien tai cua hop dong.

-- DRY-RUN BAT BUOC (chi doc)
SELECT COUNT(*) AS TotalInvoices FROM HoaDon;

SELECT COUNT(*) AS InvalidDueDateSource
FROM HoaDon hd
LEFT JOIN HopDong h ON h.Id = hd.HopDongId
WHERE h.Id IS NULL
   OR hd.Thang NOT BETWEEN 1 AND 12
   OR hd.Nam NOT BETWEEN 2000 AND 2100
   OR h.NgayThanhToanHangThang NOT BETWEEN 1 AND 31;

SELECT hd.Id AS HoaDonId,
       hd.HopDongId,
       hd.Thang,
       hd.Nam,
       h.NgayThanhToanHangThang,
       DATE_ADD(
           DATE_ADD(STR_TO_DATE(CONCAT(hd.Nam, '-', LPAD(hd.Thang, 2, '0'), '-01'), '%Y-%m-%d'), INTERVAL 1 MONTH),
           INTERVAL (
               LEAST(
                   h.NgayThanhToanHangThang,
                   DAY(LAST_DAY(DATE_ADD(
                       STR_TO_DATE(CONCAT(hd.Nam, '-', LPAD(hd.Thang, 2, '0'), '-01'), '%Y-%m-%d'),
                       INTERVAL 1 MONTH)))
               ) - 1
           ) DAY
       ) AS ExpectedNgayDenHan
FROM HoaDon hd
INNER JOIN HopDong h ON h.Id = hd.HopDongId
ORDER BY hd.Id;

SELECT COUNT(*) AS NgayDenHanColumnCount
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'HoaDon'
  AND COLUMN_NAME = 'NgayDenHan';

DELIMITER $$
DROP PROCEDURE IF EXISTS ApplyReview017$$
CREATE PROCEDURE ApplyReview017()
BEGIN
    DECLARE AnomalyCount INT DEFAULT 0;

    SELECT COUNT(*) INTO AnomalyCount
    FROM HoaDon hd
    LEFT JOIN HopDong h ON h.Id = hd.HopDongId
    WHERE h.Id IS NULL
       OR hd.Thang NOT BETWEEN 1 AND 12
       OR hd.Nam NOT BETWEEN 2000 AND 2100
       OR h.NgayThanhToanHangThang NOT BETWEEN 1 AND 31;

    IF AnomalyCount > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'REVIEW-017 BLOCKED: ky hoa don hoac ngay thanh toan hop dong khong hop le; xem dry-run.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'HoaDon'
          AND COLUMN_NAME = 'NgayDenHan'
    ) THEN
        ALTER TABLE HoaDon
            ADD COLUMN NgayDenHan DATE NULL AFTER NgayLap;
    END IF;

    UPDATE HoaDon hd
    INNER JOIN HopDong h ON h.Id = hd.HopDongId
    SET hd.NgayDenHan = DATE_ADD(
        DATE_ADD(STR_TO_DATE(CONCAT(hd.Nam, '-', LPAD(hd.Thang, 2, '0'), '-01'), '%Y-%m-%d'), INTERVAL 1 MONTH),
        INTERVAL (
            LEAST(
                h.NgayThanhToanHangThang,
                DAY(LAST_DAY(DATE_ADD(
                    STR_TO_DATE(CONCAT(hd.Nam, '-', LPAD(hd.Thang, 2, '0'), '-01'), '%Y-%m-%d'),
                    INTERVAL 1 MONTH)))
            ) - 1
        ) DAY
    )
    WHERE hd.NgayDenHan IS NULL;

    SELECT COUNT(*) INTO AnomalyCount
    FROM HoaDon
    WHERE NgayDenHan IS NULL
       OR YEAR(NgayDenHan) NOT BETWEEN 2000 AND 2101
       OR NgayDenHan < DATE_ADD(
            STR_TO_DATE(CONCAT(Nam, '-', LPAD(Thang, 2, '0'), '-01'), '%Y-%m-%d'),
            INTERVAL 1 MONTH)
       OR NgayDenHan > LAST_DAY(DATE_ADD(
            STR_TO_DATE(CONCAT(Nam, '-', LPAD(Thang, 2, '0'), '-01'), '%Y-%m-%d'),
            INTERVAL 1 MONTH));

    IF AnomalyCount > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'REVIEW-017 BLOCKED: snapshot ngay den han NULL hoac nam ngoai thang N+1.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'HoaDon'
          AND COLUMN_NAME = 'NgayDenHan'
          AND IS_NULLABLE = 'YES'
    ) THEN
        ALTER TABLE HoaDon MODIFY COLUMN NgayDenHan DATE NOT NULL AFTER NgayLap;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = 'HoaDon'
          AND CONSTRAINT_NAME = 'CK_HoaDon_NgayDenHan'
          AND CONSTRAINT_TYPE = 'CHECK'
    ) THEN
        ALTER TABLE HoaDon
            ADD CONSTRAINT CK_HoaDon_NgayDenHan CHECK (
                YEAR(NgayDenHan) BETWEEN 2000 AND 2101
                AND NgayDenHan BETWEEN
                    DATE_ADD(STR_TO_DATE(CONCAT(Nam, '-', LPAD(Thang, 2, '0'), '-01'), '%Y-%m-%d'), INTERVAL 1 MONTH)
                    AND LAST_DAY(DATE_ADD(STR_TO_DATE(CONCAT(Nam, '-', LPAD(Thang, 2, '0'), '-01'), '%Y-%m-%d'), INTERVAL 1 MONTH))
            );
    END IF;
END$$

CALL ApplyReview017()$$
DROP PROCEDURE ApplyReview017$$
DELIMITER ;

SELECT COUNT(*) AS TotalInvoices,
       COALESCE(SUM(NgayDenHan IS NULL), 0) AS MissingNgayDenHan,
       COALESCE(SUM(
           NgayDenHan < DATE_ADD(
               STR_TO_DATE(CONCAT(Nam, '-', LPAD(Thang, 2, '0'), '-01'), '%Y-%m-%d'),
               INTERVAL 1 MONTH)
           OR NgayDenHan > LAST_DAY(DATE_ADD(
               STR_TO_DATE(CONCAT(Nam, '-', LPAD(Thang, 2, '0'), '-01'), '%Y-%m-%d'),
               INTERVAL 1 MONTH))
       ), 0) AS InvalidNgayDenHan
FROM HoaDon;

SELECT IS_NULLABLE, COLUMN_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'HoaDon'
  AND COLUMN_NAME = 'NgayDenHan';

SELECT COUNT(*) AS DueDateCheckCount
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = DATABASE()
  AND TABLE_NAME = 'HoaDon'
  AND CONSTRAINT_NAME = 'CK_HoaDon_NgayDenHan'
  AND CONSTRAINT_TYPE = 'CHECK';
