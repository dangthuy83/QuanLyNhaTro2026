-- REVIEW-013: dong bang danh tinh hien thi cua hoa don tai thoi diem chot.
-- BAT BUOC chay va luu khoi DRY-RUN truoc khi CALL ApplyReview013().
-- Script chi backfill snapshot; khong merge, xoa, hoac doi du lieu nghiep vu hien tai.

-- DRY-RUN BAT BUOC
SELECT COUNT(*) AS TotalInvoices FROM HoaDon;

SELECT hd.Id AS HoaDonId, hd.HopDongId
FROM HoaDon hd
LEFT JOIN HopDong h ON h.Id=hd.HopDongId
LEFT JOIN Phong p ON p.Id=h.PhongId
LEFT JOIN Nha n ON n.Id=p.NhaId
WHERE h.Id IS NULL OR p.Id IS NULL OR n.Id IS NULL;

SELECT hd.Id AS HoaDonId, hd.HopDongId, hd.Thang, hd.Nam
FROM HoaDon hd
LEFT JOIN HopDongKhachThue x
  ON x.HopDongId=hd.HopDongId AND x.LaDaiDien=1
 AND x.NgayBatDau<=LAST_DAY(STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
 AND (x.NgayKetThuc IS NULL OR x.NgayKetThuc>=STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
GROUP BY hd.Id, hd.HopDongId, hd.Thang, hd.Nam
HAVING COUNT(x.Id)=0;

SELECT DISTINCT hd.Id AS HoaDonId, hd.HopDongId, hd.Thang, hd.Nam
FROM HoaDon hd
INNER JOIN HopDongKhachThue a ON a.HopDongId=hd.HopDongId AND a.LaDaiDien=1
INNER JOIN HopDongKhachThue b ON b.HopDongId=hd.HopDongId AND b.LaDaiDien=1 AND b.Id>a.Id
WHERE a.NgayBatDau<=LAST_DAY(STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
  AND b.NgayBatDau<=LAST_DAY(STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
  AND (a.NgayKetThuc IS NULL OR a.NgayKetThuc>=STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
  AND (b.NgayKetThuc IS NULL OR b.NgayKetThuc>=STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
  AND a.NgayBatDau<=COALESCE(b.NgayKetThuc,'9999-12-31')
  AND b.NgayBatDau<=COALESCE(a.NgayKetThuc,'9999-12-31');

SELECT ct.Id AS ChiTietHoaDonId, ct.HoaDonId, ct.DichVuId
FROM ChiTietHoaDon ct LEFT JOIN DichVu dv ON dv.Id=ct.DichVuId
WHERE dv.Id IS NULL OR NULLIF(TRIM(dv.TenDichVu),'') IS NULL OR NULLIF(TRIM(dv.DonViTinh),'') IS NULL;

SELECT k.Id AS KhoanPhatSinhId, k.HoaDonId
FROM KhoanPhatSinhHopDong k
WHERE k.HoaDonId IS NOT NULL AND NULLIF(TRIM(k.MoTa),'') IS NULL;

DELIMITER $$
DROP PROCEDURE IF EXISTS ApplyReview013$$
CREATE PROCEDURE ApplyReview013()
BEGIN
    DECLARE AnomalyCount INT DEFAULT 0;

    SELECT COUNT(*) INTO AnomalyCount
    FROM HoaDon hd
    LEFT JOIN HopDong h ON h.Id=hd.HopDongId
    LEFT JOIN Phong p ON p.Id=h.PhongId
    LEFT JOIN Nha n ON n.Id=p.NhaId
    WHERE h.Id IS NULL OR p.Id IS NULL OR n.Id IS NULL;
    IF AnomalyCount>0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='REVIEW-013: hoa don thieu hop dong/phong/nha; xem dry-run.';
    END IF;

    SELECT COUNT(*) INTO AnomalyCount
    FROM (
        SELECT hd.Id
        FROM HoaDon hd
        LEFT JOIN HopDongKhachThue x
          ON x.HopDongId=hd.HopDongId AND x.LaDaiDien=1
         AND x.NgayBatDau<=LAST_DAY(STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
         AND (x.NgayKetThuc IS NULL OR x.NgayKetThuc>=STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
        GROUP BY hd.Id HAVING COUNT(x.Id)=0
    ) bad_representatives;
    IF AnomalyCount>0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='REVIEW-013: hoa don khong co dai dien trong ky; xem dry-run.';
    END IF;

    SELECT COUNT(*) INTO AnomalyCount FROM (
        SELECT DISTINCT hd.Id
        FROM HoaDon hd
        INNER JOIN HopDongKhachThue a ON a.HopDongId=hd.HopDongId AND a.LaDaiDien=1
        INNER JOIN HopDongKhachThue b ON b.HopDongId=hd.HopDongId AND b.LaDaiDien=1 AND b.Id>a.Id
        WHERE a.NgayBatDau<=LAST_DAY(STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
          AND b.NgayBatDau<=LAST_DAY(STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
          AND (a.NgayKetThuc IS NULL OR a.NgayKetThuc>=STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
          AND (b.NgayKetThuc IS NULL OR b.NgayKetThuc>=STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
          AND a.NgayBatDau<=COALESCE(b.NgayKetThuc,'9999-12-31')
          AND b.NgayBatDau<=COALESCE(a.NgayKetThuc,'9999-12-31')
    ) overlapping_representatives;
    IF AnomalyCount>0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='REVIEW-013: hoa don co dai dien chong thoi gian trong ky; xem dry-run.';
    END IF;

    SELECT COUNT(*) INTO AnomalyCount
    FROM ChiTietHoaDon ct LEFT JOIN DichVu dv ON dv.Id=ct.DichVuId
    WHERE dv.Id IS NULL OR NULLIF(TRIM(dv.TenDichVu),'') IS NULL OR NULLIF(TRIM(dv.DonViTinh),'') IS NULL;
    IF AnomalyCount>0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='REVIEW-013: chi tiet hoa don thieu ten/don vi dich vu; xem dry-run.';
    END IF;

    SELECT COUNT(*) INTO AnomalyCount
    FROM KhoanPhatSinhHopDong k
    WHERE k.HoaDonId IS NOT NULL AND NULLIF(TRIM(k.MoTa),'') IS NULL;
    IF AnomalyCount>0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='REVIEW-013: khoan phat sinh da gan hoa don co mo ta rong; xem dry-run.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='HoaDon' AND COLUMN_NAME='NhaIdSnapshot') THEN
        ALTER TABLE HoaDon
            ADD COLUMN NhaIdSnapshot INT NULL,
            ADD COLUMN TenNhaSnapshot VARCHAR(100) NULL,
            ADD COLUMN PhongIdSnapshot INT NULL,
            ADD COLUMN TenPhongSnapshot VARCHAR(50) NULL,
            ADD COLUMN KhachDaiDienIdSnapshot INT NULL,
            ADD COLUMN TenKhachDaiDienSnapshot VARCHAR(100) NULL,
            ADD COLUMN CccdKhachDaiDienSnapshot VARCHAR(20) NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ChiTietHoaDon' AND COLUMN_NAME='TenDichVuSnapshot') THEN
        ALTER TABLE ChiTietHoaDon
            ADD COLUMN TenDichVuSnapshot VARCHAR(100) NULL,
            ADD COLUMN DonViTinhSnapshot VARCHAR(30) NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='KhoanPhatSinhHopDong' AND COLUMN_NAME='MoTaHoaDonSnapshot') THEN
        ALTER TABLE KhoanPhatSinhHopDong
            ADD COLUMN MoTaHoaDonSnapshot VARCHAR(500) NULL,
            ADD COLUMN SoTienHoaDonSnapshot DECIMAL(12,0) NULL;
    END IF;

    UPDATE HoaDon hd
    INNER JOIN HopDong h ON h.Id=hd.HopDongId
    INNER JOIN Phong p ON p.Id=h.PhongId
    INNER JOIN Nha n ON n.Id=p.NhaId
    INNER JOIN HopDongKhachThue x ON x.Id=(
        SELECT x2.Id FROM HopDongKhachThue x2
        WHERE x2.HopDongId=hd.HopDongId AND x2.LaDaiDien=1
          AND x2.NgayBatDau<=LAST_DAY(STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
          AND (x2.NgayKetThuc IS NULL OR x2.NgayKetThuc>=STR_TO_DATE(CONCAT(hd.Nam,'-',LPAD(hd.Thang,2,'0'),'-01'),'%Y-%m-%d'))
        ORDER BY x2.NgayBatDau DESC,x2.Id DESC LIMIT 1)
    INNER JOIN KhachThue kt ON kt.Id=x.KhachThueId
    SET hd.NhaIdSnapshot=n.Id,
        hd.TenNhaSnapshot=n.TenNha,
        hd.PhongIdSnapshot=p.Id,
        hd.TenPhongSnapshot=p.TenPhong,
        hd.KhachDaiDienIdSnapshot=kt.Id,
        hd.TenKhachDaiDienSnapshot=kt.HoTen,
        hd.CccdKhachDaiDienSnapshot=kt.CCCD
    WHERE hd.NhaIdSnapshot IS NULL OR hd.TenNhaSnapshot IS NULL
       OR hd.PhongIdSnapshot IS NULL OR hd.TenPhongSnapshot IS NULL
       OR hd.KhachDaiDienIdSnapshot IS NULL OR hd.TenKhachDaiDienSnapshot IS NULL;

    UPDATE ChiTietHoaDon ct
    INNER JOIN DichVu dv ON dv.Id=ct.DichVuId
    SET ct.TenDichVuSnapshot=dv.TenDichVu,
        ct.DonViTinhSnapshot=dv.DonViTinh
    WHERE ct.TenDichVuSnapshot IS NULL OR ct.DonViTinhSnapshot IS NULL;

    UPDATE KhoanPhatSinhHopDong k
    SET k.MoTaHoaDonSnapshot=k.MoTa,
        k.SoTienHoaDonSnapshot=CASE WHEN k.SoTien-k.SoTienDaXuLy>0 THEN k.SoTien-k.SoTienDaXuLy ELSE k.SoTien END
    WHERE k.HoaDonId IS NOT NULL
      AND (k.MoTaHoaDonSnapshot IS NULL OR k.SoTienHoaDonSnapshot IS NULL);

    SELECT COUNT(*) INTO AnomalyCount FROM HoaDon
    WHERE NhaIdSnapshot IS NULL OR NULLIF(TRIM(TenNhaSnapshot),'') IS NULL
       OR PhongIdSnapshot IS NULL OR NULLIF(TRIM(TenPhongSnapshot),'') IS NULL
       OR KhachDaiDienIdSnapshot IS NULL OR NULLIF(TRIM(TenKhachDaiDienSnapshot),'') IS NULL;
    IF AnomalyCount>0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='REVIEW-013: backfill nhan dien hoa don chua day du.';
    END IF;

    SELECT COUNT(*) INTO AnomalyCount FROM ChiTietHoaDon
    WHERE NULLIF(TRIM(TenDichVuSnapshot),'') IS NULL OR NULLIF(TRIM(DonViTinhSnapshot),'') IS NULL;
    IF AnomalyCount>0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='REVIEW-013: backfill dich vu hoa don chua day du.';
    END IF;

    ALTER TABLE HoaDon
        MODIFY COLUMN NhaIdSnapshot INT NOT NULL,
        MODIFY COLUMN TenNhaSnapshot VARCHAR(100) NOT NULL,
        MODIFY COLUMN PhongIdSnapshot INT NOT NULL,
        MODIFY COLUMN TenPhongSnapshot VARCHAR(50) NOT NULL,
        MODIFY COLUMN KhachDaiDienIdSnapshot INT NOT NULL,
        MODIFY COLUMN TenKhachDaiDienSnapshot VARCHAR(100) NOT NULL;

    ALTER TABLE ChiTietHoaDon
        MODIFY COLUMN TenDichVuSnapshot VARCHAR(100) NOT NULL,
        MODIFY COLUMN DonViTinhSnapshot VARCHAR(30) NOT NULL;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='KhoanPhatSinhHopDong'
          AND CONSTRAINT_NAME='CK_KhoanPhatSinh_HoaDonSnapshot'
    ) THEN
        ALTER TABLE KhoanPhatSinhHopDong
            ADD CONSTRAINT CK_KhoanPhatSinh_HoaDonSnapshot CHECK (
                HoaDonId IS NULL OR (MoTaHoaDonSnapshot IS NOT NULL AND SoTienHoaDonSnapshot>0));
    END IF;
END$$
CALL ApplyReview013()$$
DROP PROCEDURE ApplyReview013$$
DELIMITER ;

SELECT COUNT(*) AS TotalInvoices,
       SUM(NhaIdSnapshot IS NULL OR PhongIdSnapshot IS NULL OR KhachDaiDienIdSnapshot IS NULL) AS MissingInvoiceIdentity
FROM HoaDon;
SELECT COUNT(*) AS TotalDetails,
       SUM(TenDichVuSnapshot IS NULL OR DonViTinhSnapshot IS NULL) AS MissingServiceIdentity
FROM ChiTietHoaDon;
SELECT COUNT(*) AS LinkedCharges,
       SUM(HoaDonId IS NOT NULL AND (MoTaHoaDonSnapshot IS NULL OR SoTienHoaDonSnapshot IS NULL)) AS MissingChargeSnapshot
FROM KhoanPhatSinhHopDong;
