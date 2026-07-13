-- REVIEW-014 - unique CCCD sau TRIM va guard apply-once.
-- Dry-run database read-only; filesystem duoc doi chieu bo sung boi audit runner.
SELECT COUNT(*) AS TotalKhachThue,
       SUM(NULLIF(TRIM(CCCD), '') IS NULL) AS BlankOrNullCCCD,
       SUM(NULLIF(TRIM(AnhCCCDMatTruoc), '') IS NOT NULL
           OR NULLIF(TRIM(AnhCCCDMatSau), '') IS NOT NULL) AS ProfilesWithPhotoPath
FROM KhachThue;

SELECT TRIM(CCCD) AS NormalizedCCCD, COUNT(*) AS ProfileCount,
       GROUP_CONCAT(Id ORDER BY Id) AS ProfileIds
FROM KhachThue
WHERE NULLIF(TRIM(CCCD), '') IS NOT NULL
GROUP BY TRIM(CCCD) HAVING COUNT(*) > 1
ORDER BY TRIM(CCCD);

SELECT TRIM(SoDienThoai) AS NormalizedPhone, COUNT(*) AS ProfileCount,
       GROUP_CONCAT(Id ORDER BY Id) AS ProfileIds
FROM KhachThue
WHERE NULLIF(TRIM(SoDienThoai), '') IS NOT NULL
GROUP BY TRIM(SoDienThoai) HAVING COUNT(*) > 1
ORDER BY TRIM(SoDienThoai);

SELECT PhotoPath, COUNT(DISTINCT ProfileId) AS ProfileCount,
       GROUP_CONCAT(DISTINCT ProfileId ORDER BY ProfileId) AS ProfileIds
FROM (
    SELECT Id AS ProfileId, TRIM(AnhCCCDMatTruoc) AS PhotoPath
    FROM KhachThue WHERE NULLIF(TRIM(AnhCCCDMatTruoc), '') IS NOT NULL
    UNION ALL
    SELECT Id AS ProfileId, TRIM(AnhCCCDMatSau) AS PhotoPath
    FROM KhachThue WHERE NULLIF(TRIM(AnhCCCDMatSau), '') IS NOT NULL
) photos
GROUP BY PhotoPath HAVING COUNT(DISTINCT ProfileId) > 1
ORDER BY PhotoPath;

DELIMITER $$
DROP PROCEDURE IF EXISTS ApplyTenantIdentityPhotoLifecycle$$
CREATE PROCEDURE ApplyTenantIdentityPhotoLifecycle()
BEGIN
    DECLARE DuplicateCccdGroups INT DEFAULT 0;
    SELECT COUNT(*) INTO DuplicateCccdGroups
    FROM (
        SELECT TRIM(CCCD) FROM KhachThue
        WHERE NULLIF(TRIM(CCCD), '') IS NOT NULL
        GROUP BY TRIM(CCCD) HAVING COUNT(*) > 1
    ) duplicate_groups;

    IF DuplicateCccdGroups > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'REVIEW-014 blocked: duplicate CCCD groups after TRIM require manual resolution.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='KhachThue' AND COLUMN_NAME='CCCDNormalized'
    ) THEN
        ALTER TABLE KhachThue
            ADD COLUMN CCCDNormalized VARCHAR(20)
            GENERATED ALWAYS AS (NULLIF(TRIM(CCCD), '')) STORED AFTER CCCD;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='KhachThue'
          AND INDEX_NAME='UQ_KhachThue_CCCD_Normalized'
    ) THEN
        ALTER TABLE KhachThue
            ADD CONSTRAINT UQ_KhachThue_CCCD_Normalized UNIQUE (CCCDNormalized);
    END IF;
END$$
CALL ApplyTenantIdentityPhotoLifecycle()$$
DROP PROCEDURE ApplyTenantIdentityPhotoLifecycle$$
DELIMITER ;

SELECT COUNT(*) AS NormalizedColumns
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='KhachThue' AND COLUMN_NAME='CCCDNormalized';

SELECT COUNT(*) AS UniqueConstraints
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='KhachThue'
  AND CONSTRAINT_NAME='UQ_KhachThue_CCCD_Normalized' AND CONSTRAINT_TYPE='UNIQUE';
