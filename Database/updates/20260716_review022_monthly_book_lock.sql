-- REVIEW-022: khoa so ThuChi theo thang va but toan dieu chinh tham chieu ban goc.
-- Apply-once, rerun-safe. Khong sua/xoa/backfill giao dich nghiep vu.
-- BAT BUOC luu DRY-RUN va xu ly blocker truoc khi CALL.

-- DRY-RUN BAT BUOC (chi doc)
SELECT COUNT(*) AS InvalidThuChiRows
FROM ThuChi
WHERE LoaiGiaoDich NOT IN ('Thu','Chi') OR SoTien <= 0
   OR YEAR(NgayPhatSinh) NOT BETWEEN 2000 AND 2100;

SELECT COUNT(*) AS ExistingCorrectionColumn
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ThuChi' AND COLUMN_NAME='ThuChiGocId';

SELECT COUNT(*) AS ExistingPeriodTable
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ThuChiKySo';

SELECT TRIGGER_NAME
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE TRIGGER_SCHEMA=DATABASE() AND EVENT_OBJECT_TABLE='ThuChi'
ORDER BY TRIGGER_NAME;

DELIMITER $$
DROP PROCEDURE IF EXISTS ApplyReview022$$
CREATE PROCEDURE ApplyReview022()
BEGIN
    DECLARE AnomalyCount INT DEFAULT 0;

    SELECT COUNT(*) INTO AnomalyCount
    FROM ThuChi
    WHERE LoaiGiaoDich NOT IN ('Thu','Chi') OR SoTien <= 0
       OR YEAR(NgayPhatSinh) NOT BETWEEN 2000 AND 2100;
    IF AnomalyCount > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'REVIEW-022 BLOCKED: ThuChi co loai, so tien hoac ngay khong hop le.';
    END IF;

    CREATE TABLE IF NOT EXISTS ThuChiKySo (
        Nam SMALLINT NOT NULL,
        Thang TINYINT NOT NULL,
        TrangThai VARCHAR(20) NOT NULL DEFAULT 'Mo',
        KhoaLuc DATETIME NULL,
        GhiChu VARCHAR(500) NULL,
        PRIMARY KEY (Nam, Thang),
        CONSTRAINT CK_ThuChiKySo_Ky CHECK (Thang BETWEEN 1 AND 12 AND Nam BETWEEN 2000 AND 2100),
        CONSTRAINT CK_ThuChiKySo_TrangThai CHECK (TrangThai IN ('Mo', 'DaKhoa')),
        CONSTRAINT CK_ThuChiKySo_KhoaLuc CHECK (
            (TrangThai = 'Mo' AND KhoaLuc IS NULL) OR
            (TrangThai = 'DaKhoa' AND KhoaLuc IS NOT NULL)
        )
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ThuChi' AND COLUMN_NAME='ThuChiGocId'
    ) THEN
        ALTER TABLE ThuChi ADD COLUMN ThuChiGocId INT NULL AFTER GhiChu;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ThuChi' AND INDEX_NAME='IX_ThuChi_Goc'
    ) THEN
        CREATE INDEX IX_ThuChi_Goc ON ThuChi(ThuChiGocId);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='ThuChi'
          AND CONSTRAINT_NAME='FK_ThuChi_Goc' AND CONSTRAINT_TYPE='FOREIGN KEY'
    ) THEN
        ALTER TABLE ThuChi ADD CONSTRAINT FK_ThuChi_Goc
            FOREIGN KEY (ThuChiGocId) REFERENCES ThuChi(Id);
    END IF;

END$$

CALL ApplyReview022()$$
DROP PROCEDURE ApplyReview022$$

DROP TRIGGER IF EXISTS TR_ThuChi_OpenPeriod_Insert$$
CREATE TRIGGER TR_ThuChi_OpenPeriod_Insert
BEFORE INSERT ON ThuChi
FOR EACH ROW
BEGIN
    DECLARE PeriodState VARCHAR(20);
    IF NEW.Id>0 AND NEW.ThuChiGocId=NEW.Id THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT='REVIEW-022: giao dich dieu chinh khong duoc tu tham chieu.';
    END IF;
    INSERT INTO ThuChiKySo(Nam,Thang,TrangThai)
    VALUES(YEAR(NEW.NgayPhatSinh),MONTH(NEW.NgayPhatSinh),'Mo')
    ON DUPLICATE KEY UPDATE Nam=VALUES(Nam);
    SELECT TrangThai INTO PeriodState FROM ThuChiKySo
    WHERE Nam=YEAR(NEW.NgayPhatSinh) AND Thang=MONTH(NEW.NgayPhatSinh) FOR UPDATE;
    IF PeriodState='DaKhoa' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT='REVIEW-022: thang thu chi da khoa; khong the them giao dich.';
    END IF;
END$$

DROP TRIGGER IF EXISTS TR_ThuChi_OpenPeriod_Update$$
CREATE TRIGGER TR_ThuChi_OpenPeriod_Update
BEFORE UPDATE ON ThuChi
FOR EACH ROW
BEGIN
    DECLARE PeriodState VARCHAR(20);
    DECLARE OldKey INT;
    DECLARE NewKey INT;
    SET OldKey=YEAR(OLD.NgayPhatSinh)*100+MONTH(OLD.NgayPhatSinh);
    SET NewKey=YEAR(NEW.NgayPhatSinh)*100+MONTH(NEW.NgayPhatSinh);
    IF NEW.ThuChiGocId=OLD.Id THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT='REVIEW-022: giao dich dieu chinh khong duoc tu tham chieu.';
    END IF;

    IF OldKey<=NewKey THEN
        INSERT INTO ThuChiKySo(Nam,Thang,TrangThai)
        VALUES(YEAR(OLD.NgayPhatSinh),MONTH(OLD.NgayPhatSinh),'Mo')
        ON DUPLICATE KEY UPDATE Nam=VALUES(Nam);
        SELECT TrangThai INTO PeriodState FROM ThuChiKySo
        WHERE Nam=YEAR(OLD.NgayPhatSinh) AND Thang=MONTH(OLD.NgayPhatSinh) FOR UPDATE;
        IF PeriodState='DaKhoa' THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='REVIEW-022: thang goc da khoa; khong the sua.';
        END IF;
        IF NewKey<>OldKey THEN
            INSERT INTO ThuChiKySo(Nam,Thang,TrangThai)
            VALUES(YEAR(NEW.NgayPhatSinh),MONTH(NEW.NgayPhatSinh),'Mo')
            ON DUPLICATE KEY UPDATE Nam=VALUES(Nam);
            SELECT TrangThai INTO PeriodState FROM ThuChiKySo
            WHERE Nam=YEAR(NEW.NgayPhatSinh) AND Thang=MONTH(NEW.NgayPhatSinh) FOR UPDATE;
            IF PeriodState='DaKhoa' THEN
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='REVIEW-022: thang dich da khoa; khong the chuyen.';
            END IF;
        END IF;
    ELSE
        INSERT INTO ThuChiKySo(Nam,Thang,TrangThai)
        VALUES(YEAR(NEW.NgayPhatSinh),MONTH(NEW.NgayPhatSinh),'Mo')
        ON DUPLICATE KEY UPDATE Nam=VALUES(Nam);
        SELECT TrangThai INTO PeriodState FROM ThuChiKySo
        WHERE Nam=YEAR(NEW.NgayPhatSinh) AND Thang=MONTH(NEW.NgayPhatSinh) FOR UPDATE;
        IF PeriodState='DaKhoa' THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='REVIEW-022: thang dich da khoa; khong the chuyen.';
        END IF;
        INSERT INTO ThuChiKySo(Nam,Thang,TrangThai)
        VALUES(YEAR(OLD.NgayPhatSinh),MONTH(OLD.NgayPhatSinh),'Mo')
        ON DUPLICATE KEY UPDATE Nam=VALUES(Nam);
        SELECT TrangThai INTO PeriodState FROM ThuChiKySo
        WHERE Nam=YEAR(OLD.NgayPhatSinh) AND Thang=MONTH(OLD.NgayPhatSinh) FOR UPDATE;
        IF PeriodState='DaKhoa' THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='REVIEW-022: thang goc da khoa; khong the sua.';
        END IF;
    END IF;
END$$

DROP TRIGGER IF EXISTS TR_ThuChi_OpenPeriod_Delete$$
CREATE TRIGGER TR_ThuChi_OpenPeriod_Delete
BEFORE DELETE ON ThuChi
FOR EACH ROW
BEGIN
    DECLARE PeriodState VARCHAR(20);
    INSERT INTO ThuChiKySo(Nam,Thang,TrangThai)
    VALUES(YEAR(OLD.NgayPhatSinh),MONTH(OLD.NgayPhatSinh),'Mo')
    ON DUPLICATE KEY UPDATE Nam=VALUES(Nam);
    SELECT TrangThai INTO PeriodState FROM ThuChiKySo
    WHERE Nam=YEAR(OLD.NgayPhatSinh) AND Thang=MONTH(OLD.NgayPhatSinh) FOR UPDATE;
    IF PeriodState='DaKhoa' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT='REVIEW-022: thang thu chi da khoa; khong the xoa.';
    END IF;
END$$
DELIMITER ;

-- POST-CHECK
SELECT COUNT(*) AS PeriodTableReady
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ThuChiKySo';
SELECT COUNT(*) AS CorrectionColumnReady
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ThuChi' AND COLUMN_NAME='ThuChiGocId';
SELECT COUNT(*) AS LockTriggerCount
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE TRIGGER_SCHEMA=DATABASE() AND EVENT_OBJECT_TABLE='ThuChi'
  AND TRIGGER_NAME IN ('TR_ThuChi_OpenPeriod_Insert','TR_ThuChi_OpenPeriod_Update','TR_ThuChi_OpenPeriod_Delete');
