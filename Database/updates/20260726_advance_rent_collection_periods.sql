-- BILLING-ADVANCE-RENT-ARREARS-SERVICE-CUTOVER-202608
-- Sequence 13. Blocker-first: the operational cutover is known to have no legacy invoices.
-- Do not infer three new periods from historical Thang/Nam rows.

DROP PROCEDURE IF EXISTS qlnt_m13;
DELIMITER $$
CREATE PROCEDURE qlnt_m13()
BEGIN
    DECLARE legacy_invoices BIGINT DEFAULT 0;
    DECLARE has_kythu INT DEFAULT 0;

    SELECT COUNT(*) INTO has_kythu
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='HoaDon' AND COLUMN_NAME='KyThu';

    IF has_kythu = 0 THEN
        SELECT COUNT(*) INTO legacy_invoices FROM HoaDon;
        IF legacy_invoices <> 0 THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'M13 BLOCKED: HoaDon legacy exists; three-period backfill requires explicit evidence.';
        END IF;

        ALTER TABLE GiaoDichCoc
            ADD COLUMN NguonDoiChieu VARCHAR(255) NULL AFTER NguonThamChieu;

        ALTER TABLE ChiSoDienNuoc
            DROP CHECK CK_ChiSo_NgayDoc,
            ADD CONSTRAINT CK_ChiSo_NgayDoc CHECK (
                NgayDoc IS NOT NULL AND YEAR(NgayDoc) BETWEEN 2000 AND 2100
                AND (
                    (NgayDoc BETWEEN
                        DATE_ADD(STR_TO_DATE(CONCAT(Nam,'-',LPAD(Thang,2,'0'),'-01'),'%Y-%m-%d'),INTERVAL 1 MONTH)
                        AND DATE_ADD(
                            DATE_ADD(STR_TO_DATE(CONCAT(Nam,'-',LPAD(Thang,2,'0'),'-01'),'%Y-%m-%d'),INTERVAL 1 MONTH),
                            INTERVAL 4 DAY))
                    OR (MONTH(NgayDoc)=Thang AND YEAR(NgayDoc)=Nam)
                ));

        ALTER TABLE HoaDon
            DROP INDEX UQ_HoaDon,
            DROP CHECK CK_HoaDon_Ky,
            DROP CHECK CK_HoaDon_NgayDenHan,
            DROP CHECK CK_HoaDon_Tien,
            DROP CHECK CK_HoaDon_TrangThai,
            DROP CHECK CK_HoaDon_SoNgay,
            DROP COLUMN Thang,
            DROP COLUMN Nam,
            ADD COLUMN KyThu DATE NOT NULL AFTER HoaDonGhepId,
            ADD COLUMN KyTienPhong DATE NOT NULL AFTER KyThu,
            ADD COLUMN KyDichVu DATE NOT NULL AFTER KyTienPhong,
            ADD COLUMN LoaiHoaDon VARCHAR(30) NOT NULL DEFAULT 'DinhKy' AFTER KyDichVu,
            ADD COLUMN Thang TINYINT GENERATED ALWAYS AS (MONTH(KyThu)) STORED AFTER LoaiHoaDon,
            ADD COLUMN Nam SMALLINT GENERATED ALWAYS AS (YEAR(KyThu)) STORED AFTER Thang,
            ADD COLUMN TienTinDungApDung DECIMAL(12,0) NOT NULL DEFAULT 0 AFTER TienNoKyTruoc,
            ADD CONSTRAINT UQ_HoaDon UNIQUE (HopDongId,KyThu,LoaiHoaDon),
            ADD CONSTRAINT CK_HoaDon_Ky CHECK (
                DAY(KyThu)=1 AND DAY(KyTienPhong)=1 AND DAY(KyDichVu)=1
                AND YEAR(KyThu) BETWEEN 2000 AND 2100
                AND KyThu >= '2026-08-01'
                AND ((LoaiHoaDon='DinhKy' AND KyTienPhong=KyThu
                      AND KyDichVu=DATE_SUB(KyThu,INTERVAL 1 MONTH))
                     OR (LoaiHoaDon<>'DinhKy' AND KyTienPhong=KyThu AND KyDichVu=KyThu))),
            ADD CONSTRAINT CK_HoaDon_Loai CHECK (
                LoaiHoaDon IN ('DinhKy','KhoiTaoHopDong','QuyetToanTraPhong',
                               'QuyetToanChuyenPhongCu','QuyetToanChuyenPhongMoi')),
            ADD CONSTRAINT CK_HoaDon_NgayDenHan CHECK (
                (LoaiHoaDon='DinhKy' AND NgayDenHan=DATE_ADD(KyThu,INTERVAL 9 DAY))
                OR (LoaiHoaDon<>'DinhKy' AND NgayDenHan BETWEEN KyThu AND LAST_DAY(KyThu))),
            ADD CONSTRAINT CK_HoaDon_Tien CHECK (
                TienPhong>=0 AND TongTienDichVu>=0 AND TongTienPhatSinh>=0
                AND TienNoKyTruoc>=0 AND TienTinDungApDung>=0
                AND TongCong>=0 AND SoTienDaThu>=0 AND SoTienDaThu<=TongCong
                AND TongCong=TienPhong+TongTienDichVu+TongTienPhatSinh
                             +TienNoKyTruoc-TienTinDungApDung),
            ADD CONSTRAINT CK_HoaDon_TrangThai CHECK (
                (SoTienDaThu=0 AND TrangThaiThanhToan='ChuaThu')
                OR (TongCong=0 AND SoTienDaThu=0 AND TrangThaiThanhToan='DaThu')
                OR (SoTienDaThu>0 AND SoTienDaThu<TongCong
                    AND TrangThaiThanhToan='ThuMotPhan')
                OR (SoTienDaThu=TongCong AND SoTienDaThu>0
                    AND TrangThaiThanhToan='DaThu')),
            ADD CONSTRAINT CK_HoaDon_SoNgay CHECK (
                (SoNgayO IS NULL AND SoNgayTrongThang IS NULL)
                OR (SoNgayO>0 AND SoNgayTrongThang BETWEEN 28 AND 31
                    AND SoNgayO<=SoNgayTrongThang
                    AND SoNgayTrongThang=DAY(LAST_DAY(KyTienPhong))));

        ALTER TABLE ChiTietHoaDon
            ADD COLUMN KySuDung DATE NOT NULL AFTER DonViTinhSnapshot,
            ADD COLUMN NgayDocSnapshot DATE NULL AFTER KySuDung,
            ADD COLUMN ChiSoDauSnapshot DECIMAL(12,2) NULL AFTER NgayDocSnapshot,
            ADD COLUMN ChiSoCuoiSnapshot DECIMAL(12,2) NULL AFTER ChiSoDauSnapshot,
            ADD COLUMN LoaiGhiNhanSnapshot VARCHAR(20) NULL AFTER ChiSoCuoiSnapshot,
            ADD COLUMN ChiSoTruocResetSnapshot DECIMAL(12,2) NULL AFTER LoaiGhiNhanSnapshot,
            ADD COLUMN ChiSoSauResetSnapshot DECIMAL(12,2) NULL AFTER ChiSoTruocResetSnapshot,
            ADD COLUMN LyDoDieuChinhSnapshot VARCHAR(255) NULL AFTER ChiSoSauResetSnapshot,
            ADD CONSTRAINT CK_CTHD_Ky CHECK (
                DAY(KySuDung)=1 AND YEAR(KySuDung) BETWEEN 2000 AND 2100),
            ADD CONSTRAINT CK_CTHD_ChiSoSnapshot CHECK (
                (ChiSoDienNuocId IS NULL
                 AND NgayDocSnapshot IS NULL AND ChiSoDauSnapshot IS NULL
                 AND ChiSoCuoiSnapshot IS NULL AND LoaiGhiNhanSnapshot IS NULL)
                OR
                (ChiSoDienNuocId IS NOT NULL
                 AND NgayDocSnapshot IS NOT NULL AND ChiSoDauSnapshot IS NOT NULL
                 AND ChiSoCuoiSnapshot IS NOT NULL
                 AND LoaiGhiNhanSnapshot IN ('BinhThuong','Reset')));
    END IF;
END$$
DELIMITER ;

CALL qlnt_m13();
DROP PROCEDURE qlnt_m13;

CREATE TABLE IF NOT EXISTS GiaoDichTinDungTienPhong (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    HopDongId INT NOT NULL,
    HoaDonId INT NULL,
    HopDongLienQuanId INT NULL,
    LoaiGiaoDich VARCHAR(30) NOT NULL,
    SoTien DECIMAL(12,0) NOT NULL,
    SoDuSauGiaoDich DECIMAL(12,0) NOT NULL,
    NgayGiaoDich DATE NOT NULL,
    IdempotencyKey VARCHAR(160) NOT NULL,
    LyDo VARCHAR(500) NOT NULL,
    NguoiThucHien VARCHAR(100) NOT NULL,
    NgayTao DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT UQ_TinDungTienPhong_Idempotency UNIQUE (IdempotencyKey),
    CONSTRAINT FK_TinDungTienPhong_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    CONSTRAINT FK_TinDungTienPhong_HoaDon FOREIGN KEY (HoaDonId) REFERENCES HoaDon(Id),
    CONSTRAINT FK_TinDungTienPhong_HopDongLienQuan FOREIGN KEY (HopDongLienQuanId) REFERENCES HopDong(Id),
    CONSTRAINT CK_TinDungTienPhong_Loai CHECK (
        LoaiGiaoDich IN ('TaoKhiTraPhong','ChuyenSangHopDong','ApDungHoaDon','HoanTien','DieuChinh')),
    CONSTRAINT CK_TinDungTienPhong_Tien CHECK (
        SoTien<>0 AND SoDuSauGiaoDich>=0
        AND ((LoaiGiaoDich IN ('TaoKhiTraPhong','ChuyenSangHopDong') AND SoTien>0)
             OR (LoaiGiaoDich IN ('ApDungHoaDon','HoanTien') AND SoTien<0)
             OR LoaiGiaoDich='DieuChinh')),
    INDEX IX_TinDungTienPhong_HopDong (HopDongId,NgayGiaoDich,Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS AuditDongHopDongTruocCutover (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    HopDongId INT NOT NULL,
    NgayTraPhong DATE NOT NULL,
    KyTienPhongDaThanhToanDen DATE NOT NULL,
    KyDichVuDaThanhToanDen DATE NOT NULL,
    CongNoXacNhan DECIMAL(12,0) NOT NULL,
    SoTienHoanCoc DECIMAL(12,0) NOT NULL,
    NgayHoanCoc DATE NOT NULL,
    NguonDoiChieu VARCHAR(255) NOT NULL,
    NguoiThucHien VARCHAR(100) NOT NULL,
    LyDoCutover VARCHAR(500) NOT NULL,
    IdempotencyKey VARCHAR(160) NOT NULL,
    ThoiGianThucHien DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT UQ_AuditCutover_HopDong UNIQUE (HopDongId),
    CONSTRAINT UQ_AuditCutover_Idempotency UNIQUE (IdempotencyKey),
    CONSTRAINT FK_AuditCutover_HopDong FOREIGN KEY (HopDongId) REFERENCES HopDong(Id),
    CONSTRAINT CK_AuditCutover_Ky CHECK (
        NgayTraPhong<'2026-08-01'
        AND DAY(KyTienPhongDaThanhToanDen)=1
        AND DAY(KyDichVuDaThanhToanDen)=1
        AND CongNoXacNhan=0 AND SoTienHoanCoc>0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

DROP TRIGGER IF EXISTS TR_HoaDon_SnapshotImmutable_Update;
DROP TRIGGER IF EXISTS TR_CTHD_SnapshotImmutable_Update;
DELIMITER $$
CREATE TRIGGER TR_HoaDon_SnapshotImmutable_Update
BEFORE UPDATE ON HoaDon
FOR EACH ROW
BEGIN
    IF (EXISTS(SELECT 1 FROM ThanhToan WHERE HoaDonId=OLD.Id)
        OR EXISTS(SELECT 1 FROM GiaoDichTinDungTienPhong WHERE HoaDonId=OLD.Id))
       AND NOT (
           NEW.KyThu <=> OLD.KyThu
           AND NEW.KyTienPhong <=> OLD.KyTienPhong
           AND NEW.KyDichVu <=> OLD.KyDichVu
           AND NEW.LoaiHoaDon <=> OLD.LoaiHoaDon
           AND NEW.TienPhong <=> OLD.TienPhong
           AND NEW.TongTienDichVu <=> OLD.TongTienDichVu
           AND NEW.TongTienPhatSinh <=> OLD.TongTienPhatSinh
           AND NEW.TienNoKyTruoc <=> OLD.TienNoKyTruoc
           AND NEW.TienTinDungApDung <=> OLD.TienTinDungApDung
           AND NEW.TongCong <=> OLD.TongCong
           AND NEW.NhaIdSnapshot <=> OLD.NhaIdSnapshot
           AND NEW.TenNhaSnapshot <=> OLD.TenNhaSnapshot
           AND NEW.PhongIdSnapshot <=> OLD.PhongIdSnapshot
           AND NEW.TenPhongSnapshot <=> OLD.TenPhongSnapshot
           AND NEW.KhachDaiDienIdSnapshot <=> OLD.KhachDaiDienIdSnapshot
           AND NEW.TenKhachDaiDienSnapshot <=> OLD.TenKhachDaiDienSnapshot
           AND NEW.CccdKhachDaiDienSnapshot <=> OLD.CccdKhachDaiDienSnapshot
       ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT='M13: invoice snapshot is immutable after payment.';
    END IF;
END$$

CREATE TRIGGER TR_CTHD_SnapshotImmutable_Update
BEFORE UPDATE ON ChiTietHoaDon
FOR EACH ROW
BEGIN
    IF (EXISTS(SELECT 1 FROM ThanhToan WHERE HoaDonId=OLD.HoaDonId)
        OR EXISTS(SELECT 1 FROM GiaoDichTinDungTienPhong WHERE HoaDonId=OLD.HoaDonId)) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT='M13: invoice detail snapshot is immutable after payment.';
    END IF;
END$$
DELIMITER ;
