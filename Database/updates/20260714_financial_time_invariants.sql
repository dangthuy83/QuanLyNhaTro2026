-- REVIEW-016: rang buoc nam nghiep vu, tien, trang thai va overlap.
-- Apply-once, rerun-safe. Script chi doc de bao cao/block; khong sua du lieu nghiep vu.
-- Bat buoc luu ket qua DRY-RUN. Neu bat ky ViolationCount > 0, dung va xu ly ngoai script.

SELECT 'Phong' AS ViolationGroup, COUNT(*) AS ViolationCount
FROM Phong WHERE GiaThueMacDinh < 0 OR TrangThai NOT IN ('Trong','DangThue','DangSuaChua')
UNION ALL SELECT 'DichVu', COUNT(*) FROM DichVu WHERE DonGiaMacDinh < 0
UNION ALL SELECT 'LichSuHinhThucDichVu', COUNT(*) FROM LichSuHinhThucDichVu
 WHERE YEAR(KyApDung) NOT BETWEEN 2000 AND 2100
UNION ALL SELECT 'PhongDichVu', COUNT(*) FROM PhongDichVu WHERE DonGia < 0
UNION ALL SELECT 'HopDong', COUNT(*) FROM HopDong
 WHERE YEAR(NgayBatDau) NOT BETWEEN 2000 AND 2100
    OR (NgayKetThuc IS NOT NULL AND YEAR(NgayKetThuc) NOT BETWEEN 2000 AND 2100)
    OR (NgayTraPhongThucTe IS NOT NULL AND
        (YEAR(NgayTraPhongThucTe) NOT BETWEEN 2000 AND 2100 OR NgayTraPhongThucTe < NgayBatDau))
    OR TienThueThoaThuan <= 0 OR TienCoc < 0 OR TienCocHoanLai < 0
    OR NgayThanhToanHangThang NOT BETWEEN 1 AND 31
UNION ALL SELECT 'HopDongKhachThue', COUNT(*) FROM HopDongKhachThue
 WHERE YEAR(NgayBatDau) NOT BETWEEN 2000 AND 2100
    OR (NgayKetThucDuKien IS NOT NULL AND YEAR(NgayKetThucDuKien) NOT BETWEEN 2000 AND 2100)
    OR (NgayKetThuc IS NOT NULL AND YEAR(NgayKetThuc) NOT BETWEEN 2000 AND 2100)
UNION ALL SELECT 'HopDongDichVu', COUNT(*) FROM HopDongDichVu
 WHERE YEAR(KyBatDau) NOT BETWEEN 2000 AND 2100
    OR (KyKetThuc IS NOT NULL AND YEAR(KyKetThuc) NOT BETWEEN 2000 AND 2100)
UNION ALL SELECT 'ChiSoDienNuoc', COUNT(*) FROM ChiSoDienNuoc
 WHERE Thang NOT BETWEEN 1 AND 12 OR Nam NOT BETWEEN 2000 AND 2100 OR NgayDoc IS NULL
    OR YEAR(NgayDoc) <> Nam OR MONTH(NgayDoc) <> Thang OR YEAR(NgayDoc) NOT BETWEEN 2000 AND 2100
UNION ALL SELECT 'ChiSoNgoaiHopDong', COUNT(*) FROM ChiSoNgoaiHopDong
 WHERE YEAR(NgayGhiNhan) NOT BETWEEN 2000 AND 2100
UNION ALL SELECT 'HoaDon', COUNT(*) FROM HoaDon
 WHERE Thang NOT BETWEEN 1 AND 12 OR Nam NOT BETWEEN 2000 AND 2100
    OR YEAR(NgayLap) NOT BETWEEN 2000 AND 2100
    OR (NgayThuThucTe IS NOT NULL AND YEAR(NgayThuThucTe) NOT BETWEEN 2000 AND 2100)
    OR TienPhong < 0 OR TongTienDichVu < 0 OR TongTienPhatSinh < 0 OR TienNoKyTruoc < 0
    OR TongCong < 0 OR SoTienDaThu < 0 OR SoTienDaThu > TongCong
    OR TongCong <> TienPhong + TongTienDichVu + TongTienPhatSinh + TienNoKyTruoc
    OR NOT ((SoTienDaThu = 0 AND TrangThaiThanhToan = 'ChuaThu')
        OR (SoTienDaThu > 0 AND SoTienDaThu < TongCong AND TrangThaiThanhToan = 'ThuMotPhan')
        OR (SoTienDaThu = TongCong AND SoTienDaThu > 0 AND TrangThaiThanhToan = 'DaThu'))
    OR NOT ((SoNgayO IS NULL AND SoNgayTrongThang IS NULL)
        OR (SoNgayO > 0 AND SoNgayTrongThang BETWEEN 28 AND 31 AND SoNgayO <= SoNgayTrongThang
            AND SoNgayTrongThang = DAY(LAST_DAY(STR_TO_DATE(CONCAT(Nam,'-',LPAD(Thang,2,'0'),'-01'),'%Y-%m-%d')))))
UNION ALL SELECT 'ChiTietHoaDon', COUNT(*) FROM ChiTietHoaDon
 WHERE SoLuong < 0 OR DonGia < 0 OR ThanhTien < 0 OR ThanhTien <> ROUND(SoLuong * DonGia, 0)
UNION ALL SELECT 'ThanhToan', COUNT(*) FROM ThanhToan
 WHERE SoTien <= 0 OR HinhThuc IS NULL
    OR HinhThuc NOT IN ('TienMat','ChuyenKhoan','KetChuyenNo','TruCoc')
    OR YEAR(NgayThu) NOT BETWEEN 2000 AND 2100
UNION ALL SELECT 'GiaoDichCoc', COUNT(*) FROM GiaoDichCoc
 WHERE NOT ((LoaiGiaoDich IN ('ThuCoc','ThuThemCoc') AND SoTien > 0)
        OR (LoaiGiaoDich IN ('HoanCoc','TruNo') AND SoTien < 0)
        OR (LoaiGiaoDich = 'DieuChinh' AND SoTien <> 0))
    OR YEAR(NgayGiaoDich) NOT BETWEEN 2000 AND 2100
    OR (HoaDonId IS NOT NULL AND LoaiGiaoDich <> 'TruNo')
UNION ALL SELECT 'KhoanPhatSinhHopDong', COUNT(*) FROM KhoanPhatSinhHopDong
 WHERE YEAR(NgayPhatSinh) NOT BETWEEN 2000 AND 2100
UNION ALL SELECT 'ThuChi', COUNT(*) FROM ThuChi
 WHERE LoaiGiaoDich NOT IN ('Thu','Chi') OR SoTien <= 0 OR YEAR(NgayPhatSinh) NOT BETWEEN 2000 AND 2100
UNION ALL SELECT 'LichSuThayDoiGia', COUNT(*) FROM LichSuThayDoiGia
 WHERE LoaiDoiTuong NOT IN ('HopDong','DichVu','PhongLegacy')
    OR ThangApDung NOT BETWEEN 1 AND 12 OR NamApDung NOT BETWEEN 2000 AND 2100
    OR GiaCu < 0 OR GiaMoi <= 0
UNION ALL SELECT 'HopDongOverlap', COUNT(*) FROM HopDong a JOIN HopDong b
 ON a.Id < b.Id AND a.PhongId = b.PhongId
 AND a.TrangThai <> 'DaHuy' AND b.TrangThai <> 'DaHuy'
 AND a.NgayBatDau <= COALESCE(b.NgayKetThuc,'9999-12-31')
 AND b.NgayBatDau <= COALESCE(a.NgayKetThuc,'9999-12-31')
UNION ALL SELECT 'RepresentativeOverlap', COUNT(*) FROM HopDongKhachThue a JOIN HopDongKhachThue b
 ON a.Id < b.Id AND a.HopDongId = b.HopDongId AND a.LaDaiDien = 1 AND b.LaDaiDien = 1
 AND a.NgayBatDau <= COALESCE(b.NgayKetThuc,'9999-12-31')
 AND b.NgayBatDau <= COALESCE(a.NgayKetThuc,'9999-12-31');

DELIMITER $$
DROP PROCEDURE IF EXISTS Review016Blocker$$
CREATE PROCEDURE Review016Blocker()
BEGIN
    DECLARE ViolationCount INT DEFAULT 0;

    SELECT COUNT(*) INTO ViolationCount FROM (
        SELECT Id FROM Phong WHERE GiaThueMacDinh < 0 OR TrangThai NOT IN ('Trong','DangThue','DangSuaChua')
        UNION ALL SELECT Id FROM DichVu WHERE DonGiaMacDinh < 0
        UNION ALL SELECT Id FROM LichSuHinhThucDichVu WHERE YEAR(KyApDung) NOT BETWEEN 2000 AND 2100
        UNION ALL SELECT Id FROM PhongDichVu WHERE DonGia < 0
        UNION ALL SELECT Id FROM HopDong WHERE YEAR(NgayBatDau) NOT BETWEEN 2000 AND 2100
            OR (NgayKetThuc IS NOT NULL AND YEAR(NgayKetThuc) NOT BETWEEN 2000 AND 2100)
            OR (NgayTraPhongThucTe IS NOT NULL AND (YEAR(NgayTraPhongThucTe) NOT BETWEEN 2000 AND 2100 OR NgayTraPhongThucTe < NgayBatDau))
            OR TienThueThoaThuan <= 0 OR TienCoc < 0 OR TienCocHoanLai < 0 OR NgayThanhToanHangThang NOT BETWEEN 1 AND 31
        UNION ALL SELECT Id FROM HopDongKhachThue WHERE YEAR(NgayBatDau) NOT BETWEEN 2000 AND 2100
            OR (NgayKetThucDuKien IS NOT NULL AND YEAR(NgayKetThucDuKien) NOT BETWEEN 2000 AND 2100)
            OR (NgayKetThuc IS NOT NULL AND YEAR(NgayKetThuc) NOT BETWEEN 2000 AND 2100)
        UNION ALL SELECT Id FROM HopDongDichVu WHERE YEAR(KyBatDau) NOT BETWEEN 2000 AND 2100
            OR (KyKetThuc IS NOT NULL AND YEAR(KyKetThuc) NOT BETWEEN 2000 AND 2100)
        UNION ALL SELECT Id FROM ChiSoDienNuoc WHERE Thang NOT BETWEEN 1 AND 12 OR Nam NOT BETWEEN 2000 AND 2100 OR NgayDoc IS NULL
            OR YEAR(NgayDoc) <> Nam OR MONTH(NgayDoc) <> Thang OR YEAR(NgayDoc) NOT BETWEEN 2000 AND 2100
        UNION ALL SELECT Id FROM ChiSoNgoaiHopDong WHERE YEAR(NgayGhiNhan) NOT BETWEEN 2000 AND 2100
        UNION ALL SELECT Id FROM HoaDon WHERE Thang NOT BETWEEN 1 AND 12 OR Nam NOT BETWEEN 2000 AND 2100
            OR YEAR(NgayLap) NOT BETWEEN 2000 AND 2100 OR (NgayThuThucTe IS NOT NULL AND YEAR(NgayThuThucTe) NOT BETWEEN 2000 AND 2100)
            OR TienPhong < 0 OR TongTienDichVu < 0 OR TongTienPhatSinh < 0 OR TienNoKyTruoc < 0
            OR TongCong < 0 OR SoTienDaThu < 0 OR SoTienDaThu > TongCong
            OR TongCong <> TienPhong + TongTienDichVu + TongTienPhatSinh + TienNoKyTruoc
            OR NOT ((SoTienDaThu = 0 AND TrangThaiThanhToan = 'ChuaThu')
                OR (SoTienDaThu > 0 AND SoTienDaThu < TongCong AND TrangThaiThanhToan = 'ThuMotPhan')
                OR (SoTienDaThu = TongCong AND SoTienDaThu > 0 AND TrangThaiThanhToan = 'DaThu'))
            OR NOT ((SoNgayO IS NULL AND SoNgayTrongThang IS NULL)
                OR (SoNgayO > 0 AND SoNgayTrongThang BETWEEN 28 AND 31 AND SoNgayO <= SoNgayTrongThang
                    AND SoNgayTrongThang = DAY(LAST_DAY(STR_TO_DATE(CONCAT(Nam,'-',LPAD(Thang,2,'0'),'-01'),'%Y-%m-%d')))))
        UNION ALL SELECT Id FROM ChiTietHoaDon WHERE SoLuong < 0 OR DonGia < 0 OR ThanhTien < 0 OR ThanhTien <> ROUND(SoLuong * DonGia, 0)
        UNION ALL SELECT Id FROM ThanhToan WHERE SoTien <= 0 OR HinhThuc IS NULL
            OR HinhThuc NOT IN ('TienMat','ChuyenKhoan','KetChuyenNo','TruCoc') OR YEAR(NgayThu) NOT BETWEEN 2000 AND 2100
        UNION ALL SELECT Id FROM GiaoDichCoc WHERE NOT ((LoaiGiaoDich IN ('ThuCoc','ThuThemCoc') AND SoTien > 0)
                OR (LoaiGiaoDich IN ('HoanCoc','TruNo') AND SoTien < 0) OR (LoaiGiaoDich = 'DieuChinh' AND SoTien <> 0))
            OR YEAR(NgayGiaoDich) NOT BETWEEN 2000 AND 2100 OR (HoaDonId IS NOT NULL AND LoaiGiaoDich <> 'TruNo')
        UNION ALL SELECT Id FROM KhoanPhatSinhHopDong WHERE YEAR(NgayPhatSinh) NOT BETWEEN 2000 AND 2100
        UNION ALL SELECT Id FROM ThuChi WHERE LoaiGiaoDich NOT IN ('Thu','Chi') OR SoTien <= 0 OR YEAR(NgayPhatSinh) NOT BETWEEN 2000 AND 2100
        UNION ALL SELECT Id FROM LichSuThayDoiGia WHERE LoaiDoiTuong NOT IN ('HopDong','DichVu','PhongLegacy')
            OR ThangApDung NOT BETWEEN 1 AND 12 OR NamApDung NOT BETWEEN 2000 AND 2100 OR GiaCu < 0 OR GiaMoi <= 0
        UNION ALL SELECT a.Id FROM HopDong a JOIN HopDong b ON a.Id < b.Id AND a.PhongId = b.PhongId
            AND a.TrangThai <> 'DaHuy' AND b.TrangThai <> 'DaHuy'
            AND a.NgayBatDau <= COALESCE(b.NgayKetThuc,'9999-12-31') AND b.NgayBatDau <= COALESCE(a.NgayKetThuc,'9999-12-31')
        UNION ALL SELECT a.Id FROM HopDongKhachThue a JOIN HopDongKhachThue b
            ON a.Id < b.Id AND a.HopDongId = b.HopDongId AND a.LaDaiDien = 1 AND b.LaDaiDien = 1
            AND a.NgayBatDau <= COALESCE(b.NgayKetThuc,'9999-12-31') AND b.NgayBatDau <= COALESCE(a.NgayKetThuc,'9999-12-31')
    ) AS Violations;

    IF ViolationCount > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'REVIEW-016 BLOCKED: dry-run co du lieu vi pham; script khong sua du lieu.';
    END IF;
END$$

DROP PROCEDURE IF EXISTS Review016AddCheck$$
CREATE PROCEDURE Review016AddCheck(IN TableName VARCHAR(64), IN ConstraintName VARCHAR(64), IN CheckExpression TEXT)
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = TableName
          AND CONSTRAINT_NAME = ConstraintName AND CONSTRAINT_TYPE = 'CHECK'
    ) THEN
        SET @Review016Sql = CONCAT('ALTER TABLE `', TableName, '` ADD CONSTRAINT `', ConstraintName, '` CHECK (', CheckExpression, ')');
        PREPARE Review016Statement FROM @Review016Sql;
        EXECUTE Review016Statement;
        DEALLOCATE PREPARE Review016Statement;
    END IF;
END$$

DROP PROCEDURE IF EXISTS Review016Apply$$
CREATE PROCEDURE Review016Apply()
BEGIN
    CALL Review016AddCheck('Phong','CK_Phong_GiaThue','GiaThueMacDinh >= 0');
    CALL Review016AddCheck('Phong','CK_Phong_TrangThai','TrangThai IN (''Trong'',''DangThue'',''DangSuaChua'')');
    CALL Review016AddCheck('DichVu','CK_DichVu_DonGia','DonGiaMacDinh >= 0');
    CALL Review016AddCheck('LichSuHinhThucDichVu','CK_LichSuHinhThuc_Nam','YEAR(KyApDung) BETWEEN 2000 AND 2100');
    CALL Review016AddCheck('PhongDichVu','CK_PhongDichVu_DonGia','DonGia >= 0');
    CALL Review016AddCheck('HopDong','CK_HopDong_NamNghiepVu','YEAR(NgayBatDau) BETWEEN 2000 AND 2100 AND (NgayKetThuc IS NULL OR YEAR(NgayKetThuc) BETWEEN 2000 AND 2100) AND (NgayTraPhongThucTe IS NULL OR (YEAR(NgayTraPhongThucTe) BETWEEN 2000 AND 2100 AND NgayTraPhongThucTe >= NgayBatDau))');
    CALL Review016AddCheck('HopDong','CK_HopDong_Tien','TienThueThoaThuan > 0 AND TienCoc >= 0 AND (TienCocHoanLai IS NULL OR TienCocHoanLai >= 0)');
    CALL Review016AddCheck('HopDong','CK_HopDong_NgayThanhToan','NgayThanhToanHangThang BETWEEN 1 AND 31');
    CALL Review016AddCheck('HopDongKhachThue','CK_HDKT_NamNghiepVu','YEAR(NgayBatDau) BETWEEN 2000 AND 2100 AND (NgayKetThucDuKien IS NULL OR YEAR(NgayKetThucDuKien) BETWEEN 2000 AND 2100) AND (NgayKetThuc IS NULL OR YEAR(NgayKetThuc) BETWEEN 2000 AND 2100)');
    CALL Review016AddCheck('HopDongDichVu','CK_HopDongDichVu_Nam','YEAR(KyBatDau) BETWEEN 2000 AND 2100 AND (KyKetThuc IS NULL OR YEAR(KyKetThuc) BETWEEN 2000 AND 2100)');
    CALL Review016AddCheck('ChiSoDienNuoc','CK_ChiSo_Ky','Thang BETWEEN 1 AND 12 AND Nam BETWEEN 2000 AND 2100');
    CALL Review016AddCheck('ChiSoDienNuoc','CK_ChiSo_NgayDoc','NgayDoc IS NOT NULL AND YEAR(NgayDoc) BETWEEN 2000 AND 2100 AND MONTH(NgayDoc) = Thang AND YEAR(NgayDoc) = Nam');
    CALL Review016AddCheck('ChiSoNgoaiHopDong','CK_ChiSoNgoaiHopDong_Nam','YEAR(NgayGhiNhan) BETWEEN 2000 AND 2100');
    CALL Review016AddCheck('HoaDon','CK_HoaDon_Ky','Thang BETWEEN 1 AND 12 AND Nam BETWEEN 2000 AND 2100');
    CALL Review016AddCheck('HoaDon','CK_HoaDon_NgayNghiepVu','YEAR(NgayLap) BETWEEN 2000 AND 2100 AND (NgayThuThucTe IS NULL OR YEAR(NgayThuThucTe) BETWEEN 2000 AND 2100)');
    CALL Review016AddCheck('HoaDon','CK_HoaDon_Tien','TienPhong >= 0 AND TongTienDichVu >= 0 AND TongTienPhatSinh >= 0 AND TienNoKyTruoc >= 0 AND TongCong >= 0 AND SoTienDaThu >= 0 AND SoTienDaThu <= TongCong AND TongCong = TienPhong + TongTienDichVu + TongTienPhatSinh + TienNoKyTruoc');
    CALL Review016AddCheck('HoaDon','CK_HoaDon_TrangThai','(SoTienDaThu = 0 AND TrangThaiThanhToan = ''ChuaThu'') OR (SoTienDaThu > 0 AND SoTienDaThu < TongCong AND TrangThaiThanhToan = ''ThuMotPhan'') OR (SoTienDaThu = TongCong AND SoTienDaThu > 0 AND TrangThaiThanhToan = ''DaThu'')');
    CALL Review016AddCheck('HoaDon','CK_HoaDon_SoNgay','(SoNgayO IS NULL AND SoNgayTrongThang IS NULL) OR (SoNgayO > 0 AND SoNgayTrongThang BETWEEN 28 AND 31 AND SoNgayO <= SoNgayTrongThang AND SoNgayTrongThang = DAY(LAST_DAY(STR_TO_DATE(CONCAT(Nam,''-'',LPAD(Thang,2,''0''),''-01''),''%Y-%m-%d''))))');
    CALL Review016AddCheck('ChiTietHoaDon','CK_CTHD_Tien','SoLuong >= 0 AND DonGia >= 0 AND ThanhTien >= 0 AND ThanhTien = ROUND(SoLuong * DonGia, 0)');
    CALL Review016AddCheck('ThanhToan','CK_ThanhToan_Tien','SoTien > 0');
    CALL Review016AddCheck('ThanhToan','CK_ThanhToan_HinhThuc','HinhThuc IN (''TienMat'',''ChuyenKhoan'',''KetChuyenNo'',''TruCoc'')');
    CALL Review016AddCheck('ThanhToan','CK_ThanhToan_Ngay','YEAR(NgayThu) BETWEEN 2000 AND 2100');
    CALL Review016AddCheck('GiaoDichCoc','CK_GiaoDichCoc_SoTien','(LoaiGiaoDich IN (''ThuCoc'',''ThuThemCoc'') AND SoTien > 0) OR (LoaiGiaoDich IN (''HoanCoc'',''TruNo'') AND SoTien < 0) OR (LoaiGiaoDich = ''DieuChinh'' AND SoTien <> 0)');
    CALL Review016AddCheck('GiaoDichCoc','CK_GiaoDichCoc_Ngay','YEAR(NgayGiaoDich) BETWEEN 2000 AND 2100');
    CALL Review016AddCheck('GiaoDichCoc','CK_GiaoDichCoc_LienKet','HoaDonId IS NULL OR LoaiGiaoDich = ''TruNo''');
    CALL Review016AddCheck('KhoanPhatSinhHopDong','CK_KhoanPhatSinh_Nam','YEAR(NgayPhatSinh) BETWEEN 2000 AND 2100');
    CALL Review016AddCheck('ThuChi','CK_ThuChi_Loai','LoaiGiaoDich IN (''Thu'',''Chi'')');
    CALL Review016AddCheck('ThuChi','CK_ThuChi_Tien','SoTien > 0');
    CALL Review016AddCheck('ThuChi','CK_ThuChi_Ngay','YEAR(NgayPhatSinh) BETWEEN 2000 AND 2100');
    CALL Review016AddCheck('LichSuThayDoiGia','CK_LichSuGia_Loai','LoaiDoiTuong IN (''HopDong'',''DichVu'',''PhongLegacy'')');
    CALL Review016AddCheck('LichSuThayDoiGia','CK_LichSuGia_Ky','ThangApDung BETWEEN 1 AND 12 AND NamApDung BETWEEN 2000 AND 2100');
    CALL Review016AddCheck('LichSuThayDoiGia','CK_LichSuGia_Tien','GiaCu >= 0 AND GiaMoi > 0');

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ThanhToan'
          AND COLUMN_NAME = 'HinhThuc' AND IS_NULLABLE = 'YES'
    ) THEN
        ALTER TABLE ThanhToan MODIFY COLUMN HinhThuc VARCHAR(20) NOT NULL;
    END IF;
END$$

CALL Review016Blocker()$$
CALL Review016Apply()$$
DROP PROCEDURE Review016Apply$$
DROP PROCEDURE Review016AddCheck$$
DROP PROCEDURE Review016Blocker$$

DROP TRIGGER IF EXISTS TR_HopDong_NoOverlap_Insert$$
CREATE TRIGGER TR_HopDong_NoOverlap_Insert BEFORE INSERT ON HopDong FOR EACH ROW
BEGIN
    DECLARE LockedRoomId INT;
    DECLARE OverlapCount INT DEFAULT 0;
    SELECT Id INTO LockedRoomId FROM Phong WHERE Id = NEW.PhongId FOR UPDATE;
    IF NEW.TrangThai <> 'DaHuy' THEN
        SELECT COUNT(*) INTO OverlapCount FROM HopDong
        WHERE PhongId = NEW.PhongId AND TrangThai <> 'DaHuy'
          AND NgayBatDau <= COALESCE(NEW.NgayKetThuc,'9999-12-31')
          AND COALESCE(NgayKetThuc,'9999-12-31') >= NEW.NgayBatDau;
        IF OverlapCount > 0 THEN SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'REVIEW-016: hop dong chong khoang thoi gian cua phong.'; END IF;
    END IF;
END$$

DROP TRIGGER IF EXISTS TR_HopDong_NoOverlap_Update$$
CREATE TRIGGER TR_HopDong_NoOverlap_Update BEFORE UPDATE ON HopDong FOR EACH ROW
BEGIN
    DECLARE LockedRoomId INT;
    DECLARE OverlapCount INT DEFAULT 0;
    IF OLD.PhongId <= NEW.PhongId THEN
        SELECT Id INTO LockedRoomId FROM Phong WHERE Id = OLD.PhongId FOR UPDATE;
        IF NEW.PhongId <> OLD.PhongId THEN SELECT Id INTO LockedRoomId FROM Phong WHERE Id = NEW.PhongId FOR UPDATE; END IF;
    ELSE
        SELECT Id INTO LockedRoomId FROM Phong WHERE Id = NEW.PhongId FOR UPDATE;
        SELECT Id INTO LockedRoomId FROM Phong WHERE Id = OLD.PhongId FOR UPDATE;
    END IF;
    IF NEW.TrangThai <> 'DaHuy' THEN
        SELECT COUNT(*) INTO OverlapCount FROM HopDong
        WHERE PhongId = NEW.PhongId AND Id <> OLD.Id AND TrangThai <> 'DaHuy'
          AND NgayBatDau <= COALESCE(NEW.NgayKetThuc,'9999-12-31')
          AND COALESCE(NgayKetThuc,'9999-12-31') >= NEW.NgayBatDau;
        IF OverlapCount > 0 THEN SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'REVIEW-016: hop dong chong khoang thoi gian cua phong.'; END IF;
    END IF;
END$$

DROP TRIGGER IF EXISTS TR_HDKT_RepresentativeOverlap_Insert$$
CREATE TRIGGER TR_HDKT_RepresentativeOverlap_Insert BEFORE INSERT ON HopDongKhachThue FOR EACH ROW
BEGIN
    DECLARE LockedContractId INT;
    DECLARE OverlapCount INT DEFAULT 0;
    SELECT Id INTO LockedContractId FROM HopDong WHERE Id = NEW.HopDongId FOR UPDATE;
    IF NEW.LaDaiDien = 1 THEN
        SELECT COUNT(*) INTO OverlapCount FROM HopDongKhachThue
        WHERE HopDongId = NEW.HopDongId AND LaDaiDien = 1
          AND NgayBatDau <= COALESCE(NEW.NgayKetThuc,'9999-12-31')
          AND COALESCE(NgayKetThuc,'9999-12-31') >= NEW.NgayBatDau;
        IF OverlapCount > 0 THEN SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'REVIEW-016: hai dai dien chong thoi gian trong hop dong.'; END IF;
    END IF;
END$$

DROP TRIGGER IF EXISTS TR_HDKT_RepresentativeOverlap_Update$$
CREATE TRIGGER TR_HDKT_RepresentativeOverlap_Update BEFORE UPDATE ON HopDongKhachThue FOR EACH ROW
BEGIN
    DECLARE LockedContractId INT;
    DECLARE OverlapCount INT DEFAULT 0;
    IF OLD.HopDongId <= NEW.HopDongId THEN
        SELECT Id INTO LockedContractId FROM HopDong WHERE Id = OLD.HopDongId FOR UPDATE;
        IF NEW.HopDongId <> OLD.HopDongId THEN SELECT Id INTO LockedContractId FROM HopDong WHERE Id = NEW.HopDongId FOR UPDATE; END IF;
    ELSE
        SELECT Id INTO LockedContractId FROM HopDong WHERE Id = NEW.HopDongId FOR UPDATE;
        SELECT Id INTO LockedContractId FROM HopDong WHERE Id = OLD.HopDongId FOR UPDATE;
    END IF;
    IF NEW.LaDaiDien = 1 THEN
        SELECT COUNT(*) INTO OverlapCount FROM HopDongKhachThue
        WHERE HopDongId = NEW.HopDongId AND Id <> OLD.Id AND LaDaiDien = 1
          AND NgayBatDau <= COALESCE(NEW.NgayKetThuc,'9999-12-31')
          AND COALESCE(NgayKetThuc,'9999-12-31') >= NEW.NgayBatDau;
        IF OverlapCount > 0 THEN SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'REVIEW-016: hai dai dien chong thoi gian trong hop dong.'; END IF;
    END IF;
END$$
DELIMITER ;

SELECT COUNT(*) AS Review016CheckCount
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = DATABASE() AND CONSTRAINT_TYPE = 'CHECK'
  AND CONSTRAINT_NAME IN (
    'CK_Phong_GiaThue','CK_Phong_TrangThai','CK_DichVu_DonGia','CK_LichSuHinhThuc_Nam',
    'CK_PhongDichVu_DonGia','CK_HopDong_NamNghiepVu','CK_HopDong_Tien','CK_HopDong_NgayThanhToan',
    'CK_HDKT_NamNghiepVu','CK_HopDongDichVu_Nam','CK_ChiSo_Ky','CK_ChiSo_NgayDoc',
    'CK_ChiSoNgoaiHopDong_Nam','CK_HoaDon_Ky','CK_HoaDon_NgayNghiepVu','CK_HoaDon_Tien',
    'CK_HoaDon_TrangThai','CK_HoaDon_SoNgay','CK_CTHD_Tien','CK_ThanhToan_Tien',
    'CK_ThanhToan_HinhThuc','CK_ThanhToan_Ngay','CK_GiaoDichCoc_SoTien','CK_GiaoDichCoc_Ngay',
    'CK_GiaoDichCoc_LienKet','CK_KhoanPhatSinh_Nam','CK_ThuChi_Loai','CK_ThuChi_Tien',
    'CK_ThuChi_Ngay','CK_LichSuGia_Loai','CK_LichSuGia_Ky','CK_LichSuGia_Tien'
  );
SELECT COUNT(*) AS Review016TriggerCount FROM INFORMATION_SCHEMA.TRIGGERS
WHERE TRIGGER_SCHEMA = DATABASE() AND TRIGGER_NAME IN (
  'TR_HopDong_NoOverlap_Insert','TR_HopDong_NoOverlap_Update',
  'TR_HDKT_RepresentativeOverlap_Insert','TR_HDKT_RepresentativeOverlap_Update');
SELECT IS_NULLABLE AS ThanhToanHinhThucNullable FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ThanhToan' AND COLUMN_NAME = 'HinhThuc';
