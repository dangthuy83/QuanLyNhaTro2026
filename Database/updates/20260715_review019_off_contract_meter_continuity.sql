-- REVIEW-019: continuity va reset cho chi so ngoai hop dong.
-- Apply-once, rerun-safe. Khong tu sua/xoa du lieu nghiep vu.
-- Bat buoc luu ket qua DRY-RUN va xu ly blocker truoc khi CALL.

-- DRY-RUN BAT BUOC (chi doc)
SELECT COUNT(*) AS TotalOffContractReadings FROM ChiSoNgoaiHopDong;
SELECT COUNT(*) AS TotalContractReadings FROM ChiSoDienNuoc;

WITH MeterEvents AS (
    SELECT PhongId,DichVuId,NgayGhiNhan EventDate,'NHD' SourceType,Id SourceId,
           TuChiSo StartReading,DenChiSo EndReading
    FROM ChiSoNgoaiHopDong
    UNION ALL
    SELECT PhongId,DichVuId,NgayDoc,'HD',Id,ChiSoDau,ChiSoCuoi
    FROM ChiSoDienNuoc
), OrderedEvents AS (
    SELECT e.*,
           LAG(EndReading) OVER (
               PARTITION BY PhongId,DichVuId
               ORDER BY EventDate,CASE SourceType WHEN 'HD' THEN 0 ELSE 1 END,SourceId
           ) PrevEnd,
           LEAD(StartReading) OVER (
               PARTITION BY PhongId,DichVuId
               ORDER BY EventDate,CASE SourceType WHEN 'HD' THEN 0 ELSE 1 END,SourceId
           ) NextStart
    FROM MeterEvents e
)
SELECT
    COALESCE(SUM(SourceType='NHD' AND PrevEnd IS NOT NULL AND StartReading<>PrevEnd),0) AS OffStartMismatch,
    COALESCE(SUM(SourceType='NHD' AND NextStart IS NOT NULL AND EndReading<>NextStart),0) AS OffEndMismatch
FROM OrderedEvents;

SELECT COUNT(*) AS SameDateGroups
FROM (
    SELECT PhongId,DichVuId,EventDate,COUNT(*) RowCount
    FROM (
        SELECT PhongId,DichVuId,NgayGhiNhan EventDate FROM ChiSoNgoaiHopDong
        UNION ALL
        SELECT PhongId,DichVuId,NgayDoc FROM ChiSoDienNuoc
    ) e
    GROUP BY PhongId,DichVuId,EventDate
    HAVING COUNT(*)>1
) x;

SELECT COUNT(*) AS OffRowsInsideContract
FROM ChiSoNgoaiHopDong n
WHERE EXISTS (
    SELECT 1
    FROM HopDong h
    WHERE h.PhongId=n.PhongId
      AND h.TrangThai<>'DaHuy'
      AND n.NgayGhiNhan BETWEEN h.NgayBatDau AND COALESCE(h.NgayKetThuc,'9999-12-31')
);

SELECT Id,PhongId,DichVuId,NgayGhiNhan,TuChiSo,DenChiSo,LyDo,GhiChu
FROM ChiSoNgoaiHopDong
WHERE LOWER(CONCAT_WS(' ',LyDo,GhiChu)) REGEXP 'reset|thay.*dong ho|dong ho.*thay|hong.*dong ho|quay vong'
ORDER BY NgayGhiNhan,Id;

DELIMITER $$
DROP PROCEDURE IF EXISTS ApplyReview019$$
CREATE PROCEDURE ApplyReview019()
BEGIN
    DECLARE AnomalyCount INT DEFAULT 0;

    WITH MeterEvents AS (
        SELECT PhongId,DichVuId,NgayGhiNhan EventDate,'NHD' SourceType,Id SourceId,
               TuChiSo StartReading,DenChiSo EndReading
        FROM ChiSoNgoaiHopDong
        UNION ALL
        SELECT PhongId,DichVuId,NgayDoc,'HD',Id,ChiSoDau,ChiSoCuoi
        FROM ChiSoDienNuoc
    ), OrderedEvents AS (
        SELECT e.*,
               LAG(EndReading) OVER (
                   PARTITION BY PhongId,DichVuId
                   ORDER BY EventDate,CASE SourceType WHEN 'HD' THEN 0 ELSE 1 END,SourceId
               ) PrevEnd,
               LEAD(StartReading) OVER (
                   PARTITION BY PhongId,DichVuId
                   ORDER BY EventDate,CASE SourceType WHEN 'HD' THEN 0 ELSE 1 END,SourceId
               ) NextStart
        FROM MeterEvents e
    )
    SELECT COUNT(*) INTO AnomalyCount
    FROM OrderedEvents
    WHERE SourceType='NHD'
      AND ((PrevEnd IS NOT NULL AND StartReading<>PrevEnd)
        OR (NextStart IS NOT NULL AND EndReading<>NextStart));

    IF AnomalyCount > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'REVIEW-019 BLOCKED: chuoi chi so ngoai hop dong co gap hoac khong noi dong phia sau.';
    END IF;

    SELECT COUNT(*) INTO AnomalyCount
    FROM (
        SELECT PhongId,DichVuId,EventDate,COUNT(*) RowCount
        FROM (
            SELECT PhongId,DichVuId,NgayGhiNhan EventDate FROM ChiSoNgoaiHopDong
            UNION ALL
            SELECT PhongId,DichVuId,NgayDoc FROM ChiSoDienNuoc
        ) e
        GROUP BY PhongId,DichVuId,EventDate
        HAVING COUNT(*)>1
    ) x;

    IF AnomalyCount > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'REVIEW-019 BLOCKED: co nhieu moc cung phong/dich vu/ngay.';
    END IF;

    SELECT COUNT(*) INTO AnomalyCount
    FROM ChiSoNgoaiHopDong n
    WHERE EXISTS (
        SELECT 1 FROM HopDong h
        WHERE h.PhongId=n.PhongId
          AND h.TrangThai<>'DaHuy'
          AND n.NgayGhiNhan BETWEEN h.NgayBatDau AND COALESCE(h.NgayKetThuc,'9999-12-31')
    );

    IF AnomalyCount > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'REVIEW-019 BLOCKED: chi so ngoai hop dong nam trong thoi gian hop dong.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ChiSoNgoaiHopDong' AND COLUMN_NAME='LoaiGhiNhan'
    ) THEN
        ALTER TABLE ChiSoNgoaiHopDong
            ADD COLUMN LoaiGhiNhan VARCHAR(20) NOT NULL DEFAULT 'BinhThuong' AFTER DenChiSo;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ChiSoNgoaiHopDong' AND COLUMN_NAME='ChiSoTruocReset'
    ) THEN
        ALTER TABLE ChiSoNgoaiHopDong
            ADD COLUMN ChiSoTruocReset DECIMAL(10,2) NULL AFTER LoaiGhiNhan;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ChiSoNgoaiHopDong' AND COLUMN_NAME='ChiSoSauReset'
    ) THEN
        ALTER TABLE ChiSoNgoaiHopDong
            ADD COLUMN ChiSoSauReset DECIMAL(10,2) NULL AFTER ChiSoTruocReset;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ChiSoNgoaiHopDong' AND COLUMN_NAME='LyDoDieuChinh'
    ) THEN
        ALTER TABLE ChiSoNgoaiHopDong
            ADD COLUMN LyDoDieuChinh VARCHAR(255) NULL AFTER ChiSoSauReset;
    END IF;

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='ChiSoNgoaiHopDong'
          AND CONSTRAINT_NAME='CK_ChiSoNgoaiHopDong_SanLuong'
    ) THEN
        ALTER TABLE ChiSoNgoaiHopDong DROP CHECK CK_ChiSoNgoaiHopDong_SanLuong;
    END IF;

    ALTER TABLE ChiSoNgoaiHopDong
        MODIFY COLUMN SanLuong DECIMAL(10,2) GENERATED ALWAYS AS (
            CASE
                WHEN LoaiGhiNhan='Reset' THEN
                    (ChiSoTruocReset-TuChiSo)+(DenChiSo-COALESCE(ChiSoSauReset,0))
                ELSE DenChiSo-TuChiSo
            END
        ) STORED;

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='ChiSoNgoaiHopDong'
          AND CONSTRAINT_NAME='CK_ChiSoNgoaiHopDong_KhongAm'
    ) THEN
        ALTER TABLE ChiSoNgoaiHopDong DROP CHECK CK_ChiSoNgoaiHopDong_KhongAm;
    END IF;
    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='ChiSoNgoaiHopDong'
          AND CONSTRAINT_NAME='CK_ChiSoNgoaiHopDong_Loai'
    ) THEN
        ALTER TABLE ChiSoNgoaiHopDong DROP CHECK CK_ChiSoNgoaiHopDong_Loai;
    END IF;
    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='ChiSoNgoaiHopDong'
          AND CONSTRAINT_NAME='CK_ChiSoNgoaiHopDong_SanLuongHopLe'
    ) THEN
        ALTER TABLE ChiSoNgoaiHopDong DROP CHECK CK_ChiSoNgoaiHopDong_SanLuongHopLe;
    END IF;

    ALTER TABLE ChiSoNgoaiHopDong
        ADD CONSTRAINT CK_ChiSoNgoaiHopDong_Loai
            CHECK (LoaiGhiNhan IN ('BinhThuong','Reset')),
        ADD CONSTRAINT CK_ChiSoNgoaiHopDong_KhongAm
            CHECK (
                TuChiSo>=0 AND DenChiSo>=0
                AND (ChiSoTruocReset IS NULL OR ChiSoTruocReset>=0)
                AND (ChiSoSauReset IS NULL OR ChiSoSauReset>=0)
            ),
        ADD CONSTRAINT CK_ChiSoNgoaiHopDong_SanLuongHopLe
            CHECK (
                (LoaiGhiNhan='BinhThuong'
                 AND DenChiSo>=TuChiSo
                 AND ChiSoTruocReset IS NULL
                 AND ChiSoSauReset IS NULL
                 AND LyDoDieuChinh IS NULL)
                OR
                (LoaiGhiNhan='Reset'
                 AND ChiSoTruocReset IS NOT NULL
                 AND ChiSoTruocReset>=TuChiSo
                 AND DenChiSo>=COALESCE(ChiSoSauReset,0)
                 AND LyDoDieuChinh IS NOT NULL
                 AND TRIM(LyDoDieuChinh)<>'')
            );
END$$

CALL ApplyReview019()$$
DROP PROCEDURE ApplyReview019$$
DELIMITER ;

SELECT COUNT(*) AS ResetColumns
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA=DATABASE()
  AND TABLE_NAME='ChiSoNgoaiHopDong'
  AND COLUMN_NAME IN ('LoaiGhiNhan','ChiSoTruocReset','ChiSoSauReset','LyDoDieuChinh');

SELECT COUNT(*) AS Review019Checks
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA=DATABASE()
  AND TABLE_NAME='ChiSoNgoaiHopDong'
  AND CONSTRAINT_NAME IN (
      'CK_ChiSoNgoaiHopDong_Loai',
      'CK_ChiSoNgoaiHopDong_KhongAm',
      'CK_ChiSoNgoaiHopDong_SanLuongHopLe'
  )
  AND CONSTRAINT_TYPE='CHECK';
