-- REVIEW-006/007: contract date/status invariants and overlap lookup support.
-- Apply once after checking the dry-run queries below return zero invalid rows.
SET SQL_SAFE_UPDATES = 0;

SELECT COUNT(*) AS InvalidDateRanges
FROM HopDong
WHERE NgayKetThuc IS NOT NULL AND NgayKetThuc < NgayBatDau;

SELECT COUNT(*) AS InvalidStatuses
FROM HopDong
WHERE TrangThai NOT IN ('ChoHieuLuc', 'DangHieuLuc', 'DaKetThuc', 'DaHuy', 'DaChuyenPhong');

ALTER TABLE HopDong
    ADD CONSTRAINT CK_HopDong_KhoangNgay
        CHECK (NgayKetThuc IS NULL OR NgayKetThuc >= NgayBatDau),
    ADD CONSTRAINT CK_HopDong_TrangThai
        CHECK (TrangThai IN ('ChoHieuLuc', 'DangHieuLuc', 'DaKetThuc', 'DaHuy', 'DaChuyenPhong'));

CREATE INDEX IX_HopDong_Phong_KhoangNgay
    ON HopDong(PhongId, NgayBatDau, NgayKetThuc, TrangThai);
